using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ConstructionController : NetworkBehaviour {
	const int CHANNEL = 1;
	const int MAYOR_CATEGORY = 6;
	const float ROAD_ANGLE_DOWN = 20f;
	const float ROAD_ANGLE_UP = 30f;

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
		public int category;
		// item category 

		// categories: 0 cheap residential 1 med res 2 high res 3 business 4 decoration 5 utility 
		public Spawnable (Object dum, Object spawn, int cost, int level, int type, int a, string n, string d, int c) {
			dummy = dum;
			spawnable = spawn;
			price = cost;
			priceLevel = level;
			buildingType = type;
			attractiveness = a;
			name = n;
			description = d;
			category = c;
		}
	}

	private Player player;
	private Quaternion constructionRotation;
	private List<Spawnable>[] spawnables;
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
	private int currentCategory;
	private bool categorySelected;
	private bool snapped;
	private bool lockedX;
	private bool lockedY;
	private bool lockedZ;

	// Use this for initialization
	void Start () {
		nm = FindObjectOfType<NetworkManager> ();
		layerMask = ~(1 << LayerMask.NameToLayer ("player") | 1 << LayerMask.NameToLayer ("node") | 1 << LayerMask.NameToLayer ("Ignore Raycast") | 1 << LayerMask.NameToLayer("trail") | 1 << LayerMask.NameToLayer("lot"));
		spawnables = new List<Spawnable>[7];
		spawnables[0] = new List<Spawnable> ();
		spawnables[1] = new List<Spawnable> ();
		spawnables[2] = new List<Spawnable> ();
		spawnables[3] = new List<Spawnable> ();
		spawnables[4] = new List<Spawnable> ();
		spawnables[5] = new List<Spawnable> ();
		spawnables [6] = new List<Spawnable> ();
		Spawnable tmp;
		currentCategory = 5;
		categorySelected = false;

		string[] lines = System.IO.File.ReadAllLines (@"Assets\constructables\constructables_list.txt");
		foreach (string s in lines) {
			if (!s.StartsWith("//")) {
				string[] values = s.Split (',');
				if (values.Length > 8) {
					Object dummy = Resources.Load (values [0].Trim());
					Object prefab = Resources.Load (values [1].Trim());
					int cost = int.Parse (values [2]);
					int level = int.Parse (values [3]);
					int type = int.Parse (values [4]);
					int attr = int.Parse (values [5]);
					string name = values [6].Trim();
					string desc = values [7].Trim();
					int category = int.Parse(values [8].Trim ());
					tmp = new Spawnable (dummy, prefab, cost, level, type, attr, name, desc, category); 
					spawnables[category].Add (tmp);
				}
			}
		}

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
		Spawnable current = spawnables [currentCategory][currentSpawnable % spawnables[currentCategory].Count];

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
		Text t = tooltip.transform.Find ("message").GetComponent<Text> ();
		t.alignment = TextAnchor.UpperLeft;
		t.text = s;
	}

	private void setCategoryTooltip() {
		string s;
		if (currentCategory == 6) {
			s = "City Utilities";	
		} else if (currentCategory == 5) {
			s = "Utilities";
		} else if (currentCategory == 4) {
			s = "Decorations";
		} else if (currentCategory == 3) {
			s = "Businesses";
		} else if (currentCategory == 2) {
			s = "Expensive Residential";
		} else if (currentCategory == 1) {
			s = "Medium Residential";
		} else {
			s = "Cheap Residential";
		} 
		Text t = tooltip.transform.Find ("message").GetComponent<Text> ();
		t.alignment = TextAnchor.UpperCenter;
		t.text = s;
	}

	/// <summary>
	/// Constructs a building over the network
	/// </summary>
	/// <param name="index">Index in the spawnables array of the building to construct.</param>
	/// <param name="pos">Position.</param>
	/// <param name="q">rotation.</param>
	/// <param name="pid">Player id.</param>
	[Command (channel = CHANNEL)]
	public void CmdBuild (int index, int category, Vector3 pos, Quaternion q, NetworkInstanceId pid, NetworkInstanceId lotId) {
		Player player = getLocalInstance (pid).GetComponent<Player> ();
		Lot lot = getLocalInstance (lotId).GetComponent<Lot> ();
		GameObject constructionParticles;
		constructionParticles = (GameObject)Resources.Load ("ConstructionParticles");
		GameObject tmp = (GameObject)Instantiate (spawnables[category][index].spawnable, pos, q);
		NetworkServer.Spawn (tmp);

		//Spawns construction particle indicator and plays construction sound
		GameObject particles = (GameObject)Instantiate (constructionParticles, pos, q);
		NetworkServer.Spawn (particles);


		// deduct some amount from the player's budget
		OwnableObject b = tmp.GetComponent<OwnableObject> ();

		if (b != null) {
			if (b is Building) {
				b.GetComponent<Building>().upgrade = true;     // it should not spawn with bad modifiers
			}
			b.lot = lotId;
			lot.addObject (b.netId);

			if (player != null) {
				player.budget -= spawnables[category] [index].price;
				player.message = "Spent $" + spawnables [category][index].price + " to build " + spawnables [category][index].name + "!";
				b.setOwner(player.netId);
				b.notForSale = true;
			} 
		}
	}
		
	[Command (channel = CHANNEL)]
	public void CmdCityBuild (int index, int category, Vector3 pos, Quaternion q, Vector3 scale, NetworkInstanceId pid, NetworkInstanceId regionId) {
		Player player = getLocalInstance (pid).GetComponent<Player> ();
		GameObject constructionParticles;
		GameObject rTmp = getLocalInstance (regionId);
		Region region = null;
		if (rTmp != null) {
			region = getLocalInstance (regionId).GetComponent<Region> ();
		}
		constructionParticles = (GameObject)Resources.Load ("ConstructionParticles");
		GameObject tmp = (GameObject)Instantiate (spawnables[category][index].spawnable, pos, q);
		tmp.transform.localScale = scale;
		NetworkServer.Spawn (tmp);

		//Spawns construction particle indicator and plays construction sound
		GameObject particles = (GameObject)Instantiate (constructionParticles, pos, q);
		NetworkServer.Spawn (particles);


		// deduct some amount from the player's budget
		OwnableObject b = tmp.GetComponent<OwnableObject> ();

		if (b != null) {
			if (b is Building) {
				b.GetComponent<Building>().upgrade = true;     // it should not spawn with bad modifiers
			}
			if (regionId != NetworkInstanceId.Invalid && region != null) {
				b.region = regionId;
				b.localRegion = region;
			}
			if (player != null) {
				player.budget -= spawnables[category] [index].price;
				player.message = "Spent $" + spawnables [category][index].price + " to build " + spawnables [category][index].name + "!";
				b.setOwner(player.netId);
				b.notForSale = true;
			} 
		}
	}

	[Command]
	public void CmdMove (NetworkInstanceId buildingId, NetworkInstanceId player, Vector3 pos, Quaternion rotation) {
		Player p = getLocalInstance (player).GetComponent<Player> ();
		OwnableObject b = getLocalInstance (buildingId).GetComponent<OwnableObject> ();

		if ((p != null) && b.ownedBy(p.netId)) {
			b.transform.position = pos;
			b.transform.rotation = rotation;
		}
	}

	[Command]
	public void CmdDemolish (NetworkInstanceId buildingId, NetworkInstanceId player) {
		Player p = getLocalInstance (player).GetComponent<Player> ();
		OwnableObject b = getLocalInstance (buildingId).GetComponent<OwnableObject> ();
		Lot l = b.getLot ();

		if ((p != null) && b.ownedBy(p)) {
			int price = getDestroyCost (b);
			p.budget -= price;
			p.owned.removeId (b.netId);
			if (l != null) {
				l.removeObject (b.netId);
			}
			if (b is Building) {
				Building tmp = b.GetComponent<Building> ();
				if (tmp.isOccupied ()) {
					tmp.tenant.evict ();
				}
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

	public bool CityCanBuild (int amount) {
		bool b = false;
		if (player.activeCityHall != null && player.activeCityHall.ownedBy (player)) {
			if (player.activeCityHall.GetBudget() >= amount) {
				b = true;
			}
		}
		return b;
	}

	/// <summary>
	/// Checks if a player can move a building
	/// </summary>
	/// <returns><c>true</c>, if player is able to move the building, <c>false</c> otherwise.</returns>
	/// <param name="b">The building.</param>
	public bool canMove (OwnableObject b) {
		bool canMove = false;
		if ((b.lot != null) && getLocalInstance (b.lot).GetComponent<OwnableObject> ().ownedBy(player)) {
			canMove = true;
		}
		return canMove;
	}

	public void buildMode () {
		ConstructionBoundary lotBoundary;
		int index = currentSpawnable % spawnables[currentCategory].Count;

		// Player is currently selecting the mayor category, use its function
		if (currentCategory == MAYOR_CATEGORY) {
			//if (player.activeCityHall != null && player.activeCityHall.ownedBy(player)) {
				CityBuildMode ();
				return;
		//	} else { // skip over the city utilities category if the player isnt acting as a city
		//		currentCategory = 0;
		//	}
		}

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
			if (toBuild != null || !categorySelected) {
				Destroy (toBuild);
				Destroy (tooltip);
			}
			player.construction = false;
		}
		if (!categorySelected) {
			setCategoryTooltip ();
		} else if (toBuild == null) {
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;
			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100f, layerMask)) {
				toBuild = (GameObject)Instantiate (spawnables [currentCategory][index].dummy, hit.point, constructionRotation);
				if (toBuild.CompareTag ("floor")) {
					toBuild.transform.rotation = Quaternion.identity;
				}
			}
		} else {
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;

			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
				if (hit.collider.gameObject.name != toBuild.name && !toBuild.CompareTag ("floor")) {
					ConstrainedPosition (hit);
				}
				else {
					toBuild.transform.position = GetSharedSnapPosition(hit.point, .5f);
				}
			}

			if (hit.collider != null) {
				//Grabs construction boundary from toBuild object to check for proper placement in the lot
				lotBoundary = toBuild.GetComponent<ConstructionBoundary> ();
				Lot l = hit.collider.gameObject.GetComponent<Lot> ();
				if (lotBoundary.isConstructable) {
					if ((l != null) || lotBoundary.triggerLot != null) {
						if (lotBoundary.triggerLot != null) {
							l = lotBoundary.triggerLot;
						}
						if (l.ownedBy (this.netId) && l.canBuild (spawnables [currentCategory] [index].buildingType)) {
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
					if (canBuild (spawnables [currentCategory][index].price)) {
						if (readyToConstruct) {
							confirm = (GameObject)Instantiate (Resources.Load ("Confirm"));
							confirm.transform.SetParent (GameObject.Find ("Canvas").transform, false);
							confirm.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Build " + spawnables[currentCategory] [index].name + " for $" + spawnables[currentCategory] [index].price + "?";
							confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
								lotBoundary.resetColor ();
								CmdBuild (index,currentCategory, toBuild.transform.position, toBuild.transform.rotation, this.netId, l.netId);
								constructionRotation = toBuild.transform.rotation;
								//Destroy (toBuild);
								//Destroy (tooltip);
								//toBuild = null;
								//player.construction = false;
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
				
			RotationControls ();
			constructionRotation = toBuild.transform.rotation;
		}
		ItemSelect ();
	}

	public void moveMode (GameObject target) {
		OwnableObject building = target.GetComponent<OwnableObject> ();
		if (targetBuilding == -1) {
			int tmp = findBuildingCategory (target);
			if (tmp > 0) {
				currentCategory = tmp;
			}
			targetBuilding = findBuildingSpawnable (target, currentCategory); // index of placement prefab
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
					toBuild = (GameObject)Instantiate (spawnables [currentCategory][targetBuilding].dummy, hit.point, constructionRotation);
				}
			} else {
				Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
				RaycastHit hit;

				if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
					if (hit.collider.gameObject.name != toBuild.name) {
						ConstrainedPosition (hit);
					}
				}

				if (hit.collider != null) {
					//Grabs construction boundary from toBuild object to check for proper placement in the lot
					lotBoundary = toBuild.GetComponent<ConstructionBoundary> ();
					Lot l;
					if (lotBoundary.isConstructable) {
						l = hit.collider.gameObject.GetComponent<Lot> ();
						if ((l != null) || lotBoundary.triggerLot != null) {
							if (lotBoundary.triggerLot != null) {
								l = lotBoundary.triggerLot;
							}
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

				RotationControls ();
			}
		} else {
			player.moveMode = false;
		}
	}

	/// <summary>
	/// Build mode for city objects, doesn't look at lots and stuff
	/// </summary>
	public void CityBuildMode() {
		//road should not be snapped at start of buildmode
		ConstructionBoundary lotBoundary;
		int index = currentSpawnable % spawnables[currentCategory].Count;

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
			if (toBuild != null || !categorySelected) {
				Destroy (toBuild);
				Destroy (tooltip);
			}
			player.construction = false;
		}
		if (!categorySelected) {
			setCategoryTooltip ();
		} else if (toBuild == null) {
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;
			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100f, layerMask)) {
				toBuild = (GameObject)Instantiate (spawnables [currentCategory][index].dummy, hit.point, Quaternion.identity);
				if (toBuild.CompareTag ("floor")) {
					toBuild.transform.rotation = Quaternion.identity;
					if (hit.collider.gameObject.GetComponent<RoadConnector> () != null) {
						foreach (Renderer r in toBuild.GetComponents<Renderer>()) {
							r.enabled = false; //toggles visibility of the road dummy prefab OFF if you aren't raycasting aroad
						}
						snapped = false;
					}
				}
			}
		} else {
			bool isRoad = toBuild.CompareTag("floor");
			Vector3 fwd = playerCamera.transform.TransformDirection (Vector3.forward); // ray shooting from camera
			RaycastHit hit;

			if (Physics.Raycast (playerCamera.transform.position, fwd, out hit, 100, layerMask)) {
				if (isRoad) { // this handles the placement of road items, ignore it for cops/lots/others
					if (hit.collider.gameObject.GetComponent<RoadConnector> () != null) { //if i raycast a road
						if (!snapped) {
							Transform[] roadSnapTransforms = hit.collider.gameObject.GetComponent<RoadConnector> ().GetRoadConnectorTransforms (); //Get roadconnector component on the road and all its connectors
							int numOfConnectors = roadSnapTransforms.Length; //get the number of connectorS
							Vector3[] positionOfTransform = new Vector3[numOfConnectors];
							Transform snapPosition; //used to select snap position
							float shortestDistanceToTransform = Vector3.Distance (hit.point, positionOfTransform [0]);
							for (int i = 0; i < numOfConnectors; i++) { //looks through all possible snap transforms to find their world position Vector3
								positionOfTransform [i] = roadSnapTransforms [i].position;
							}
							for (int i = 0; i < numOfConnectors; i++) {
								if (Vector3.Distance (hit.point, positionOfTransform [i]) < shortestDistanceToTransform) { //compares position of snaptransform and the raycast point
									shortestDistanceToTransform = Vector3.Distance (hit.point, positionOfTransform [i]); //if it's the shortest position in the array
									snapPosition = roadSnapTransforms [i]; //sets the snap position to that transform's Vector3
									foreach (Renderer r in toBuild.GetComponents<Renderer>()) {
										r.enabled = true; //toggles visibility of the road dummy prefab ON
									}
									toBuild.transform.position = snapPosition.position; //snaps dummy prefab to raycast road position
									toBuild.transform.rotation = snapPosition.rotation;
									snapped = true; //toggles snap to allow for construction (road can only be constructed if snapped
									RotateToTerrain ();
								}
							}
						} else {
							RotateUp ();
						}
					} else {
						foreach (Renderer r in toBuild.GetComponents<Renderer>()) {
							r.enabled = false; //toggles visibility of the road dummy prefab OFF if you aren't raycasting aroad
						}
						snapped = false;
					}
				} else { // non-road objects
					toBuild.transform.position = hit.point;
				}
			}

			if (hit.collider != null) {
				//Grabs construction boundary from toBuild object to check for proper placement in the lot
				lotBoundary = toBuild.GetComponent<ConstructionBoundary> ();

				if (lotBoundary.scaffold.colliding) { // city stuff only needs to make sure its not colliding with anything, since it will not be on lots
					readyToConstruct = false;
					lotBoundary.turnRed ();
				} else if (!lotBoundary.scaffold.colliding && (!isRoad || snapped)) { //ensures road is not colliding with anything and that it's also SNAPPED to a snaptransform
					if (toBuild.CompareTag ("lot")) {
						if (toBuild.GetComponent<RoadConnectionChecker> ().IsConnected ()) {
							readyToConstruct = true;
							lotBoundary.turnGreen ();
						} else {
							readyToConstruct = false;
							lotBoundary.turnRed ();
						}
					} else {
						RoadTerrainCollisionCheck tmp = toBuild.GetComponent<RoadTerrainCollisionCheck> ();
						if (tmp != null && tmp.Colliding ()) {
							readyToConstruct = false;
							lotBoundary.turnRed ();
						} else {
							readyToConstruct = true;
							lotBoundary.turnGreen ();
						}
					}
				} else {
					readyToConstruct = false;
					lotBoundary.turnRed ();
				}

				if (Input.GetKeyDown (KeyCode.E)) {
					if (canBuild (spawnables [currentCategory][index].price)) {
						if (readyToConstruct) {
							confirm = (GameObject)Instantiate (Resources.Load ("Confirm"));
							confirm.transform.SetParent (GameObject.Find ("Canvas").transform, false);
							confirm.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Build " + spawnables[currentCategory] [index].name + " for $" + spawnables[currentCategory] [index].price + "?";
							confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
								lotBoundary.resetColor ();
								CmdCityBuild (index,currentCategory, toBuild.transform.position, toBuild.transform.rotation, toBuild.transform.localScale, this.netId, NetworkInstanceId.Invalid);
								//constructionRotation = toBuild.transform.rotation;
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
					
				if (Input.GetKey (KeyCode.Mouse1)) {
					if (!isRoad) {
						toBuild.transform.Rotate (new Vector3 (0, 2, 0));
					}
					//constructionRotation = toBuild.transform.rotation;
				} else if (Input.GetKey (KeyCode.Mouse0)) {
					if (!isRoad) {
						toBuild.transform.Rotate (new Vector3 (0, -2, 0));
					}
					//constructionRotation = toBuild.transform.rotation;
				}
			}
		}
		ItemSelect ();	
	}

	public GameObject confirmDestroy(GameObject target) {
		confirm = (GameObject)Instantiate (Resources.Load ("Confirm"));
		confirm.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		confirm.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Demolish this building? It will cost $" + getDestroyCost(target.GetComponent<OwnableObject>()) + ".";
		confirm.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
			NetworkInstanceId tmp = target.GetComponent<NetworkIdentity>().netId;
			player.targetObject = null;
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

	public int findBuildingCategory(GameObject b) {
		int index = -1;
		if (b != null) {
			for (int i = 0; i < spawnables.Count () && index == -1; i++) {
				foreach (Spawnable s in spawnables[i]) {
					if (b.name.Contains (s.spawnable.name)) {
						index = i;
						break;
					}
				}
			}
		}
		return index;
	}

	public int findBuildingSpawnable (GameObject b, int category) {
		int index = -1;
		if (b != null && category != -1) {
			for (int i = 0; i < spawnables [category].Count; i++) {
				if (b.name.Contains (spawnables  [category][i].spawnable.name)) {
					index = i;
					i = spawnables [category].Count;
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

	/// <summary>
	/// Rotates the toBuild gameobject to the terrain's slope. returns false if the rotation is too severe
	/// </summary>
	private void RotateToTerrain() {

		MeshRenderer mr = null;
		toBuild.transform.Rotate (-toBuild.transform.eulerAngles.x, 0, 0); // bring it back to flat x rotation before starting
		if (toBuild.name.Contains("Road_Straight")) {
			mr = toBuild.GetComponent<MeshRenderer> ();
		}

		if (mr != null) {
			RaycastHit hit;
			Vector3 ray = Vector3.down;
			if (Physics.Raycast (mr.bounds.center, ray, out hit)) {
				if (hit.collider.gameObject.CompareTag ("terrain")) {
					float GroundDis = hit.distance;
					float xRot = 0;
					if (hit.distance > 5) { // terrain is far below the object, so just rotate down as much as possible
						xRot = ROAD_ANGLE_DOWN;
					} else {
						// terrain is not too far below the object, rotate to terrain's slope
						Vector3 rotation = Quaternion.FromToRotation (toBuild.transform.up, hit.normal).eulerAngles;
						//Debug.Log ("Hit terrain: DISTANCE" + hit.distance + "      ROTATION" + rotation.x);
						if (rotation.x <= -ROAD_ANGLE_DOWN) {
							xRot = -ROAD_ANGLE_DOWN;
						} else if (rotation.x >= ROAD_ANGLE_DOWN) {
							xRot = ROAD_ANGLE_DOWN;
						} else {
							xRot = rotation.x;
						}
					}
					Vector3 angles = toBuild.transform.rotation.eulerAngles;
					if ((angles.x + xRot) >= ROAD_ANGLE_DOWN && (angles.x + xRot) <= (360 - ROAD_ANGLE_DOWN)) {
						xRot = xRot - angles.x;
					} 

					toBuild.transform.Rotate (xRot, 0, 0);

				} else {
					//Debug.Log ("Did not hit terrain: " + hit.collider.gameObject.name);
				}
			} else {
				//Debug.Log ("Hit nothing");
			}
		}
	}

	private void RotateUp() {
		RoadTerrainCollisionCheck collCheck = toBuild.GetComponent<RoadTerrainCollisionCheck> ();
		if (collCheck != null && collCheck.Colliding() && (toBuild.transform.eulerAngles.x < ROAD_ANGLE_UP || toBuild.transform.eulerAngles.x > 360-ROAD_ANGLE_UP) ){
			toBuild.transform.Rotate (-1f, 0, 0);
		}
	}

	/// <summary>
	/// Positions the toBuild object to the hit point while disregarding locked axes
	/// </summary>
	/// <param name="hit">Hit point.</param>
	private void ConstrainedPosition(RaycastHit hit) {
		if (Input.GetKeyDown (KeyCode.X)) {
			lockedX = !lockedX;
		}
		if (Input.GetKeyDown (KeyCode.Y)) {
			lockedY = !lockedY;
		}
		if (Input.GetKeyDown (KeyCode.Z)) {
			lockedZ = !lockedZ;
		}
		if (toBuild != null) {
			float x = toBuild.transform.position.x; 
			float y = toBuild.transform.position.y;
			float z = toBuild.transform.position.z;
			if (!lockedZ) {
				z = hit.point.z;
			}
			if (!lockedY) {
				y = hit.point.y;
			}
			if (!lockedX) {
				x = hit.point.x;
			}
			toBuild.transform.position = new Vector3 (x, y, z);
		}
	}

	private void RotationControls() {
		if (Input.GetKeyDown (KeyCode.Mouse1) && toBuild.CompareTag ("floor")) { //For paths
			toBuild.transform.Rotate (new Vector3 (0, 30, 0)); //Snap to 90 degrees
		} else if (Input.GetKeyDown (KeyCode.Mouse0) && toBuild.CompareTag ("floor")) { //For paths
			toBuild.transform.Rotate (new Vector3 (0, -30, 0)); //Snap to 90 degrees
		} else if (Input.GetKeyDown (KeyCode.LeftControl)) {
			toBuild.transform.Rotate (0, 30, 0);
		} else if (Input.GetKeyDown (KeyCode.RightControl)) {
			toBuild.transform.Rotate (0, -30, 0);
		} else if (Input.GetKeyDown (KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt)) {
			toBuild.transform.Rotate (-toBuild.transform.eulerAngles.x, -toBuild.transform.eulerAngles.y, -toBuild.transform.eulerAngles.z);
		} else if (Input.GetKey (KeyCode.Mouse1)) {
			toBuild.transform.Rotate (new Vector3 (0, 2, 0));
		} else if (Input.GetKey (KeyCode.Mouse0)) {
			toBuild.transform.Rotate (new Vector3 (0, -2, 0));
		}
	}

	/// <summary>
	/// Function used by the build modes to check the input used to select the current spawnable or spawnable category
	/// </summary>
	private void ItemSelect() {
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			if (categorySelected) {
				Destroy (toBuild);
				toBuild = null;
				currentSpawnable++;
				setTooltip ();
			} else {
				currentCategory++;
				currentCategory = currentCategory % spawnables.Length;
				setCategoryTooltip ();
			}
		} else if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			if (categorySelected) {
				Destroy (toBuild);
				toBuild = null;
				currentSpawnable--;
				if (currentSpawnable < 0) {
					currentSpawnable = (spawnables [currentCategory].Count - 1);
				}
				setTooltip ();
			} else {
				currentCategory--;
				currentCategory = currentCategory % spawnables.Length;
				if (currentCategory < 0) {
					currentCategory = (spawnables.Count () - 1);
				}
				setCategoryTooltip ();
			}
		} else if (Input.GetKeyDown (KeyCode.UpArrow)) {
			categorySelected = true;
			setTooltip ();
		} else if (Input.GetKeyDown (KeyCode.DownArrow)) {
			categorySelected = false;
			Destroy (toBuild);
			toBuild = null;
			setCategoryTooltip ();
		}
	}

	private int getDestroyCost(OwnableObject b) {
		int price = b.getBaseCost () / 4;
		return price;
	}
}
