using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;

public class Vehicle : NetworkBehaviour {
	public bool vehicleOccupied;
	protected AudioSource horn;
	public int type;
	[SyncVar]
	public int cost;
	[SyncVar]
	public int upkeep;
	[SyncVar]
	public int id;
	[SyncVar]
	public bool notForSale;
	[SyncVar]
	public bool fire;
	[SyncVar]
	public string vehicleName;
	[SyncVar]
	public string typeName;
	[SyncVar]
	protected NetworkInstanceId owner;
	const int TYPENUM = 27;

	public Color color;           // The original vehicle color
	public Collider c;            // vehicle's collider

	protected FireTransform[] fireTrans; //The number of fire transforms connected to the building

	private static string[] rSmallFirst = {
		"Swag",
		"Pimp",
		"Cherry",
		"Sweet Ass",
		"Stylish"
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Wagon", "Mobile", "Car"};


	void Start () {
		AudioSource[] carSounds = GetComponents<AudioSource> ();
		horn = carSounds [1];
		vehicleOccupied = false;
		type = TYPENUM;
		foreach (AudioSource aSources in carSounds) {
			aSources.enabled = false;
		}
		if (isServer) {
			cost = 4000;
			fire = false;
			upkeep = 0; // ADD UPKEEP HERE
			typeName = "Vehicle";
			vehicleName = nameGen ();
		}
	}


	void Update() {
		if (vehicleOccupied) {
			if (Input.GetKeyDown (KeyCode.Mouse0) && !horn.isPlaying) {
				horn.Play ();
			}
			if (Input.GetKeyUp (KeyCode.Mouse0)) {
				horn.Stop ();
			}
			if (Input.GetKey (KeyCode.G)) {
				Player p = gameObject.GetComponentInChildren<Player> ();
				ExitVehicle (p);
			}
		}
	}
		

	//Vehicle Initialization here
	public void StartVehicle (Player p) {
		HidePlayer (p, true);
		EnableCar (true);
	}


	public void ExitVehicle (Player p) {
		HidePlayer (p, false);
		EnableCar (false);
	}


	protected virtual void EnableCar (bool active) {
		AudioSource[] carSounds = GetComponents<AudioSource> ();
		foreach (AudioSource aSources in carSounds) {
			aSources.enabled = active;
		}
		UnityStandardAssets.Vehicles.Car.CarAudio carA = GetComponent<UnityStandardAssets.Vehicles.Car.CarAudio>();
		carA.enabled = active;
		UnityStandardAssets.Vehicles.Car.CarUserControl carU = GetComponent<UnityStandardAssets.Vehicles.Car.CarUserControl> ();
		carU.enabled = active;
		Camera carCam = GetComponentInChildren<Camera> ();
		carCam.enabled = active;
		if (active) {
			carSounds [2].Play ();
			carSounds [0].Play ();
		}
	}


	private void HidePlayer (Player play, bool active)
	{
		Renderer[] rends = play.GetComponentsInChildren<Renderer> ();
		if (active) {
			play.GetComponent<Rigidbody> ().isKinematic = true;
			play.GetComponent<CapsuleCollider> ().enabled = false;
			foreach (Renderer r in rends) {
				r.enabled = false;
			}
			play.gameObject.transform.Find("MainCamera").GetComponent<Camera>().enabled = false;
			play.gameObject.transform.SetParent(this.gameObject.transform);
			vehicleOccupied = true;
			play.message = "Press G to leave the vehicle.";
		} else {
			play.GetComponent<Rigidbody> ().isKinematic = false;
			play.GetComponent<Collider> ().enabled = true;
			foreach (Renderer r in rends) {
				r.enabled = true;
			}
			play.gameObject.transform.Find("MainCamera").GetComponent<Camera>().enabled = true;
			play.gameObject.transform.SetParent(null);
			vehicleOccupied = false;
		}
	}


	//BEGIN OWNERSHIP METHODS

	/// <summary>
	/// Generates a name for the building from the residential names file.
	/// </summary>
	/// <returns>The gen.</returns>
	protected string nameGen() {
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}

	//advance month function here?

	public virtual int getCost() {
		return cost;
	}


	/// <summary>
	/// returns the owner ID (player number) or -1 if unowned
	/// </summary>
	/// <returns>The owner ID.</returns>
	public int getOwner() {
		int id;

		if (!validOwner()) {
			id = -1;
		} else {
			id = getPlayer(owner).id;
		}

		return id;
	}

	public virtual bool validOwner() {
		bool isValid = false;
		if (!owner.IsEmpty() && (owner != NetworkInstanceId.Invalid) && (getLocalInstance(owner) != null)) {
			isValid = true;
		}
		return isValid;
	}


	/// <summary>
	/// Returns the data associated with the vehicle
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadout() {
		string s;
//		modManager.clearButtons ();
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + typeName + "\nName : " + vehicleName + "\nOwner: " + ownerName + "\nPrice: " + cost /*+ "\nCondition: " + conditionToString()*/;

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		return s;
	}

	/// <summary>
	/// Returns the data associated with the vehicle, does not do anything with buttons
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadoutText() {
		string s;
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else  {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + typeName + "\nName : " + vehicleName + "\nOwner: " + ownerName + "\nPrice: " + cost /*+ "\nCondition: " + conditionToString ()*/;

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		return s;
	}


	/// <summary>
	/// Adds a color overlay to the building
	/// </summary>
	/// <param name="newColor">New color.</param>
	public virtual void setColor(Color newColor) {
		if (c != null) {
			c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color = newColor;
		}
	}

	/// <summary>
	/// Returns the building to its original color, or black if its a ruin
	/// </summary>
	public void resetColor() {
	//	if (ruin) {
	//		setColor (Color.black);
	//	} else {
	//		setColor(color);
	//	}
	}

	public virtual Player getPlayer(NetworkInstanceId playerId) {
		GameObject tmp = getLocalInstance (playerId);
		Player p;
		if (tmp != null) {
			p = tmp.GetComponent<Player> ();
		} else {
			p = null;
		}
		return p;
	}

	public virtual NetworkInstanceId getOwnerNetId() {
		return owner;
	}

	public virtual Player getPlayerOwner() {
		Player p;
		if (validOwner()) {
			p = getPlayer (owner);
		} else {
			p = null;
		}
		return p;
	}


	/// <summary>
	/// Sets the owner and removes the building from the owned list of its previous owner.
	/// </summary>
	/// <param name="newOwner">New owner's id.</param>
	public virtual void setOwner(NetworkInstanceId newOwner) {
		if (newOwner == owner)
			return;
		Player oldOwner = getPlayerOwner ();
		if (oldOwner != null) {
			oldOwner.owned.removeId (this.netId);
		}
		Player p = getLocalInstance (newOwner).GetComponent<Player> ();
		p.owned.addId (this.netId);
		owner = newOwner;
	}


	/// <summary>
	/// Checks ownership by netId
	/// </summary>
	/// <returns><c>true</c>, if owned by the object (company or player) whose netId was passed, <c>false</c> otherwise.</returns>
	/// <param name="o">Owner.</param>
	public virtual bool ownedBy(NetworkInstanceId o) {
		bool owned = false;
		if (validOwner()) {
			if (owner == o) {
				owned = true;
			}
		}
		return owned;
	}

	public virtual bool ownedBy(Player p) {
		bool owned = false;
		if (owner == p.netId) {
			owned = true;
		}
		return owned;
	}

	public virtual void unsetOwner() {
		owner = NetworkInstanceId.Invalid;
	}



	protected GameObject getLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}



	//	/// <summary>
	//	/// Checks the state of the fire.
	//	/// </summary>
	//	public void CheckFireState () {
	//		int fires = 0;
	//		FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform> ();
	//		foreach (FireTransform ft in fireTrans) {
	//			if (ft.onFire) {
	//				fires += 1;
	//			}
	//		}
	//		if (fires == 0) {
	//			endFire ();
	//		}
	//	}

	//	/// <summary>
	//	/// Sets the building on fire.
	//	/// </summary>
	//	public virtual void setFire() {
	//		if (isServer) {
	//			if (validOwner()) {
	//				RpcMessageOwner( buildingName + " is on fire!");
	//			}
	//			fire = true;
	//			GameObject fireObj = (GameObject)Resources.Load ("HouseFire");
	//			FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform>();
	//			if (fireTrans.Length < 1) {
	//				GameObject tmp = (GameObject)Instantiate (fireObj, new Vector3 (gameObject.transform.position.x, getHighest(), gameObject.transform.position.z), fireObj.transform.rotation);
	//				NetworkServer.Spawn (tmp);
	//				Debug.LogError ("Building is on fire but has no transforms");
	//			}
	//			foreach (FireTransform ft in fireTrans) {
	//				GameObject tmp = (GameObject)Instantiate (fireObj, ft.transform.position, fireObj.transform.rotation);
	//				FireKiller fk = tmp.GetComponent<FireKiller> ();
	//				ft.onFire = true; //Tells the fire transform that it is on fire. All fts must report back OnFire = false for advance month to consider the building not on fire!
	//				fk.myTransform = ft; //sets the FireKiller's firetransform, which allows it to update the FT about the state of the fire!
	//				fk.setBuilding (gameObject.GetComponent<Building> ());
	//				NetworkServer.Spawn (tmp);
	//			}
	//		}
	//	}
	//
	//	/// <summary>
	//	/// Ends the fire.
	//	/// </summary>
	//	public void endFire() {
	//		if (isServer) {
	//			fire = false;
	//		}
	//	}

	//	/// <summary>
	//	/// Spreads fire to neighbors.
	//	/// </summary>
	//	protected void spreadFire() {
	//		Collider[] colliding = Physics.OverlapSphere(c.transform.position, 5);
	//		foreach (Collider hit in colliding) {
	//			Building b = hit.GetComponent<Building> ();
	//
	//			if (b != null && !b.fire) {
	//				if (Random.value < .1f) {
	//					b.setFire ();
	//				}
	//			}
	//		}
	//	}

	/// <summary>
	/// Returns the highest point of the building's mesh.
	/// </summary>
	/// <returns>Highest point.</returns>
	public float getHighest()
	{
		if ((c != null) && (c.gameObject.GetComponent<MeshCollider>() != null) && (c.gameObject.GetComponent<MeshCollider>().sharedMesh != null)) {
			Vector3[] verts = c.gameObject.GetComponent<MeshCollider> ().sharedMesh.vertices;
			Vector3 topVertex = new Vector3 (0, float.NegativeInfinity, 0);
			for (int i = 0; i < verts.Length; i++) {
				Vector3 vert = transform.TransformPoint (verts [i]);
				if (vert.y > topVertex.y) {
					topVertex = vert;
				}
			}

			return topVertex.y;
		} else {
			return 0;
		}
	}

}
