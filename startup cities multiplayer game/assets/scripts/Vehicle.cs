using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;

// TODO: vehicleambient loop
// ToggleVisualizeDamage
// Sound synchronization
// Write design schematic for vehicle prefabs
// Passengers in vehicles with class PassengerEnterVehicle

public class Vehicle : DamageableObject
{
	public bool vehicleOccupied;
	public AudioSource vehicleAmbientLoop; //ambient loop for certain vehicles
	private Transform vehicleDamageParticleSystem;
	protected AudioSource horn;
	public int type;
	[SyncVar]
	public int upkeep;
	[SyncVar]
	public string vehicleName;
	[SyncVar]
	public string typeName;
	[SyncVar]
	public bool ruin;

	protected const int TYPENUM = 27;
	public bool eligibleToExit;

	public Color color; 	// The original vehicle color
	public Collider c; 	// vehicle's collider

	private static string[] rSmallFirst = {
		"Generic",
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Vehicle", };


	void Start ()
	{
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		//vehicleDamageParticleSystem = gameObject.transform.
		horn = vehicleSounds [1];
		vehicleOccupied = false;
		type = TYPENUM;
		eligibleToExit = false;

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}

		if (isServer) {
			cost = 4000;
			fire = false;
			baseCost = cost;
			baseCondition = 100;
			condition = 100;
			ruin = false;
			upkeep = 0; // ADD UPKEEP HERE
			typeName = "Vehicle";
			vehicleName = nameGen ();
		}
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
			CheckCondition ();
			ToggleVisualizeDamage ();
		}
	}


	//Advance month function to check fire state, pay taxes, pay upkeep


	protected virtual void CheckCondition ()
	{
		if (condition <= 0) { //Triggers ruin state if vehicle reaches 0
			AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
			foreach (AudioSource aSources in vehicleSounds) {
				aSources.enabled = false;
			}
			ruin = true;
			UnityStandardAssets.Vehicles.Car.CarUserControl carU = GetComponent<UnityStandardAssets.Vehicles.Car.CarUserControl> ();
			UnityStandardAssets.Vehicles.Car.CarController carC = GetComponent<UnityStandardAssets.Vehicles.Car.CarController> ();
			carC.enabled = false;
			carU.enabled = false;
		}
	}
		

	/// <summary>
	/// Visualizes condition damage below 25 with black smoke from the
	/// vehicle prefab's hood
	/// </summary>
	protected virtual void ToggleVisualizeDamage () {
		if (condition <= 25) {
//			vehicleDamageParticleSystem
			//Turn on particle system
			//RPC to do the same
			RpcToggleVisualizeDamage(true);
			//trigger particle system on prefab with black smoke from car's hood
		} else {
			//turn off damageparticlesystem
			//RPC to do the same
			RpcToggleVisualizeDamage(false);

		}
	}

	/// <summary>
	/// RPC the toggle visualize damage.
	/// </summary>
	/// <param name="damaged">If set to <c>true</c> damaged.</param>
	[ClientRpc]
	protected virtual void RpcToggleVisualizeDamage(bool damaged) {
		if (damaged) {


		} else {


		}

	}

	/// <summary>
	/// Starts the vehicle.
	/// </summary>
	/// <param name="p">P.</param>
	public void StartVehicle (Player p)
	{
		NetworkInstanceId netId = p.netId;
		//HidePlayer (p, true);
		ToggleVehicleAmbientLoop (true);
		if (isServer) {
			RpcHidePlayer (netId, true); //Hides player model
			EnableVehicle (netId, true); //Enables vehicle sounds and controls
		}
		if (p.isLocalPlayer) {
			StartCoroutine ("DelayToExit"); //Coroutine to prevent immediate exit with "F"
		}
	}

	protected void ToggleVehicleAmbientLoop (bool enabled) {
		if (enabled) {
//			vehicleAmbientLoop.enabled = true;
			if (isServer) {
				RpcToggleVehicleAmbientLoop (true);
			} else {
				CmdToggleVehicleAmbientLoop (true);
			}
		} else {
			//vehicleAmbientLoop.enabled = false;
			if (isServer) {
				RpcToggleVehicleAmbientLoop (false);
			} else {
				CmdToggleVehicleAmbientLoop (false);
			}
		}
	}

	[ClientRpc]
	protected void RpcToggleVehicleAmbientLoop (bool enabled) {
		if (enabled) {
			vehicleAmbientLoop.enabled = true;
		} else {
			vehicleAmbientLoop.enabled = false;
		}
	}

	[Command]
	protected void CmdToggleVehicleAmbientLoop (bool enabled) {
		if (enabled) {
			vehicleAmbientLoop.enabled = true;
		} else {
			vehicleAmbientLoop.enabled = false;
		}
	}

	/// <summary>
	/// Exits the vehicle.
	/// </summary>
	/// <param name="p">P.</param>
	public void ExitVehicle (Player p)
	{
		NetworkInstanceId netId = p.netId;
		//HidePlayer (p, false);
		if (isServer) {
			RpcHidePlayer (netId, false); //Unhides player model
			if (vehicleAmbientLoop != null) {
				ToggleVehicleAmbientLoop (false); //turns off ambient loop
			}
		} else {
			//unhide player with Command
		}
		EnableVehicle (netId, false); //Enables vehicle sounds and controls
		if (p.isLocalPlayer) {
			StartCoroutine ("DelayToEnter"); //Coroutine to prevent immediate exit
		}
	}


	/// <summary>
	/// Enables the vehicle for player's use
	/// Sounds, user controller, core behaviours in carcontroller and camera
	/// </summary>
	/// <param name="active">If set to <c>true</c> active.</param>
	protected virtual void EnableVehicle (NetworkInstanceId netId, bool active)
	{
		Player play = getPlayer (netId);
		EnableVehicleSounds (active);
		if (play.isLocalPlayer) {
			UnityStandardAssets.Vehicles.Car.CarUserControl carU = GetComponent<UnityStandardAssets.Vehicles.Car.CarUserControl> ();
			UnityStandardAssets.Vehicles.Car.CarController carC = GetComponent<UnityStandardAssets.Vehicles.Car.CarController> ();
			carC.enabled = active;
			carU.enabled = active;
			Camera vehicleCam = GetComponentInChildren<Camera> ();
			vehicleCam.enabled = active;
		}

	}

	void EnableVehicleSounds (bool active) {
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		if (isServer) {
			RpcEnableVehicleSounds (active, vehicleSounds);
		} else {
			CmdEnableVehicleSounds (active, vehicleSounds);
		}
		
	}

	void RpcEnableVehicleSounds (bool active, AudioSource[] vehicleSounds) {
		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = active;
		}
		UnityStandardAssets.Vehicles.Car.CarAudio carA = GetComponent<UnityStandardAssets.Vehicles.Car.CarAudio> ();
		carA.enabled = active;
		if (active) {
			vehicleSounds [2].Play ();
			vehicleSounds [0].Play ();
		}
	}

	void CmdEnableVehicleSounds (bool active, AudioSource[] vehicleSounds) {
		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = active;
		}
		UnityStandardAssets.Vehicles.Car.CarAudio carA = GetComponent<UnityStandardAssets.Vehicles.Car.CarAudio> ();
		carA.enabled = active;
		if (active) {
			vehicleSounds [2].Play ();
			vehicleSounds [0].Play ();
		}
	}





//	/// <summary>
//	/// Hides the player and removes collision for the player object when player
//	/// enters the vehicle. Player is attached to the vehicle's transform on
//	/// the "EnterVehicle" class/transforms on vehicle prefab
//	/// </summary>
//	/// <param name="play">Play.</param>
//	/// <param name="active">If set to <c>true</c> active.</param>
//	private void HidePlayer (Player play, bool active)
//	{
//		if (play.isLocalPlayer) {
//			Renderer[] rends = play.GetComponentsInChildren<Renderer> ();
//			if (active) {
//				play.GetComponent<Rigidbody> ().isKinematic = true;
//				play.GetComponent<CapsuleCollider> ().enabled = false;
//				foreach (Renderer r in rends) {
//					r.enabled = false;
//				}
//				play.gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = false;
//				play.gameObject.transform.SetParent (this.gameObject.transform);
//				vehicleOccupied = true;
//				play.message = "Press F to leave vehicle.";
//			} else {
//				play.GetComponent<Rigidbody> ().isKinematic = false;
//				play.GetComponent<Collider> ().enabled = true;
//				foreach (Renderer r in rends) {
//					r.enabled = true;
//				}
//				play.gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = true;
//				play.gameObject.transform.SetParent (null);
//				vehicleOccupied = false;
//			}
//		}
//	}

	/// <summary>
	/// Client RPC for hiding the player that has entered the vehicle
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	/// <param name="active">If set to <c>true</c> active.</param>
	[ClientRpc]
	private void RpcHidePlayer (NetworkInstanceId netId, bool active)
	{
		Player play = getPlayer (netId);
		Debug.LogError ("Player check in RpcHidePlayer: " + play.name);
		Renderer[] rends = play.GetComponentsInChildren<Renderer> ();
		Debug.LogError ("Renderer check should be more than 1: " + rends.Length);
		if (active) {
			play.GetComponent<Rigidbody> ().isKinematic = true;
			play.GetComponent<CapsuleCollider> ().enabled = false;
			foreach (Renderer r in rends) {
				r.enabled = false;
			}
			if (play.isLocalPlayer) {
				play.gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = false;
			}
			play.gameObject.transform.SetParent (this.gameObject.transform);
			vehicleOccupied = true;
			play.message = "Press F to get out.";
		} else {
			play.GetComponent<Rigidbody> ().isKinematic = false;
			play.GetComponent<Collider> ().enabled = true;
			foreach (Renderer r in rends) {
				r.enabled = true;
			}
			if (play.isLocalPlayer) {
				play.gameObject.transform.Find ("MainCamera").GetComponent<Camera> ().enabled = true;
			}
			play.gameObject.transform.SetParent (null);
			vehicleOccupied = false;
		}
	}



	/// <summary>
	/// Generates a name for the vehicle
	/// TODO: move vehicle names to file I/O
	/// </summary>
	/// <returns>The gen.</returns>
	private string nameGen ()
	{
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}

	//advance month function here?



	/// <summary>
	/// Delay to prevent immediate enter/exit from vehicle on pressing F
	/// </summary>
	/// <returns>The to exit.</returns>
	private IEnumerator DelayToExit ()
	{
		yield return new WaitForSeconds (2f);
		eligibleToExit = true;
	}


	/// <summary>
	/// Delay to prevent immediate enter/exit from vehicle on pressing F
	/// </summary>
	/// <returns>The to enter.</returns>
	private IEnumerator DelayToEnter ()
	{
		yield return new WaitForSeconds (2f);
		eligibleToExit = false;
	}


	/// <summary>
	/// Returns the data associated with the vehicle
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadout ()
	{
		string s;
//		modManager.clearButtons ();
		string ownerName = "";
		if (!validOwner ()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer (owner).getName ();
		}
		s = "Type: " + typeName + "\nName : " + vehicleName + "\nOwner: " + ownerName + "\nPrice: " + cost + "\nCondition: " + condition.ToString ();

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
	public virtual string getReadoutText ()
	{
		string s;
		string ownerName = "";
		if (!validOwner ()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer (owner).getName ();
		}
		s = "Type: " + typeName + "\nName : " + vehicleName + "\nOwner: " + ownerName + "\nPrice: " + cost + "\nCondition: " + condition.ToString ();

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		return s;
	}


	/// <summary>
	/// Gets the cost to restore the vehicle to 100 condition
	/// </summary>
	/// <returns>The repair cost.</returns>
	public override int getRepairCost ()
	{
		int repairCost;
		if (ruin) {
			repairCost = baseCost;
		} else {			
			repairCost = (100 - baseCondition) * 5;
		}
		return repairCost;
	}

	/// <summary>
	/// Gets the cost of repairing a single point of condition.
	/// </summary>
	/// <returns>The point repair cost.</returns>
	public override int getPointRepairCost ()
	{
		int repairCost;
		if (ruin) {
			repairCost = baseCost;
		} else {
			repairCost = baseCost / 100; // cost of each point
		}
		return repairCost;
	}

	/// <summary>
	/// Turns the vehicle to a ruin--used when condition is 0.
	/// </summary>
	[ClientRpc]
	protected void RpcMakeRuin ()
	{
		setColor (Color.black);
		if (isServer) {
			ruin = true;
			//occupied = false;
			endFire ();
		}
	}

	/// <summary>
	/// Repair a ruined vehicle
	/// </summary>
	[ClientRpc]
	protected void RpcFixRuin ()
	{
		if (ruin) {
			if (isServer) {
				condition = 100;
				baseCondition = 100;
				ruin = false;
			}
			setColor (color);
		}
	}


	/// <summary>
	/// Adds a color overlay to the building
	/// </summary>
	/// <param name="newColor">New color.</param>
	public virtual void setColor (Color newColor)
	{
		if (c != null) {
			c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color = newColor;
		}
	}

	/// <summary>
	/// Returns the building to its original color, or black if its a ruin
	/// </summary>
	public void resetColor ()
	{
		if (ruin) {
			setColor (Color.black);
		} else {
			setColor (color);
		}
	}


	/// <summary>
	/// Repair the vehicle to 100 condition.
	/// </summary>
	public override void repair() {

		if (isServer) {
			if (ruin) {
				RpcFixRuin ();
			} else {
				condition = 100;
				baseCondition = 100;
			}
		}
	}

	/// <summary>
	/// Repairs the vehicle by point.
	/// </summary>
	/// <param name="numPoints">Number points.</param>
	public override void repairByPoint(int numPoints) {
		if (isServer && !ruin) {
			condition += numPoints;
			baseCondition += numPoints;
		}
	}


	/// <summary>
	/// Sets the vehicle on fire.
	/// </summary>
	public override void setFire ()
	{
		if (isServer) {
			if (validOwner ()) {
				RpcMessageOwner (vehicleName + " is on fire!");
			}
			fire = true;
			GameObject fireObj = (GameObject)Resources.Load ("CarFire"); //unique fire prefab for vehicle class
			FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform> ();
			if (fireTrans.Length < 1) {
				GameObject tmp = (GameObject)Instantiate (fireObj, new Vector3 (gameObject.transform.position.x, getHighest (), gameObject.transform.position.z), fireObj.transform.rotation);
				NetworkServer.Spawn (tmp);
				Debug.LogError (vehicleName + " is on fire but has no transforms");
			}
			foreach (FireTransform ft in fireTrans) {
				GameObject tmp = (GameObject)Instantiate (fireObj, ft.transform.position, fireObj.transform.rotation);
				//tmp.transform.SetParent (ft.transform);
				FireKiller fk = tmp.GetComponent<FireKiller> ();
				ft.onFire = true; //Tells the fire transform that it is on fire. All fts must report back OnFire = false for advance month to consider the building not on fire!
				fk.myTransform = ft; //sets the FireKiller's firetransform, which allows it to update the FT about the state of the fire!
				fk.setObject (gameObject.GetComponent<Vehicle> ());
				NetworkServer.Spawn (tmp);
			}
		}
	}
		

}
