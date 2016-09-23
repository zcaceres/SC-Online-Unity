using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ConstructionController : NetworkBehaviour {
	const int CHANNEL = 1;

	private struct Spawnable {
		public Object dummy;
		// dummy used for placement
		public Object spawnable;
		// the object the network will spawn
		public int price;
		// cost to spawn
		public int priceLevel;
		// 0 low income, 1 mid income, 2 high income
		public int buildingType;
		// building type as int
		public int attractiveness;
		// attractiveness effect
		public string name;
		// name of the building prefab
		public string description;
		// short description of the building


		public Spawnable (Object dum, Object spawn, int cost, int level, int type, int a, string n, string d) {
			dummy = dum;
			spawnable = spawn;
			price = cost;
			priceLevel = level;
			buildingType = type;
			attractiveness = a;
			name = n;
			description = d;
		}
	}

	private Player player;
	private Quaternion constructionRotation;
	private List<Spawnable> spawnables;
	private int currentSpawnable;
	private GameObject toBuild;
	private GameObject playerCamera;
	private GameObject tooltip;
	private GameObject confirm;
	private bool readyToConstruct;
	private int targetBuilding;
	private LayerMask layerMask;
	private MonthManager monthManager;
	private NetworkManager nm;

	// Use this for initialization
	void Start () {
		nm = FindObjectOfType<NetworkManager> ();
		layerMask = ~(1 << LayerMask.NameToLayer ("player") | 1 << LayerMask.NameToLayer ("node") | 1 << LayerMask.NameToLayer ("Ignore Raycast") | 1 << LayerMask.NameToLayer("trail"));
		spawnables = new List<Spawnable> ();
		Spawnable tmp;

		string[] lines = System.IO.File.ReadAllLines (@"Assets\constructables\constructables_list.txt");
		foreach (string s in lines) {
			if (!s.StartsWith("//")) {
				string[] values = s.Split (',');
				if (values.Length > 7) {
					Object dummy = Resources.Load (values [0].Trim());
					Object prefab = Resources.Load (values [1].Trim());
					int cost = int.Parse (values [2]);
					int level = int.Parse (values [3]);
					int type = int.Parse (values [4]);
					int attr = int.Parse (values [5]);
					string name = values [6].Trim();
					string desc = values [7].Trim();
					tmp = new Spawnable (dummy, prefab, cost, level, type, attr, name, desc); 
					spawnables.Add (tmp);
				}
			}
		}

//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_DirtPath"), Resources.Load ("ConstructableBuildings/DirtPath"), 5, 0, 23, -3, "Dirt Path", "A cheap path. All buildings must connect to road or sidewalk!");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Sidewalk"), Resources.Load ("ConstructableBuildings/Sidewalk"), 50, 0, 22, 0, "Sidewalk", "A nice path. All buildings must connect to road or sidewalk!");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Trailer"), Resources.Load ("ConstructableBuildings/Trailer"), 900, 0, 19, -15, "Trailer", "Cheap housing on wheels.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Trailer (Square)"), Resources.Load ("ConstructableBuildings/Trailer (Square)"), 900, 0, 19, -15, "Trailer", "Cheap housing on wheels.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building2A"), Resources.Load ("ConstructableBuildings/Building2A"), 6000, 1, 10, -15, "Small Apartment Building", "A small apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building2B"), Resources.Load ("ConstructableBuildings/Building2B"), 6000, 1, 10, -15, "Small Apartment Building", "A small apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3A"), Resources.Load ("ConstructableBuildings/Building3A"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3B"), Resources.Load ("ConstructableBuildings/Building3B"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3C"), Resources.Load ("ConstructableBuildings/Building3C"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3D"), Resources.Load ("ConstructableBuildings/Building3D"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3E"), Resources.Load ("ConstructableBuildings/Building3E"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3F"), Resources.Load ("ConstructableBuildings/Building3F"), 7500, 1, 11, -20, "Apartment Building", "A mid-level apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3G"), Resources.Load ("ConstructableBuildings/Building3G"), 2000, 1, 9, -10, "Tenement Building", "A cheap apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3H"), Resources.Load ("ConstructableBuildings/Building3H"), 2000, 1, 9, -10, "Tenement Building", "A cheap apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Building3I"), Resources.Load ("ConstructableBuildings/Building3I"), 2000, 1, 9, -10, "Tenement Building", "A cheap apartment building.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Billboard"), Resources.Load ("ConstructableBuildings/Billboard"), 4000, 1, 15, -30, "Billboard", "A billboard used for advertising.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Dinner"), Resources.Load ("ConstructableBuildings/Dinner01"), 7500, 1, 3, -20, "Diner", "A commercial restaurant property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_CheapRestaurant"), Resources.Load ("ConstructableBuildings/CheapRestaurant"), 2500, 0, 3, -30, "Cheap Restaurant", "A low-end commercial property. Employs 3.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_HardwareStore"), Resources.Load ("ConstructableBuildings/HardwareStore"), 5000, 0, 4, -10, "Hardware Store", "A low-end commercial property. Employs 8");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_HouseA"), Resources.Load ("ConstructableBuildings/HouseA"), 2000, 0, 1, 0, "House", "A small residential property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_HouseB"), Resources.Load ("ConstructableBuildings/HouseB"), 2000, 0, 1, 0, "House", "A small residential property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_Mart"), Resources.Load ("ConstructableBuildings/Mart"), 15000, 2, 8, -30, "Supermart", "A large commercial property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_PlazaA"), Resources.Load ("ConstructableBuildings/PlazaA"), 15000, 2, 12, -30, "Plaza", "A large residential property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_PlazaB"), Resources.Load ("ConstructableBuildings/PlazaB"), 15000, 2, 12, -30, "Plaza", "A large residential property.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_TreeA"), Resources.Load ("ConstructableBuildings/TreeA"), 500, 0, 18, 10, "Tree", "A bit of greenery.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_TreeB"), Resources.Load ("ConstructableBuildings/TreeB"), 500, 0, 18, 10, "Tree", "A bit of greenery.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_TreeC"), Resources.Load ("ConstructableBuildings/TreeC"), 500, 0, 18, 10, "Tree", "A bit of greenery.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_WoodenFence"), Resources.Load ("ConstructableBuildings/WoodenFence"), 100, 0, 18, 1, "Wooden Fence", "Mark the edges of your lot.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_BrickWall"), Resources.Load ("ConstructableBuildings/BrickWall"), 300, 0, 18, 1, "Brick Wall", "For privacy...");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_MetalFence"), Resources.Load ("ConstructableBuildings/MetalFence"), 200, 0, 18, -1, "Metal Fence", "For keeping out the riff-raff.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_ParkBench"), Resources.Load ("ConstructableBuildings/ParkBench"), 100, 0, 18, 5, "Park Bench", "A nice place to sit.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_StreetLamp"), Resources.Load ("ConstructableBuildings/StreetLamp"), 400, 0, 18, 10, "Street Lamp", "A pretty light at night.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_StreetLight"), Resources.Load ("ConstructableBuildings/StreetLight"), 400, 0, 18, 10, "Street Light", "A tall street light.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_SecurityGuard"), Resources.Load ("ConstructableBuildings/SecurityGuard"), 400, 0, 24, -5, "Hire a Security Guard", "Scares away criminals. Costs $100 a month.");
//		spawnables.Add (tmp);
//		tmp = new Spawnable (Resources.Load ("DummyBuildings/Dummy_FireHydrant"), Resources.Load ("ConstructableBuildings/FireHydrant"), 500, 0, 24, -5, "Fire Hydrant", "Stand near this hydrant to put out fires.");
//		spawnables.Add (tmp);

		player = GetComponent<Player> ();
		constructionRotation = Quaternion.identity;
		playerCamera = gameObject.transform.Find ("MainCamera").gameObject;
		currentSpawnable = 0;
		targetBuilding = -1;
		monthManager = GameObject.Find ("Clock").GetComponent<MonthManager> ();

	}

	private GameObject getLocalInstance (NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}

	private void setTooltip () {
		Spawnable current = spawnables [currentSpawnable % spawnables.Count];

		string pretty = "";
		if (current.attractiveness > 0) { // green for positive prettiness
			pretty += "<color=#00ff00ff>" + current.attractiveness + "</color>";
		} else if (current.attractiveness < 0) { // red for negative
			pretty += "<color=#ff0000ff>" + current.attractiveness + "</color>";
		} else { // no color for 0
			pretty += current.attractiveness;
		}
		string s = "Cost to build: $" + current.price + "\n" + current.name + "\n" + current.description + "\nAttractiveness Effect: " + pretty;
		if (monthManager.dictRent.ContainsKey (current.buildingType)) {
			s += "\n" + "Current market rent for type: " + monthManager.GetAverageRent (current.buildingType);
		} else {
			s += "\n" + "No market data for this building.";
		}
		tooltip.transform.Find ("message").GetComponent<Text> ().text = s;
	}

	/// <summary>
	/// Constructs a building over the network
	/// </summary>
	/// <param name="index">Index in the spawnables array of the building to construct.</param>
	/// <param name="pos">Position.</param>
	/// <param name="q">rotation.</param>
	/// <param name="pid">Player id.</param>
	[Command (channel = CHANNEL)]
	public void CmdBuild (int index, Vector3 pos, Quaternion q, NetworkInstanceId pid, NetworkInstanceId lotId) {
		Player player = getLocalInstance (pid).GetComponent<Player> ();
		Lot lot = getLocalInstance (lotId).GetComponent<Lot> ();
		GameObject constructionParticles;
		constructionParticles = (GameObject)Resources.Load ("ConstructionParticles");
		GameObject tmp = (GameObject)Instantiate (spawnables [index].spawnable, pos, q);
		NetworkServer.Spawn (tmp);

		//Spawns construction particle indicator and plays construction sound
		GameObject particles = (GameObject)Instantiate (constructionParticles, pos, q);
		NetworkServer.Spawn (particles);


		// deduct some amount from the player's budget
		Building b = tmp.GetComponent<Building> ();

		if (b != null) {
			b.upgrade = true;     // it should not spawn with bad modifiers
			b.lot = lotId;
			lot.addObject (b.netId);

			if (player != null) {
				player.budget -= spawnables [index].price;
				player.message = "Spent $" + spawnables [index].price + " to build " + spawnables [index].name + "!";
				b.setOwner(player.netId);
				b.notForSale = true;
			} 
		}
	}

	[Command]
	public void CmdMove (NetworkInstanceId buildingId, NetworkInstanceId player, Vector3 pos, Quaternion rotation) {
		Player p = getLocalInstance (player).GetComponent<Player> ();
		Building b = getLocalInstance (buildingId).GetComponent<Building> ();

		if ((p != null) && b.ownedBy(p.netId)) {
			b.transform.position = pos;
			b.transform.rotation = rotation;
		}
	}

	[Command]
	public void CmdDemolish (NetworkInstanceId buildingId, NetworkInstanceId player) {
		Player p = getLocalInstance (player).GetComponent<Player> ();
		Building b = getLocalInstance (buildingId).GetComponent<Building> ();
		Lot l = b.getLot ();

		if ((p != null) && b.ownedBy(p)) {
			int price = getDestroyCost (b);
			p.budget -= price;
			p.owned.removeId (b.netId);
			if (l != null) {
				l.removeObject (b.netId);
			}
			if (b.isOccupied ()) {
				b.tenant.evict ();
			}
			Destroy (b.gameObject);
		}
	}

	/// <summary>
	/// checks if the player can build the object. If they're not acting as a company, it just checks their budget.
	/// If they're acting as a company, it also checks to make sure they have permission to build using company funds.
	/// </summary>
	/// <returns><c>true</c>, if player can build, <c>false</c> otherwise.</returns>
	/// <param name="amount">cost to build.</param>
	public bool canBuild (int amount) {
		bool b = false;
		if (player.budget >= amount) {
			b = true;
		} else {
			player.showMessage ("You can't afford that!");
		}
		return b;
	}

	/// <summary>
	/// Checks if a player can move a building
	/// </summary>
	/// <returns><c>true</c>, if player is able to move the building, <c>false</c> otherwise.</returns>
	/// <param name="b">The building.</param>
	public bool canMove (Building b) {
		bool canMove = false;
		if ((b.lot != null) && getLocalInstance (b.lot).GetComponent<Building> ().ownedBy(player)) {
			canMove = true;
		}
		return canMove;
	}

	public void buildMode () {
		ConstructionBoundary lotBoundary;
		int index = currentSpawnable % spawnables.Count;

		if (confirm != null) { // don't move the object around while the player is dealing with the confirmation box
			if (Input.GetKeyDown (KeyCode.E) || Input.GetKeyDown (KeyCode.Return)) {
				confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.Invoke (); // hit the button if they player hits E or Enter
			} else if (Input.GetKeyDown (KeyCode.Escape)) {
				confirm.transform.Find ("Cancel").GetComponent<Button> ().onClick.Invoke (); // hit escape to cancel
			}
			return;
		}
		if (tooltip == null) {
			tooltip = (GameObject)Instantiate (Resources.Load ("BuildTooltip"));
			tooltip.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			setTooltip ();
		}
		if (Input.GetKeyDown (KeyCode.Tab)) {
			if (toBuild != null) {
				Destroy (toBuild);
				Destroy (tooltip);
			}
			player.construction = false;
		}

		if (toBuild == null) {
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;
			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100f, layerMask)) {
				toBuild = (GameObject)Instantiate (spawnables [index].dummy, hit.point/*GetSharedSnapPosition(hit.point, .1f)*/, constructionRotation);
				if (toBuild.CompareTag ("floor")) {
					toBuild.transform.rotation = Quaternion.identity;
				}
			}
		} else {
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;

			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
				if (hit.collider.gameObject.name != toBuild.name && !toBuild.CompareTag("floor")) {
					toBuild.transform.position = hit.point;
				} else {
					toBuild.transform.position = GetSharedSnapPosition(hit.point, .5f);
				}
			}

			if (hit.collider != null) {
				//Grabs construction boundary from toBuild object to check for proper placement in the lot
				lotBoundary = toBuild.GetComponent<ConstructionBoundary> ();
				Lot l = hit.collider.gameObject.GetComponent<Lot> ();
				if (lotBoundary.isConstructable) {
					if ((l != null)) {
						if (l.ownedBy (this.netId)) {
							readyToConstruct = true;
							lotBoundary.turnGreen ();
						}
					} else {
						readyToConstruct = false;
						lotBoundary.turnRed ();
					}
				} else {
					readyToConstruct = false;
					lotBoundary.turnRed ();
				}
			
				if (Input.GetKeyDown (KeyCode.E)) {
					if (canBuild (spawnables [index].price)) {
						if (readyToConstruct) {
							confirm = (GameObject)Instantiate (Resources.Load ("Confirm"));
							confirm.transform.SetParent (GameObject.Find ("Canvas").transform, false);
							confirm.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Build " + spawnables [index].name + " for $" + spawnables [index].price + "?";
							confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
								lotBoundary.resetColor ();
								CmdBuild (index, toBuild.transform.position, toBuild.transform.rotation, this.netId, l.netId);
								constructionRotation = toBuild.transform.rotation;
								Destroy (toBuild);
								Destroy (tooltip);
								toBuild = null;
								player.construction = false;
								Destroy (confirm);
							});
							confirm.transform.Find ("Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
								Destroy (confirm);
							});
						} else {
							player.showMessage ("You can't build there.");
						}
					}
				}
			}
			
			if (Input.GetKeyDown (KeyCode.Mouse1) && toBuild.CompareTag ("floor")) { //For paths
				toBuild.transform.Rotate (new Vector3 (0, 30, 0)); //Snap to 90 degrees
				constructionRotation = toBuild.transform.rotation;
			} else if (Input.GetKeyDown (KeyCode.Mouse0) && toBuild.CompareTag ("floor")) { //For paths
				toBuild.transform.Rotate (new Vector3 (0, -30, 0)); //Snap to 90 degrees
				constructionRotation = toBuild.transform.rotation;
			}
			if (Input.GetKey (KeyCode.Mouse1) && !toBuild.CompareTag ("floor")) {
				toBuild.transform.Rotate (new Vector3 (0, 2, 0));
				constructionRotation = toBuild.transform.rotation;
			} else if (Input.GetKey (KeyCode.Mouse0) && !toBuild.CompareTag ("floor")) {
				toBuild.transform.Rotate (new Vector3 (0, -2, 0));
				constructionRotation = toBuild.transform.rotation;
			}
		}
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			Destroy (toBuild);
			toBuild = null;
			currentSpawnable++;
			setTooltip ();
		} else if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			Destroy (toBuild);
			toBuild = null;
			currentSpawnable--;
			if (currentSpawnable < 0) {
				currentSpawnable = (spawnables.Count - 1);
			}
			setTooltip ();
		}
	}

	public void moveMode (GameObject target) {
		Building building = target.GetComponent<Building> ();
		if (targetBuilding == -1) {
			targetBuilding = findBuildingSpawnable (target); // index of placement prefab
		}
		if (confirm != null) { // don't move the object around while the player is dealing with the confirmation box
			if (Input.GetKeyDown (KeyCode.E) || Input.GetKeyDown (KeyCode.Return)) {
				confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.Invoke (); // hit the button if they player hits E or Enter
			} else if (Input.GetKeyDown (KeyCode.Escape)) {
				confirm.transform.Find ("Cancel").GetComponent<Button> ().onClick.Invoke (); // hit escape to cancel
			}
			return;
		}
		if (((building != null && canMove (building)) || (building == null)) && (targetBuilding > -1)) {
			if (target.gameObject.activeSelf) {
				target.gameObject.SetActive (false);
			}
			ConstructionBoundary lotBoundary;

			// Cancel key
			if (Input.GetKeyDown (KeyCode.Tab)) {
				target.gameObject.SetActive (true);
				if (toBuild != null) {
					Destroy (toBuild);
					targetBuilding = -1;
				}
				player.moveMode = false;
			}

			if (toBuild == null) {
				Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
				RaycastHit hit;

				if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
					toBuild = (GameObject)Instantiate (spawnables [targetBuilding].dummy, hit.point, constructionRotation);
				}
			} else {
				Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
				RaycastHit hit;

				if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
					if (hit.collider.gameObject.name != toBuild.name) {
						toBuild.transform.position = hit.point;
					}
				}

				if (hit.collider != null) {
					//Grabs construction boundary from toBuild object to check for proper placement in the lot
					lotBoundary = toBuild.GetComponent<ConstructionBoundary> ();
					Lot l;
					if (lotBoundary.isConstructable) {
						l = hit.collider.gameObject.GetComponent<Lot> ();
						if ((l != null)) {
							if (l.ownedBy (this.netId) && ((building != null && l.netId == building.lot) || building == null)) {
								readyToConstruct = true;
								lotBoundary.turnGreen ();
							} 
						} else {
							readyToConstruct = false;
							lotBoundary.turnRed ();
						}
					} else {
						readyToConstruct = false;
						lotBoundary.turnRed ();
					}


					if (Input.GetKeyDown (KeyCode.E)) {
						if (readyToConstruct) {
							target.gameObject.SetActive (true);
							CmdMove (target.GetComponent<NetworkIdentity> ().netId, player.netId, toBuild.transform.position, toBuild.transform.rotation);
							Destroy (toBuild);
							targetBuilding = -1;
							player.moveMode = false;
						}
					}

					if (Input.GetKeyDown (KeyCode.X)) {
						confirmDestroy (target);
					}
				}

				if (Input.GetKey (KeyCode.Mouse1)) {
					toBuild.transform.Rotate (new Vector3 (0, 2, 0));
					constructionRotation = toBuild.transform.rotation;
				} else if (Input.GetKey (KeyCode.Mouse0)) {
					toBuild.transform.Rotate (new Vector3 (0, -2, 0));
					constructionRotation = toBuild.transform.rotation;
				}
			}
		} else {
			player.moveMode = false;
		}
	}

	public GameObject confirmDestroy(GameObject target) {
		confirm = (GameObject)Instantiate (Resources.Load ("Confirm"));
		confirm.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		confirm.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Demolish this building? It will cost $" + getDestroyCost(target.GetComponent<Building>()) + ".";
		confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
			NetworkInstanceId tmp = target.GetComponent<NetworkIdentity>().netId;
			player.targetBuilding = null;
			player.updateUI();
			CmdDemolish (tmp, player.netId);
			Destroy (toBuild);
			player.moveMode = false;
			player.controlsAllowed(true);
			Destroy (confirm);
			targetBuilding = -1;
		});
		confirm.transform.Find ("Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
			Destroy (confirm);
			player.controlsAllowed(true);
			targetBuilding = -1;
		});
		return confirm;
	}

	public int findBuildingSpawnable (GameObject b) {
		int index = -1;
		if (b != null) {
			for (int i = 0; i < spawnables.Count; i++) {
				if (b.name.Contains (spawnables [i].spawnable.name)) {
					index = i;
					i = spawnables.Count;
				}
			}
		}
		return index;
	}

	/// <summary>
	/// Accepts a value, and snaps it according to the value of snap
	/// </summary>
	public static float GetSnapValue(float value, float snap)
	{
		return (!Mathf.Approximately(snap, 0f)) ? Mathf.RoundToInt(value / snap) * snap : value;
	}
		
	/// <summary>
	/// Accepts a position, and sets each axis-value of the position to be snapped according to the value of snap
	/// </summary>
	public static Vector3 GetSharedSnapPosition(Vector3 originalPosition, float snap/*float snap = 0.01f*/)
	{
		return new Vector3(GetSnapValue(originalPosition.x, snap), GetSnapValue(originalPosition.y, snap), GetSnapValue(originalPosition.z, snap));
	}

	private int getDestroyCost(Building b) {
		int price = b.getBaseCost () / 4;
		return price;
	}
}
