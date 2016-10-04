using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityStandardAssets.Vehicles.Car;

public class Player : NetworkBehaviour {
	public struct NetId {
		public NetworkInstanceId id;
	}

	public class SyncListNetId : SyncListStruct<NetId> {	
		public void addId(NetworkInstanceId id) {
			NetId w;
			w.id = id;
			this.Add (w);
		}

		public void removeId(NetworkInstanceId id) {
			List<NetId> toRemove = this.Where (w => (w.id == id)).ToList ();
			if (toRemove.Count > 0) {
				this.Remove (toRemove[0]);
			}
		}
	}

	const int NEGATIVE_MONTH_LIMIT = 2;
	const int TAX_PERIOD = 12;
	const int MAX_LOTS = 3;

	const int CHANNEL = 1;
	static int numPlayers = 0;
	static Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.black, Color.magenta };
	static int[] ignoredTypes = { 18, 17, 15, 21, 25};

	[SyncVar]
	public string playerName;
	[SyncVar]
	public string companyName;
	[SyncVar(hook = "updateBudget")]
	public int budget;
	[SyncVar(hook = "updateRevenue")]
	public int revenue;
	[SyncVar]
	public int id;
	[SyncVar]
	public int lots;
	[SyncVar(hook = "setColor")]
	public Color color;
	[SyncVar]
	public int networth;
	[SyncVar(hook = "updateExperience")]
	public int playerExperience;
	[SyncVar(hook = "showMessage")]
	public string message;
	[SyncVar(hook = "togglePlayerVisibility")]
	public bool playerNotVisible;

	//Vehicle vars
	public bool eligibleToExitVehicle;
	public CarController currentVehicle;

	[SyncVar]
	public string leaders;


	public SyncListNetId owned = new SyncListNetId();

	public OwnableObject targetObject;
	public Vehicle targetVehicle;
	public int destinationBuilding;
	public MonthManager monthManager;
	public Player targetPlayer;
	public Player localPlayer;
	public CameraController soundPlayer;

	private bool busyPanel; // used to indicate there is an active panel on the screen, so extra panels shouldn't be spawned

	[SyncVar]
	private int negativeMonths;
	public int updateIn;
	private BuildingModifier targetMods;
	private GameObject playerCamera;
	private GameObject flybyCamera;
	private ButtonSetter button;
	private CanvasManager ui;
	private int oldMonth;
	private GameObject upgradePanel;
	private AuctionManager auction;
	public OverlandMap overlandMap;
	private Camera overlandMapCam;
	private GameObject playerMapMarker;
	private List<GameObject> notifications;
	private static Sprite[] notificationSprites;
	public List<Building> deliveryDestinations;
	private InputField chatbox;
	public bool construction;
	private GameObject toBuild;
	private List<Object> spawnables;
	private List<Object> networkSpawnables;
	private int currentSpawnable;
	private bool readyToConstruct;
	private ConstructionController constructionController;
	public bool moveMode;
	private GameObject moveTarget;
	private GameObject activePanel;
	private StartupRigidbodyFirstPersonController characterController;
	public GameObject bankruptChoice;

	// Use this for initialization
	void Start () {
		// Server handles initialization of 
		if (isServer) {
			id = numPlayers;
			playerName = "Player " + numPlayers;
			budget = 2000;
			revenue = 0;
			playerExperience = 0;
			color = colors [id % colors.Length];
			numPlayers++;
			companyName = "Epic Inc.";
			negativeMonths = 0;
		}

		if (notificationSprites == null) {
			notificationSprites = Resources.LoadAll<Sprite> ("Icons and Portraits/64 flat icons/png/32px");
		}
		flybyCamera = GameObject.Find ("Flyby Camera");
		button = GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").GetComponent<ButtonSetter> ();
		updateIn = 0;

		monthManager = GameObject.Find ("Clock").GetComponent<MonthManager> ();
		constructionController = GetComponent<ConstructionController> ();
		characterController = GetComponent<StartupRigidbodyFirstPersonController> ();
		deliveryDestinations = new List<Building> ();
		oldMonth = monthManager.getMonth ();
		eligibleToExitVehicle = false;

		//Sets the player's MapMarker to their player color
		playerMapMarker = gameObject.transform.Find ("MapMarker").gameObject;
		playerMapMarker.GetComponent<MeshRenderer> ().material.color = color;
		overlandMap = gameObject.transform.Find ("OverlandMapCam").GetComponent<OverlandMap> ();
		overlandMapCam = overlandMap.GetComponentInParent<Camera> ();

		Player[] players = FindObjectsOfType<Player> ();
		foreach (Player p in players) {
			if (p.isLocalPlayer) {
				localPlayer = p;
				break;
			}
		}

		if (!isLocalPlayer) {
			return;
		}

		button.setButtons (this);	
		activePanel = spawnSetupPanel ();
		characterController.setMovementEnabled (false);

		notifications = new List<GameObject> ();
		GameObject canvas = GameObject.Find ("Canvas");
		GameObject tmp = canvas.transform.Find ("ChatUI").transform.Find("Bg").transform.Find("Inpunt").gameObject.transform.Find("InputField").gameObject;
		chatbox = tmp.GetComponent<InputField> ();
		playerCamera = gameObject.transform.Find ("MainCamera").gameObject;
		soundPlayer = playerCamera.GetComponent<CameraController> ();

		Destroy (flybyCamera);
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		leaders = monthManager.SetLeaders ();
		updateUI ();

	}
		
	// Update is called once per frame
	void Update () {

		if (!isLocalPlayer || chatbox.isFocused || activePanel != null) {
			return;
		}

		if (Input.GetKeyDown (KeyCode.E) && !moveMode && !construction) {
			Vector3 fwd = playerCamera.transform.TransformDirection(Vector3.forward); // ray shooting from camera
			RaycastHit hit;

			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100)) {
				GameObject tmp = hit.collider.gameObject;
				if (tmp != null) {
					OwnableObject b = tmp.GetComponent<OwnableObject> ();
					if ((b != null) && b.ownedBy(netId) && (getLocalInstance(b.lot) != null) && (getBuilding(b.lot).validOwner())) {
						moveMode = true;
						moveTarget = tmp;
					}
				}
			}

			if (!moveMode) {
				construction = true;
			}
		}

		if (moveMode) {
			constructionController.moveMode (moveTarget);
			return;
		}

		if (construction) {
			constructionController.buildMode ();
			return;
		}

		if (Input.GetButtonDown ("Fire2")) {
			targetSelect ();
		}

		if (Input.GetKeyDown (KeyCode.Tab)) {
			updateUI ();
			ui.ledgerToggle ();
		}

		// overland map toggle
		if (Input.GetKeyDown (KeyCode.M)) {
			if (overlandMapCam.enabled) {
				overlandMap.DisableOverlandMap ();
			} else {
				overlandMap.EnableOverlandMap ();
			}
		}

		if (Input.GetKeyDown (KeyCode.N)) {
			formNeighborhood (targetObject);
		}
		if (Input.GetKeyDown (KeyCode.K)) { 
			addToNeighborhood (targetObject);
		}
		if (Input.GetKeyDown (KeyCode.F)) {
			spawnGiveMoneyPanel ();
		}
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			toggleManager();
		}
		if (Input.GetKeyDown(KeyCode.I)) {
			CmdAddMoney (this.GetComponent<NetworkIdentity> ().netId, 10000);
		}

		// Repair
		if (Input.GetKeyDown (KeyCode.R)) {
			if (targetObject != null) {
				if (targetObject.getOwner () == id) { // personally owned building
					CmdRepair (gameObject.GetComponent<NetworkIdentity> ().netId, targetObject.gameObject.GetComponent<NetworkIdentity> ().netId);
				}
				updateUI ();
			}
			if (targetVehicle != null) {
				if (targetVehicle.getOwner () == id) {
					CmdRepair (gameObject.GetComponent<NetworkIdentity> ().netId, targetVehicle.gameObject.GetComponent<NetworkIdentity> ().netId);
				}
			}
		}

		// Fire test button
		if (Input.GetKeyDown (KeyCode.Z)) {
			if (targetObject != null) {
				if (targetObject is DamageableObject) {
					targetObject.GetComponent<DamageableObject> ().setFire ();
				}
			}
		}

		if (Input.GetKeyDown (KeyCode.P)) {
			if (targetObject != null) {
				if (targetObject.getOwner() == id) {
					sellChoice ();
				}
			}
		}

		if (Input.GetKeyDown (KeyCode.X)) {
			if (targetObject != null && targetObject.isDestructable() && targetObject.ownedBy(this.netId)) {
				activePanel = constructionController.confirmDestroy (targetObject.gameObject);
				controlsAllowed (false);
			}
		}

		if (Input.GetKeyDown (KeyCode.LeftShift)) {
			buy ();
		}

		if (Input.GetKeyDown (KeyCode.O)) {
			monthManager.toggleColors ();
		}

		if (oldMonth != monthManager.getMonth ()) {
			updateUI ();
			oldMonth = monthManager.getMonth ();
			if (bankruptChoice != null) {
				bankruptChoice.GetComponent<BankruptcyPanel> ().seize ();
				bankruptChoice = null;
			} else if (negativeMonths >= NEGATIVE_MONTH_LIMIT && bankruptChoice == null) {
				repoMan ();
			}
		} else if (updateIn > 0) {
			updateIn--;
			if (updateIn < 1) {
				localPlayer.updateUI ();
			}
		}
		if (targetObject != null) {
			updateBuildingReadout ();
		}
	}

	public void advanceMonth() {
		if (isServer) {
			updateRent ();
			budget += revenue;
			if (monthManager.getMonth () % TAX_PERIOD == 0) { // end of year tax time
				payTaxes();
			}
			if ((budget < 0) && (owned.Count > 0)) {
				negativeMonths++;
				if (negativeMonths < NEGATIVE_MONTH_LIMIT) {
					message = "Your budget is negative. In " + (NEGATIVE_MONTH_LIMIT - negativeMonths) + " turn(s) your creditors may seize a property.";
				}
			} else {
				negativeMonths = 0;
			}
		}
		
		leaders = monthManager.SetLeaders ();
		updateUI ();
	}

	/// <summary>
	/// Raycasts a building or player and displays the data associated with the object.
	/// </summary>
	public void targetSelect() {
		
		if (targetObject != null) { // a building is currently targeted, clear its buttons
			if (targetObject is Building) {
				targetObject.GetComponent<Building>().tenant.clearButtons ();
			}
			targetObject = null;
		}

		targetPlayer = null;
		ui.readoutToggle(false);
		ui.playerReadoutToggle (false);

		Vector3 fwd = playerCamera.transform.TransformDirection(Vector3.forward); // ray shooting from camera
		RaycastHit hit;

		if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100)) {
			if (targetMods != null) {
				targetMods.clearButtons ();
			}
				
			targetObject = hit.collider.GetComponent<OwnableObject> ();
			targetPlayer   = hit.collider.GetComponent<Player> ();
			targetMods     = hit.collider.GetComponent<BuildingModifier> ();
			Resident targetResident = hit.collider.GetComponent<Resident> ();
			targetVehicle = hit.collider.GetComponentInParent<Vehicle> ();
			if (targetObject != null) {
				ui.readoutToggle (true);
				ui.updateReadout (targetObject.getReadout (gameObject.GetComponent<NetworkIdentity> ().netId));
				if (targetObject is Building) {
					Building tmpBuilding = targetObject.GetComponent<Building> ();
					ui.updateWorkers (targetObject.GetComponent<Building>().getWorkerText ());
					soundPlayer.PlayRaycastSound (tmpBuilding.type);
					if ((tmpBuilding.getOwner() == id) && !ignoredTypes.Contains(tmpBuilding.type) && !(tmpBuilding is Business)) {
						ui.setPriceToggle (true);
						button.setRentPrice (netId, tmpBuilding.netId, tmpBuilding.rent);
					} else {
						ui.setPriceToggle (false);
					}
					if (tmpBuilding.ownedBy(this) && !tmpBuilding.occupied) {
						tmpBuilding.tenant.setButtons ();
					} else if (tmpBuilding.ownedBy(this) && tmpBuilding.occupied && !tmpBuilding.tenant.isNone ()) {
						tmpBuilding.tenant.showActive ();
					}
				}

				Neighborhood n = targetObject.getNeighborhood ();
				if (n == null) {
					if (targetObject is Neighborhood) {
						ui.updateNeighborhoodName (targetObject.GetComponent<Neighborhood>().buildingName);
					} else {
						ui.updateNeighborhoodName ("No neighborhood");
					}
				} else {
					ui.updateNeighborhoodName ("Part of " + n.buildingName);
				}
			} else if (targetPlayer != null) {
				ui.playerReadoutToggle (true);
				ui.updatePlayerReadout (targetPlayer.getReadout ());
				//ui.updateWorkers ("");
			} else if (targetResident != null) {
				ui.playerReadoutToggle (true);
				ui.updatePlayerReadout (targetResident.personToString ());
			} else if (targetVehicle != null) {
				ui.playerReadoutToggle(true);
				ui.updatePlayerReadout (targetVehicle.getReadout());
			}
			else {
				ui.updateReadout ("");
				ui.setPriceToggle (false);
				ui.readoutToggle (false);
				ui.playerReadoutToggle (false);
			}
		}
	}

	public void buy() {
		if (targetObject != null) {
			CmdBuy (netId, targetObject.netId);
		} else if (targetVehicle != null) {
			CmdBuy (netId, targetVehicle.netId);
		}
	}

	public int propertyValue() {
		List<Building> props = getBuildings();
		List<Building> hoods = props.Where (b => (b is Neighborhood)).ToList();
		List<Building> lots = props.Where (b => ((b is Lot) && !b.inNeighborhood ())).ToList();
		int value = 0;
		foreach (Building b in hoods) {
			value += b.appraise ();
		}
		foreach (Building b in lots) {
			value += b.appraise ();
		}
		return value;
	}

	public void getRentComparison (NetworkInstanceId buildingId, string inputValue) {
		GameObject obj;
		int comparisonVal;
		int inputIntVal;
		int.TryParse (inputValue, out inputIntVal);

		obj = getLocalInstance (buildingId);

		if (obj != null) {
			Building b = obj.GetComponent<Building> ();
			if ((b != null) && monthManager.dictRent.ContainsKey (b.type)) {
				int marketAverage = monthManager.GetAverageRent (b.type);
				if (inputIntVal < marketAverage) {
					comparisonVal = Mathf.Abs (inputIntVal - marketAverage);
					ui.updatePriceReadout (0, comparisonVal, marketAverage);
				} else if (inputIntVal > marketAverage) {
					comparisonVal = Mathf.Abs (inputIntVal - marketAverage);
					ui.updatePriceReadout (1, comparisonVal, marketAverage);
				} else {
					ui.updatePriceReadout ("At market rate.");
				}
			} else {
				ui.updatePriceReadout ("No market data for this building type.");
			}
		} else {
			ui.updatePriceReadout ("No market data for this building type.");
		}
	}

	/// <summary>
	/// Pay taxes on owned buildings
	/// </summary>
	public void payTaxes() {
		if (isServer) {
			int tax = 0;
			foreach (NetId buildingId in owned) {
				Building b = getBuilding (buildingId.id);
				if (b != null) { //used to avoid null refs on vehicles
					if (b is Lot) {
						tax += (int)(b.baseRent * .1f);
					} else if (!(b is Neighborhood)) {
						tax += (int)(b.appraise () * .1f);
					}
				}
			}
			budget -= tax;
			if (tax > 0) {
				RpcMessage ("Paid $" + tax + " in property taxes.");
			}
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdSetup(string cName, string pName, Color playerColor) {
		companyName = cName;	
		playerName = pName;
		color = playerColor;
		setColor (playerColor);
	}

	[Command(channel=CHANNEL)]
	public void CmdBuy(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		OwnableObject b = obj.GetComponent<OwnableObject> ();
		Building building;
		Player player = p.GetComponent<Player> ();

		if (b != null) {
			building = b.GetComponent<Building> ();
			if ((b.cost <= player.budget) && !b.notForSale) {
				if (b.validLot ()) {
					player.message = "You cannot buy that without owning its lot.";
				} else if ((b is Lot) && (player.lots >= MAX_LOTS)) {
					player.message = "Add lots to neighborhoods to expand further.";
				} else {
					player.budget -= b.cost;
					if (building != null) {
						player.revenue += building.getRent ();
					}

					b.notForSale = true;
					GameObject eventLightRay = (GameObject)Resources.Load ("GodRays");
					GameObject tmp = (GameObject)Instantiate (eventLightRay, new Vector3 (b.transform.position.x, b.getHighest () + 8, b.transform.position.z), Quaternion.identity);
					tmp.GetComponent<EventLightRayColorSet> ().particleColor = player.color;
					NetworkServer.Spawn (tmp);

					if (b.getOwner () != -1) {
						Player oldOwner = b.getPlayerOwner ();
						oldOwner.budget += b.cost;
						if (building != null) {
							oldOwner.message = player.playerName + " bought " + building.buildingName + " from you for $" + b.cost;
						}
						if (b is Lot) {
							oldOwner.lots -= 1;
						}
					}
					b.setOwner (playerId);
					if (b is Lot) {
						Lot l = b.gameObject.GetComponent<Lot> ();
						l.unsetNeighborhood ();
						player.lots += 1;
					}
					monthManager.RpcUpdateColors ();
				}
			} else {
				player.SetDirtyBit(int.MaxValue);
				player.message = "You can't buy that.";
			}
		} else {
			GameObject vehicleObject = getLocalInstance (buildingId);
			Vehicle v = vehicleObject.GetComponent<Vehicle> ();
			if ((v.cost <= player.budget) && !v.notForSale) {
					player.budget -= v.cost;
					v.notForSale = true;
					GameObject eventLightRay = (GameObject)Resources.Load ("GodRays");
					GameObject tmp = (GameObject)Instantiate (eventLightRay, new Vector3 (v.transform.position.x, v.getHighest () + 8, v.transform.position.z), Quaternion.identity);
					tmp.GetComponent<EventLightRayColorSet> ().particleColor = player.color;
					NetworkServer.Spawn (tmp);

					if (v.getOwner () != -1) {
						Player oldOwner = v.getPlayerOwner ();
						oldOwner.budget += v.cost;
						oldOwner.message = player.playerName + " bought " + v.vehicleName + " from you for $" + v.cost;
					}
					v.setOwner (playerId);
					monthManager.RpcUpdateColors ();
				}
			else {
				player.SetDirtyBit(int.MaxValue);
				player.message = "You can't buy that vehicle.";
			} 
			}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdGive(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		OwnableObject b = obj.GetComponent<Building> ();
		Player player = p.GetComponent<Player> ();

		if (b != null) {
			if (b is Building) {
				player.revenue += b.GetComponent<Building>().getRent ();
			}

			b.notForSale = true;
			b.setOwner(player.netId);
			monthManager.RpcUpdateColors ();
		}
		RpcUpdateUI ();
	}

	/// <summary>
	/// Gives money to another player/company
	/// </summary>
	/// <param name="giveId">Player giving the money.</param>
	/// <param name="getId">Player/company getting the money.</param>
	/// <param name="amount">Amount of money given.</param>
	[Command(channel=CHANNEL)]
	public void CmdGiveMoney(NetworkInstanceId giveId, NetworkInstanceId getId, int amount) {
		GameObject tmpOne = getLocalInstance (giveId);
		GameObject tmpTwo = getLocalInstance (getId);

		Player giver = tmpOne.GetComponent<Player> ();
		Player getter = tmpTwo.GetComponent<Player> ();

		if (getter != null) { //person getting money is a player, not a company
			if (giver.budget >= amount) {
				giver.budget -= amount;
				getter.budget += amount;
				giver.message = "Gave $" + amount + " to " + getter.playerName + ".";
				getter.message = "Got $" + amount + " from " + giver.playerName + "!";
			} else {
				giver.message = "Not enough money.";
			} 
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdRemove(NetworkInstanceId buildingId) {
		OwnableObject b = getLocalInstance (buildingId).GetComponent<OwnableObject> ();
		if (b.getOwner () > -1) {
			Player tmp = b.getPlayerOwner ();
			tmp.owned.removeId (buildingId);
			tmp.updateRent ();
			b.unsetOwner ();
		}
		monthManager.RpcUpdateColors ();
		RpcUpdateUI ();
	}

	/// <summary>
	/// Initiates an auction on the building
	/// </summary>
	/// <param name="playerId">Player identifier.</param>
	/// <param name="buildingId">Building identifier.</param>
	[Command(channel=CHANNEL)]
	public void CmdSell(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		Building b = obj.GetComponent<Building> ();
		Player player = p.GetComponent<Player> ();

		if (b != null) {
			if (b.ownedBy(player)) {
				CmdAuction (buildingId);
			} else {
				player.message = "You can't sell that building.";
			}
		}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdRepo(NetworkInstanceId bid) {
		OwnableObject b = getLocalInstance (bid).GetComponent<OwnableObject> ();
		b.repo ();
	}

	[Command(channel=CHANNEL)]
	public void CmdRepair(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		DamageableObject b = obj.GetComponent<DamageableObject> ();
		Player player = p.GetComponent<Player> ();
		Vehicle v = obj.GetComponent<Vehicle> ();

		if (b != null) {
			if (b.fire) {
				player.message = "You can't repair that while it is on fire!";
			} else if (!b.ownedBy(player)) {
				player.message = "You don't own that!";
			} else if (b.baseCondition < 100) {
				int repairCost = b.getRepairCost ();
				int point = b.getPointRepairCost ();
				if (player.budget >= repairCost) {
					player.budget -= repairCost;
					b.repair ();
					player.message = "Object repaired for $" + repairCost + ".";
				} else if (player.budget >= point) {
					repairCost = 0;
					int numPoints = 0;
					while ((repairCost + point) <= player.budget) {
						repairCost += point;
						numPoints++;
					}

					player.budget -= repairCost;
					b.repairByPoint (numPoints);
					player.message = "Object repaired for $" + repairCost + ".";
				} else {
					player.message = "You don't have enough money to repair that.";
				}
			} else {
				player.message = "That object does not need repairs.";
			}
		}
//		if (v != null) {
//			if (v.fire) {
//				player.message = "You can't repair a burning vehicle!";
//			} else if (!v.ownedBy (player)) {
//				player.message = "You don't own that vehicle!";
//			} else if (v.condition < 100) {
//				int repairCost = v.getRepairCost ();
//				int point = v.getPointRepairCost ();
//				if (player.budget >= repairCost) {
//					player.budget -= repairCost;
//					v.repair ();
//					player.message = "Vehicle repaired for $" + repairCost + ".";
//				} else if (player.budget >= point) {
//					repairCost = 0;
//					int numPoints = 0;
//					while ((repairCost + point) <= player.budget) {
//						repairCost += point;
//						numPoints++;
//					}
//
//					player.budget -= repairCost;
//					v.repairByPoint (numPoints);
//					player.message = "Vehicle repaired for $" + repairCost + ".";
//				} else {
//					player.message = "You don't have enough money to repair that.";
//				}
//			} else {
//				player.message = "That vehicle does not need repairs.";
//			}
//		}
		RpcUpdateUI ();
	}
		
	[Command(channel=CHANNEL)]
	public void CmdSetTenant(int i, NetworkInstanceId buildingId) {
		GameObject obj;

		obj = getLocalInstance (buildingId);
		setTenant (i, obj.GetComponent<Building> ());
	}

	public void setTenant(int i, Building b) {
		if (isServer) {
			Tenant t = b.GetComponent<Tenant> ();
			b.occupied = true;

			t.setActive (t.availableTenants.ElementAt (i).id);
			t.availableTenants.Clear ();
			t.clearButtons ();

			//If a restaurant, will toggle availability of jobs immediately
			if (b is Business) {
				b.GetComponent<Business> ().addWorker (t.resident.netId);
				t.resident.leaveJob ();
				t.resident.job = b.netId;
				if (b is Restaurant) {
					b.gameObject.GetComponent<Restaurant> ().offerDeliveryJob (b.occupied);
				}
			}
			RpcUpdateUI ();
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdSetRent(string s, NetworkInstanceId buildingId) {
		int i;
		int.TryParse (s, out i);
		if (i > 0) {
			Building b = getLocalInstance (buildingId).GetComponent<Building> ();
			if (!b.occupied || !b.tenant.onLease ()) {
				b.setRent (i);
				b.tenant.availableTenants.Clear (); // tenants all stop applying when the price has changed
				RpcUpdateUI ();
			} else {
				if (b.tenant.onLease ()) {
					b.messageOwner("Can't change the rent until tenant's lease ends in " + b.tenant.getLeaseMonths() + " turn(s).");
				}
			}
		} 
	}

	/// <summary>
	/// displays the selection panel which lets the player shoose between auctioning or setting a price for a building
	/// </summary>
	/// <param name="playerId">Player identifier.</param>
	/// <param name="buildingId">Building identifier.</param>
	public void sellChoice() {
		if (targetObject != null) {
			NetworkInstanceId playerId = this.netId;
			NetworkInstanceId buildingId = targetObject.netId;
			GameObject r;
			GameObject p;
			GameObject obj;

			obj = getLocalInstance (buildingId);
			p = getLocalInstance (playerId);

			OwnableObject b = obj.GetComponent<Building> ();
			Lot l = b.getLot ();
			Player player = p.GetComponent<Player> ();

			if (b.ownedBy(player) && !busyPanel) {
				if ((l == null) || (b is Lot)) {
					busyPanel = true;
					r = (GameObject)Resources.Load ("SellSelection");
					activePanel = (GameObject)Instantiate (r);
					activePanel.transform.SetParent (ui.gameObject.transform, false);
					controlsAllowed (false);
					Button auctionButton = activePanel.transform.Find ("AuctionSell").GetComponent<Button> ();
					Button sellButton = activePanel.transform.Find ("QuickSell").GetComponent<Button> ();

					auctionButton.onClick.AddListener (delegate {
						CmdAuction (buildingId);
						Destroy (activePanel);
						controlsAllowed (true);
						busyPanel = false;
					});

					if (!b.notForSale) {
						sellButton.transform.Find ("Text").GetComponent<Text> ().text = "Take off market";
						sellButton.onClick.AddListener (delegate {
							CmdTakeOffMarket (playerId, buildingId);
							Destroy (activePanel);
							controlsAllowed (true);
							busyPanel = false;
						});
		
					} else {
						sellButton.onClick.AddListener (delegate {
							controlsAllowed (true);
							Destroy (activePanel);
							setPrice (playerId, buildingId);
							busyPanel = false;
						});
					}
				} else { // building is part of a lot and should not be sold independently
					player.showMessage ("Cannot sell part of a lot! If you wish to sell this building you must sell the entire lot.");
				}
			} else {
				if (!busyPanel) {
					player.showMessage ("You can't sell that building");
				}
			}
		}
	}

	public void setPrice(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject r;
		GameObject panel;
		r = (GameObject)Resources.Load ("CostSetPanel");
		panel = (GameObject)Instantiate (r);
		panel.transform.SetParent (ui.gameObject.transform, false);
		busyPanel = true;
		controlsAllowed (false);
		GameObject obj;

		obj = getLocalInstance (buildingId);

		OwnableObject b = obj.GetComponent <OwnableObject> ();
		InputField field = panel.transform.Find ("PriceField").GetComponent<InputField> ();
		field.text = b.cost.ToString();

		panel.transform.Find ("Submit").GetComponent<Button> ().onClick.AddListener (delegate {
			CmdSetPrice(playerId, buildingId, field.text);
			Destroy(panel);
			busyPanel = false;
			controlsAllowed(true);
		});
	}

	[Command(channel=CHANNEL)]
	public void CmdSetPrice(NetworkInstanceId playerId, NetworkInstanceId buildingId, string price) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		OwnableObject b = obj.GetComponent<OwnableObject> ();
		Player player = p.GetComponent<Player> ();

		if (b != null) {
			if (int.Parse (price) > 0) {
				b.cost = int.Parse (price);
				b.notForSale = false;
				if (b is Building) {
					RpcMessageAll (player.playerName + " is selling " + b.GetComponent<Building>().buildingName + " for " + b.cost + "!");
				}
			} else {
				player.message = "Price must be positive";
			}
		}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdTakeOffMarket(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);

		OwnableObject b = obj.GetComponent<Building> ();
		Player player = p.GetComponent<Player> ();

		if (b.ownedBy(player)) {
			b.notForSale = true;
		}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdAddExperience(NetworkInstanceId playerId, int xp) {
		GameObject p;
		p = getLocalInstance (playerId);
		Player player = p.GetComponent<Player> ();
		player.playerExperience += xp;
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdAddMoney(NetworkInstanceId playerId, int money) {
		GameObject p;
		p = getLocalInstance (playerId);
		Player player = p.GetComponent<Player> ();
		if (player != null) {
			player.budget += money;
		} 
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdToggleManager(NetworkInstanceId playerId, NetworkInstanceId hoodId, bool b) {
		Player p = getLocalInstance (playerId).GetComponent<Player> ();
		Neighborhood n = getLocalInstance (hoodId).GetComponent<Neighborhood> ();

		if (n.ownedBy (playerId)) {
			n.setManager (b);
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdHirePlayer(NetworkInstanceId targetPlayerId, NetworkInstanceId playerId, int money) {
		GameObject p;
		GameObject tp;

		tp = getLocalInstance (targetPlayerId);
		p = getLocalInstance (playerId);

		Player targetPlayer = tp.GetComponent<Player> ();
		Player player = p.GetComponent<Player> ();

		targetPlayer.budget += money;
		player.budget -= money;
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdFoundOffice(NetworkInstanceId playerId, NetworkInstanceId buildingId) {
		CmdSpawnCrime ();

		GameObject obj;
		GameObject p;

		obj = getLocalInstance (buildingId);
		p = getLocalInstance (playerId);
		Building b = obj.GetComponent<Building> ();
		Player player = p.GetComponent<Player> ();

		if (b != null && b.getOwner() == player.id) {
			b.officeName = "Office of " + companyName;
		} 
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdSpawnCrime() { 
		Vector3 spawnVector = new Vector3 (21, 2, -43);
		GameObject criminal = (GameObject)Resources.Load ("defaultEnemy");
		GameObject tmp = (GameObject)Instantiate (criminal, spawnVector, criminal.transform.rotation);
		NetworkServer.Spawn (tmp);
	}

	[Command(channel=CHANNEL)]
	public void CmdAuction(NetworkInstanceId buildingId) {
		AuctionManager auction = FindObjectOfType<AuctionManager> ();
		if (auction == null) {
			GameObject obj;
			obj = getLocalInstance (buildingId);

			Building b = obj.GetComponent<Building> ();
			if (!b.onAuction) {
				b.onAuction = true;
				GameObject tmp = (GameObject)Instantiate (Resources.Load ("AuctionPanel"));
				tmp.GetComponent<AuctionManager> ().setBuilding (buildingId);
				NetworkServer.Spawn (tmp);

				RpcAuctionPrep (buildingId);
			}
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdAdd100(NetworkInstanceId auctionID, NetworkInstanceId playerId) {
		GameObject obj;
		GameObject p;
		obj = getLocalInstance (auctionID);
		p = getLocalInstance (playerId);

		AuctionManager auction = obj.GetComponent<AuctionManager> ();
		Player player = p.GetComponent<Player> ();

		if (player.budget > (auction.currentBid + 100)) {
			auction.currentBid += 100;
			auction.leaderName = playerName;
			auction.leaderId = playerId;
			auction.resetTime ();
		} else {
			player.message = "Not enough money!";
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdAdd1000(NetworkInstanceId auctionID, NetworkInstanceId playerId) {
		GameObject obj;
		GameObject p;

		obj = getLocalInstance (auctionID);
		p = getLocalInstance (playerId);

		AuctionManager auction = obj.GetComponent<AuctionManager> ();
		Player player = p.GetComponent<Player> ();

		if (player.budget >= (auction.currentBid + 1000)) {
			auction.currentBid += 1000;
			auction.leaderName = playerName;
			auction.leaderId = playerId;
			auction.resetTime ();
		} else {
			player.message = "Not enough money!";
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdAdd10000(NetworkInstanceId auctionID, NetworkInstanceId playerId) {
		GameObject obj;
		GameObject p;
		obj = getLocalInstance (auctionID);
		p = getLocalInstance (playerId);

		AuctionManager auction = obj.GetComponent<AuctionManager> ();
		Player player = p.GetComponent<Player> ();

		if (player.budget >= (auction.currentBid + 10000)) {
			auction.currentBid += 10000;
			auction.leaderName = playerName;
			auction.leaderId = playerId;
			auction.resetTime ();
		} else {
			player.message = "Not enough money!";
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdRemoveMod(string m, int cost, NetworkInstanceId playerId, NetworkInstanceId mm) {
		GameObject obj;
		GameObject p;
		obj = getLocalInstance (mm);
		p = getLocalInstance (playerId);

		BuildingModifier bm = obj.GetComponent<BuildingModifier> ();
		Player player = p.GetComponent<Player> ();

		if (player.budget >= cost) {
			bm.removeMod (m);
			player.budget -= cost;
			player.message = "Spent $" + cost + " on renovations.";
		} else {
			player.message = "You don't have enough money to do that.";
		}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdEvict(NetworkInstanceId playerId, NetworkInstanceId mm) {
		GameObject obj;
		GameObject p;
		obj = getLocalInstance (mm);
		p = getLocalInstance (playerId);

		Building b = obj.GetComponent<Building> ();
		Player player = p.GetComponent<Player> ();

		if ((b != null) && b.ownedBy(player) && b.occupied) {
			Tenant tmp = b.GetComponent<Tenant> ();
			player.budget -= (b.baseRent * 2);
			player.message = tmp.resident.residentName + " has been evicted from " + b.buildingName + "!";
			tmp.evict ();
			//If a restaurant, will toggle availability of jobs immediately
			if (obj.gameObject.GetComponent <Restaurant> () != null) {
				obj.gameObject.GetComponent<Restaurant> ().offerDeliveryJob (b.occupied);
			}
		} else {
			player.message = "You cannot evict the tenant from that building";
		}
		RpcUpdateUI ();
	}

	[Command(channel=CHANNEL)]
	public void CmdMessage(NetworkInstanceId playerId, string s) {
		GameObject p;
		p = getLocalInstance (playerId);
			
		Player player = p.GetComponent<Player> ();

		player.message = s;
	}

	[Command(channel=CHANNEL)]
	public void CmdFormNeighborhood(NetworkInstanceId playerId, NetworkInstanceId lotId, string name) {
		Player player = getLocalInstance (playerId).GetComponent<Player> ();
		Lot l = getLocalInstance (lotId).GetComponent<Lot> ();

		if ((l != null) && l.ownedBy(playerId) && !l.inNeighborhood()) {
			GameObject n = (GameObject)Instantiate (Resources.Load ("Neighborhood"), l.transform.position, l.transform.rotation);
			Neighborhood tmp = n.GetComponent<Neighborhood> ();
			n.transform.position = new Vector3 (n.transform.position.x, n.transform.position.y + 25, n.transform.position.z);
			tmp.setName (name);
			NetworkServer.Spawn (n);
			tmp.addLot (l);
			tmp.setOwner (playerId);
			player.lots -= 1;
			RpcTimedUpdate ();
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdAddToNeighborhood(NetworkInstanceId playerId, NetworkInstanceId lotId, NetworkInstanceId neighborhoodId) {
		Player player = getLocalInstance (playerId).GetComponent<Player> ();
		Lot l = getLocalInstance (lotId).GetComponent<Lot> ();
		Neighborhood n = getLocalInstance (neighborhoodId).GetComponent<Neighborhood> ();

		if ((l != null) && (n != null) && l.ownedBy (playerId) && n.ownedBy (playerId)) {
			if (l.inNeighborhood ()) {
				player.message = "That lot is already part of a neighborhood.";
			} else {
				n.addLot (l);
				lots -= 1;
				RpcTimedUpdate ();
			}
		}
	}

	[Command(channel=CHANNEL)]
	public void CmdRemoveFromNeighborhood(NetworkInstanceId playerId, NetworkInstanceId lotId) {
		Player player = getLocalInstance (playerId).GetComponent<Player> ();
		Lot l = getLocalInstance (lotId).GetComponent<Lot> ();
		if ((l != null) && l.ownedBy (playerId)) {
			l.unsetNeighborhood ();
		}
		RpcTimedUpdate ();
	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcUpdateUI() {
		localPlayer.updateUI ();
	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcTimedUpdate() {
		localPlayer.updateIn = 15;
	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcAuctionPrep(NetworkInstanceId buildingId) {
		Player[] players = GameObject.FindObjectsOfType<Player> ();

		foreach(Player p in players) {
			if (p.isLocalPlayer)
				p.setButtons ();
		}

	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcMessageAll(string s) {
		GameObject error = (GameObject)Instantiate (Resources.Load ("ErrorPanel"));
		error.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		Text tmp = error.transform.Find ("ErrorMessage").GetComponent<Text> ();
		tmp.text = s;
	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcMessage(string s) {
		if (isLocalPlayer) {
			showMessage (s);
		}
	}

	[ClientRpc(channel=CHANNEL)]
	public void RpcSoundPlayer(string keyword) {
		if (keyword == "CantBuy" && isLocalPlayer) {
			soundPlayer.CantBuy ();
		}
		else if (keyword == "BuySound" && isLocalPlayer) {
			soundPlayer.PlayBuy ();
		}
	}

	public void setButtons() {
		AuctionManager auction = GameObject.FindObjectOfType<AuctionManager> ();
		auction.setButtons (gameObject.GetComponent<NetworkIdentity> ().netId);
	}

	public void updateUI()
	{
		string s = "No owned buildings.";
		if (owned.Count > 0) {
			s = "Owned Properties:";
		}

		if (ui != null) {
			ui.clearButtons ();
			ui.updateLedger (s);

			if (targetObject != null) {
				ui.updateReadout (targetObject.getReadout (gameObject.GetComponent<NetworkIdentity> ().netId));
				Neighborhood n = targetObject.getNeighborhood ();
				if (n == null) {
					if (targetObject is Neighborhood) {
						ui.updateNeighborhoodName (targetObject.GetComponent<Neighborhood>().buildingName);
					} else {
						ui.updateNeighborhoodName ("No neighborhood");
					}
				} else {
					ui.updateNeighborhoodName ("Part of " + n.buildingName);
				}
				if (targetObject is Building) {
					Building targetBuilding = targetObject.GetComponent<Building> ();
					targetBuilding.tenant.clearButtons ();
					if (targetBuilding.ownedBy (this)) {
						targetBuilding.tenant.showActive ();
						if (!targetBuilding.occupied) {
							targetBuilding.tenant.setButtons ();
						}
					}
				}
			} else if (targetPlayer != null) {
				ui.updateReadout (targetPlayer.getReadout ());
			} else {
				ui.readoutToggle (false);
			}

			updateBudget (budget);
			updateRevenue (revenue);
			spawnNotifications ();
			ui.updateExperience ("Experience: " + playerExperience.ToString ());
			ui.updateLeaderBoard (leaders);
		}
	}

	public void updateBuildingReadout() {
		ui.updateReadout (targetObject.getReadoutText (gameObject.GetComponent<NetworkIdentity> ().netId));
	}

	/// <summary>
	/// Calculates the player's net worth
	/// </summary>
	/// <returns>The player's net worth.</returns>
	public int netWorth() {
		int networth = budget + (revenue * 12);
		return networth;
	}

	public List<Neighborhood> getNeighborhoods() {
		List<Neighborhood> n = new List<Neighborhood> ();
		foreach (NetId id in owned) {
			Building b = getBuilding (id.id);
			if (b is Neighborhood) {
				n.Add (b.GetComponent<Neighborhood>());
			}
		}
		return n;
	}

	/// <summary>
	/// Returns data associated with the player as a string
	/// </summary>
	/// <returns>String of player data formatted for the readout panel.</returns>
	public string getReadout() {
		string bud = moneyFormat("\nBudget: ", budget);
		string r = moneyFormat("\nRevenue: ", revenue);
		string net = moneyFormat("\nNet Worth: ", netWorth());

		string s = "Player Name: " + playerName + bud + r + net + "\nCompany Affiliation:\n" + companyName + "\nExperience: " + playerExperience + "\nUnincorporated Lots: " + lots + "\n\nOwned Buildings:";

		foreach (NetId buildingId in owned) {
			Building b = getBuilding (buildingId.id);
			if (b != null) {
				s += "\n\t" + b.buildingName;
			}
		}

		return s;
	}

	public string getName() {
		return companyName;
	}

	/// <summary>
	/// Issues a popup to the player.
	/// </summary>
	/// <param name="s">Popup message.</param>
	public void showMessage(string s) {
		if (ui != null) {
			GameObject error = (GameObject)Instantiate (Resources.Load ("ErrorPanel"));
			error.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			Text tmp = error.transform.Find ("ErrorMessage").GetComponent<Text> ();
			tmp.text = s;
			updateUI ();
		}
	}
		
	/// <summary>
	/// Count up the rent for all the buildings the player owns
	/// </summary>
	public void updateRent() {
		if (isServer) {
			revenue = 0;
			foreach (NetId buildingId in owned) {
				Building b = getBuilding (buildingId.id);
				if (b != null) { //prevents null ref on vehicles and non-buildings
					revenue += b.getRent ();
					revenue -= b.upkeep;
				}
			}
			updateRevenue (revenue);
		}
	}

	/// <summary>
	/// Updates the ui budget element.
	/// </summary>
	/// <param name="b">The budget #.</param>
	private void updateBudget(int b) {
		budget = b;
		if (ui != null) {
			ui.updateBudget(moneyFormat("Budget: ", budget));
		}
	}


	/// <summary>
	/// Updates the revenue.
	/// </summary>
	/// <param name="r">revenue.</param>
	private void updateRevenue(int r) {
		revenue = r;
		if (ui != null) {
			ui.updateRevenue(moneyFormat("Revenue: ", revenue));
		}
	}
		
	/// <summary>
	/// Updates the ui xp element.
	/// </summary>
	/// <param name="b">The xp.</param>
	private void updateExperience(int xp) {
		playerExperience = xp;
		if (ui != null) {
			ui.updateExperience("Experience: " + playerExperience);
		}
	}

	/// <summary>
	/// Formats a number with the negative sign in the appropriate place
	/// (before the $)
	/// </summary>
	/// <returns>Formatted string.</returns>
	/// <param name="label">String before the $.</param>
	/// <param name="money">The money number to format.</param>
	private string moneyFormat(string label, int money) {
		string s = label;
		if (money >= 0) {
			s += "$" + money;
		} else {
			s += "-$" + Mathf.Abs (money);
		}
		return s;
	}

	private GameObject getLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}

	public void spawnNotifications() {
		float x = 0; // x coordinate where the notification will be spawns: increment by width for each
		float y = 0; 
		float width = 32; // width of the notification
		GameObject notification = (GameObject)Resources.Load ("PersistNotification");
		foreach (GameObject g in notifications) { // clear out last month's notifications
			g.GetComponent<PersistNotification> ().buttonDestroy ();
		}
		notifications.Clear ();
		if (revenue < 0) {
			spawnNotification (notification, "Your revenue is negative.", Color.red, 21, null, x, y);
			x += width;
		}
			
		foreach (NetId buildingId in owned) {
			Building b = getBuilding (buildingId.id);
			if (b != null && !ignoredTypes.Contains (b.type)) { //null check prevents null ref on Vehicle class
				if (b.fire) {
					spawnNotification (notification, b.buildingName + " is on fire!", Color.red, 3, b, x, y);
					x += width;
				}
				if (!b.occupied && !b.ruin) { // no occupant, but don't bother notifying them if its an uninhabitable ruin
					spawnNotification (notification, b.buildingName + " does not have a tenant.", Color.white, 60, b, x, y);
					x += width;
				} else if (b.tenant.resident != null) {
					if (b.tenant.resident.criminal) {
						spawnNotification (notification, "The tenant at " + b.buildingName + " has been involved with criminal activity.", Color.red, 53, b, x, y);
						x += width;
					}
					if (!b.tenant.resident.employed() && b.isOccupied ()) {
						spawnNotification (notification, "The tenant at " + b.buildingName + " does not have a job.", Color.white, 22, b, x, y);
						x += width;
					}
				}
			}
		}
		foreach (Building b in deliveryDestinations) { // spawn a notification for each building with an active delivery
			spawnNotification (notification, "You have a delivery for " + b.buildingName + ".", Color.green, 36, b, x, y);
			x += width;
		}
	}

	public void spawnNotification(GameObject notification, string message, Color c, int sprite, Building b, float x, float y) {
		GameObject tmp = (GameObject)Instantiate (notification, new Vector3(notification.transform.position.x + x, notification.transform.position.y + y, notification.transform.position.z), Quaternion.identity);
		tmp.transform.SetParent (GameObject.Find("Canvas").transform, false);
		PersistNotification p = tmp.GetComponent<PersistNotification> ();
		p.message = message;
		p.setImage(notificationSprites[sprite], c);
		if (b != null) {
			Building beaconBuilding = b;
			tmp.GetComponent<Button> ().onClick.AddListener (delegate {
				overlandMap.makeBeacon (beaconBuilding);
				destinationBuilding = beaconBuilding.id;
			});
		}
		notifications.Add (tmp);
	}

	public void targetNeighborhood() {
		if (!(targetObject is Neighborhood)) {
			Neighborhood n = targetObject.getNeighborhood ();
			if (n != null) {
				targetObject = n;
			}
		}
		updateUI ();
	}

	private void formNeighborhood(OwnableObject target) {
		if (target != null) {
			Lot l = target.GetComponent<Lot> ();
			if ((l != null) && target.ownedBy(netId)) {
				if (l.inNeighborhood ()) {
					showMessage ("That lot is already in a neighborhood.");
				} else {
					busyPanel = true;
					characterController.setMovementEnabled (false);
					characterController.setMouseLookEnabled (false);
					activePanel = (GameObject)Instantiate (Resources.Load ("NeighborhoodPanel"));
					activePanel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
					activePanel.transform.Find ("HoodNameField").GetComponent<InputField> ().text = Neighborhood.nameGen ();
					activePanel.transform.Find ("Submit").GetComponent<Button> ().onClick.AddListener (delegate {
						string nName = activePanel.transform.Find ("HoodNameField").GetComponent<InputField> ().text;
						if (string.IsNullOrEmpty (nName)) {
							showMessage ("Enter a valid name for the neighborhood.");
						} else {
							CmdFormNeighborhood (this.netId, l.netId, nName);
							characterController.setMovementEnabled (true);
							characterController.setMouseLookEnabled (true);
							Destroy (activePanel);
							busyPanel = false;
						}
					});
				}
			} else {
				showMessage ("You must select a lot you own to form a neighborhood!");
			}
		}
	}

	private void addToNeighborhood(OwnableObject target) {
		if (target != null && target is Lot && target.ownedBy(netId)) {
			if (target.getNeighborhood () == null) {
//				NetworkInstanceId tmp = owned.Where (b => (getBuilding (b.id) is Neighborhood)).ToList () [0].id;
//				Neighborhood n = getLocalInstance (tmp).GetComponent<Neighborhood> ();
//				CmdAddToNeighborhood (netId, target.netId, n.netId);
				GameObject tmp = (GameObject)Instantiate(Resources.Load("NeighborhoodChoice"));
				tmp.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			} else {
				CmdRemoveFromNeighborhood (netId, target.netId);
			}
		}
	}

	private Building getBuilding(NetworkInstanceId id) {
		GameObject tmp = getLocalInstance (id);
		if (tmp != null) {
			Building b = tmp.GetComponent<Building> ();
			return b;
		} else {
			return null;
		}
	}

	/// <summary>
	/// Gets the buildings that the player owns.
	/// </summary>
	/// <returns>list of buildings.</returns>
	public List<Building> getBuildings() {
		List<Building> buildings = new List<Building> ();
		foreach (NetId tmp in owned) {
			Building tmpBuilding = getBuilding (tmp.id);
			if (tmpBuilding != null) {
				buildings.Add (tmpBuilding);
			}
		}
		return buildings;
	}

	/// <summary>
	/// Sets the busyPanel variable to false, permitting extra panels to be spawned
	/// </summary>
	public void closePanel() {
		busyPanel = false;
	}


	private void spawnGiveMoneyPanel() {
		if (!busyPanel && (targetPlayer != null) && (targetPlayer.id != id)) {
			busyPanel = true;
			GameObject tmp = (GameObject)Instantiate (Resources.Load ("GiveMoneyPanel"));
			tmp.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			tmp.GetComponent<SendMoney> ().setTarget (targetPlayer.gameObject);
		}
	}

	private GameObject spawnSetupPanel() {
		GameObject activePanel = (GameObject)Instantiate (Resources.Load ("PlayerPanel"));
		activePanel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		Slider red = activePanel.transform.Find("Red").GetComponent<Slider>();
		Slider green = activePanel.transform.Find("Green").GetComponent<Slider>();
		Slider blue = activePanel.transform.Find("Blue").GetComponent<Slider>();

		Image color = activePanel.transform.Find("ColorSample").GetComponent<Image>();
		red.value = Random.Range (0, .5f);
		green.value = Random.Range (0, .5f);
		blue.value = Random.Range (0, .5f);

		color.color = new Color (red.value, green.value, blue.value);
		red.onValueChanged.AddListener (delegate {
			color.color = new Color(red.value, green.value, blue.value);
		});
		green.onValueChanged.AddListener (delegate {
			color.color = new Color(red.value, green.value, blue.value);
		});
		blue.onValueChanged.AddListener (delegate {
			color.color = new Color(red.value, green.value, blue.value);
		});
		activePanel.transform.Find ("Submit").GetComponent<Button> ().onClick.AddListener (delegate {
			string cName = activePanel.transform.Find("CompanyNameField").GetComponent<InputField>().text;
			string pName = activePanel.transform.Find("PlayerNameField").GetComponent<InputField>().text;
			Color myColor = activePanel.transform.Find("ColorSample").GetComponent<Image>().color;
			if (string.IsNullOrEmpty(cName) || string.IsNullOrEmpty(pName)) {
				showMessage("Enter a valid company name and player name.");
			} else {
				CmdSetup(cName, pName, myColor);
				controlsAllowed(true);
				GameObject.Find("Canvas").transform.Find ("ChatUI").GetComponent<bl_ChatUI> ().SetPlayerName (pName);
				Destroy(activePanel);
			}
		});
		return activePanel;
	}

	private void repoMan() {
		bankruptChoice = (GameObject)Instantiate (Resources.Load ("BankruptcyPanel"));
	}

	private void setColor(Color c) {
		color = c;
		playerMapMarker = gameObject.transform.Find ("MapMarker").gameObject;
		playerMapMarker.GetComponent<MeshRenderer> ().material.color = c;
	}

	public void controlsAllowed(bool b){
		characterController.setMovementEnabled (b);
		characterController.setMouseLookEnabled (b);
	}

	private void toggleManager() {
		if (targetObject != null) {
			if (targetObject is Neighborhood) {
				Neighborhood n = targetObject.GetComponent<Neighborhood> ();
				if (n.isManaged()) {
					CmdToggleManager (netId, n.netId, false);
					showMessage (n.buildingName + " no longer has a manager.");
				} else {
					controlsAllowed (false);
					busyPanel = true;
					GameObject confirmation = (GameObject)Instantiate (Resources.Load ("Confirm"));
					confirmation.transform.SetParent (GameObject.Find ("Canvas").transform, false);
					confirmation.transform.Find("ConfirmMessage").GetComponent<Text>().text = "Assign a manager to this neighborhood? It will cost $" + n.getManagerSalary() + " per turn";
					confirmation.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
						busyPanel = false;
						CmdToggleManager(netId, n.netId, true);
						controlsAllowed(true);
						Destroy(confirmation);
					});
					confirmation.transform.Find ("Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
						busyPanel = false;
						controlsAllowed(true);
						Destroy(confirmation);
					});
				}
			}
		}
	}





	/* VEHICLE / DRIVING METHODS */

	/// <summary>
	/// Toggles the vehicle controls on the Player object to permit control of vehicles
	/// </summary>
	/// <param name="enabled">If set to <c>true</c> enabled.</param>
	public void ToggleVehicleControls (bool enabled, NetworkInstanceId netId) {
		VehicleControls vc = GetComponent<VehicleControls> ();
		Vehicle v = getLocalInstance (netId).GetComponent<Vehicle> ();
		//vc.m_Car = v.GetComponent<CarController> ();
		if (isServer) {
			currentVehicle = v.GetComponent<CarController> ();
		} else {
			CmdSetCurrentVehicle (netId);
		}
		vc.enabled = enabled;
	}

	/// <summary>
	/// Command for setting the currentVehicle var in the player class. Ensures that the VehicleControls
	/// has the CarController data in time for when it's enabled.
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	[Command]
	public void CmdSetCurrentVehicle (NetworkInstanceId netId) {
		CarController v = getLocalInstance (netId).GetComponent<Vehicle> ().GetComponent<CarController>();
		currentVehicle = v;
	}


	/// <summary>
	/// Command for toggling visibility syncvar. Linked to Syncvar Hook TogglePlayervisibility.
	/// </summary>
	/// <param name="netId">Network identifier.</param>
	/// <param name="visible">If set to <c>true</c> visible.</param>
	[Command]
	public void CmdSetPlayerVisibility (NetworkInstanceId netId, bool visible)
	{
		playerNotVisible = visible;
	}

	/// <summary>
	/// Utility function for hiding the player. Called from syncvar hook playerNotVisible
	/// </summary>
	/// <param name="active">If set to <c>true</c> active.</param>
	private void togglePlayerVisibility (bool active)
	{
		playerNotVisible = active;
		Renderer[] rends = gameObject.GetComponentsInChildren<Renderer> ();
		if (playerNotVisible) {
			gameObject.GetComponent<Rigidbody> ().isKinematic = true;
			gameObject.GetComponent<CapsuleCollider> ().enabled = false;
			foreach (Renderer r in rends) {
				r.enabled = false;
			}
			if (isLocalPlayer) {
				gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = false;
			}
		} else {
			gameObject.GetComponent<Rigidbody> ().isKinematic = false;
			gameObject.GetComponent<Collider> ().enabled = true;
			foreach (Renderer r in rends) {
				r.enabled = true;
			}
			if (isLocalPlayer) {
				gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = true;
			}
		}
	}


	/// <summary>
	/// Command for toggling the eligibility of the player to exit a vehicle.
	/// Syncvar on player, set by the vehicle coroutine that delays immediate entrance/exit
	/// of vehicle to prevent glitching (switching in and out by holding down enter/exit key)
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	/// <param name="eligible">If set to <c>true</c> eligible.</param>
	[Command]
	public void CmdSetPlayerEligibilityToExit (NetworkInstanceId netId, bool eligible)
	{
		eligibleToExitVehicle = eligible;
	}


	/// <summary>
	/// Command to set the vehicleOccupied bool. Used in update functions in Vehicle.
	/// </summary>
	/// <param name="netId">NetworkInstance ID</param>
	/// <param name="occupied">If set to <c>true</c> occupied.</param>
	[Command]
	public void CmdSetVehicleOccupied (NetworkInstanceId netId, bool occupied) {
		Vehicle v = getLocalInstance (netId).GetComponent<Vehicle>();
		v.vehicleOccupied = occupied;
	}


	/// <summary>
	/// Command to set a new parent for the player gameobject.
	/// Called from vehicle class for player to 'enter' door transform on the vehicle
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	/// <param name="parenting">If set to <c>true</c> parenting.</param>
	[Command]
	public void CmdSetNewParent (NetworkInstanceId netId, bool parenting) {
		if (parenting) {
			Transform t = getLocalInstance (netId).transform;
			gameObject.transform.SetParent (t);
			RpcSetNewParent (netId, true);
		} else {
			gameObject.transform.SetParent (null);
		}
	}


	/// <summary>
	/// Rpc to set a new parent for the player gameobject.
	/// Called from vehicle class for player to 'enter' door transform on the vehicle
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	/// <param name="parenting">If set to <c>true</c> parenting.</param>
	[ClientRpc]
	public void RpcSetNewParent (NetworkInstanceId netId, bool parenting) {
		if (parenting) {
			Transform t = getLocalInstance (netId).transform;
			gameObject.transform.SetParent (t);
		} else {
			gameObject.transform.SetParent (null);
		}
	}


	/// <summary>
	/// Command that enables client to send Rigidbody Force movement data to a vehicle
	/// </summary>
	/// <param name="h">horizontal axis</param>
	/// <param name="v">vertical axis</param>
	/// <param name="ve">velocity</param>
	/// <param name="hb">handbrake</param>
	[Command]
	public void CmdDrive (float h, float v, float ve, float hb) {
		currentVehicle.Move (h, v, ve, hb);
	}


}