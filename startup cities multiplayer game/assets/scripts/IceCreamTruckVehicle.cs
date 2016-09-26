using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;

public class IceCreamTruckVehicle : Vehicle
{
	private UnityStandardAssets.Vehicles.Car.CarController carController;
	private AudioSource megaPhoneLoop;
	private Megaphone mega; //used for mobile collection of $$ from customers
	protected int earnings;
//	public bool vehicleOccupied;
//	private AudioSource horn;
//	public int type;
//	[SyncVar]
//	public int cost;
//	[SyncVar]
//	public int upkeep;
//	[SyncVar]
//	public int id;
//	[SyncVar]
//	public bool notForSale;
//	[SyncVar]
//	public bool fire;
//	[SyncVar]
//	public string vehicleName;
//	[SyncVar]
//	public string typeName;
//	[SyncVar]
//	protected NetworkInstanceId owner;

//	public Color color;
//	// The original vehicle color
//	public Collider c;
	// vehicle's collider

//	private FireTransform[] fireTrans;
	//The number of fire transforms connected to the building

	private static string[] rSmallFirst = {
		"Yumsters",
		"Frozen",
		"Cold",
		"Dairy",
		"Icey"
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Ice Cream", "Delights", "Pops" };


	void Start ()
	{
		AudioSource[] carSounds = GetComponents<AudioSource> ();
		horn = carSounds [1];
		vehicleOccupied = false;
		mega = GetComponentInChildren<Megaphone> ();
//		type = TYPENUM;
		foreach (AudioSource aSources in carSounds) {
			aSources.enabled = false;
		}
		if (isServer) {
			cost = 10000;
			fire = false;
			upkeep = 0; // ADD UPKEEP HERE
			typeName = "Vehicle";
			vehicleName = nameGen ();
		}
		carController = GetComponent<UnityStandardAssets.Vehicles.Car.CarController> ();

	}


	void Update ()
	{
		if (vehicleOccupied) {
			if (Input.GetKeyDown (KeyCode.Mouse0) && !horn.isPlaying) {
				horn.Play ();
			}
			if (Input.GetKeyUp (KeyCode.Mouse0)) {
				horn.Stop ();
			}
			if (Input.GetKey (KeyCode.F) && eligibleToExit) {
				Player p = gameObject.GetComponentInChildren<Player> ();
				ExitVehicle (p);
			}
			if (carController.CurrentSpeed <= 15f && vehicleOccupied) {
				mega.ToggleFoodTruck (true);
			} else {
				mega.ToggleFoodTruck (false);
			}
		}
	}

//	public void addMoney (int i)
//	{
//		earnings += i;
//	}

	//BEGIN OWNERSHIP METHODS

	//advance month function here?


	/// <summary>
	/// Generates a name for the building from the residential names file.
	/// </summary>
	/// <returns>The gen.</returns>
	private string nameGen() {
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
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
	public float getHighest ()
	{
		if ((c != null) && (c.gameObject.GetComponent<MeshCollider> () != null) && (c.gameObject.GetComponent<MeshCollider> ().sharedMesh != null)) {
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
