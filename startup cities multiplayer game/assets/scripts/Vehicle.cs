using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using UnityStandardAssets.Vehicles.Car;

// TODO: Write design schematic for vehicle prefabs
// TODO: Passengers in vehicles with class PassengerEnterVehicle
// TODO: ^ better to just expand EnterVehicle class to accommodate passengers


public class Vehicle : DamageableObject
{
	public AudioSource vehicleAmbientLoop;
	//ambient loop for certain vehicles
	public GameObject vehicleDamageParticleSystem; //Set in Inspector!
	//particle system that shows that a vehicle has fallen below 25 condition
	protected AudioSource horn;
	public int type; //integer to indicate type of vehicle
	[SyncVar]
	public int upkeep;
	[SyncVar]
	public string vehicleName; //name of vehicle using namegen
	[SyncVar]
	public string typeName; //name of vehicle type
	[SyncVar]
	public bool ruin;
	[SyncVar(hook="ToggleVehicleSounds")]
	public bool vehicleOccupied; //toggled based on whether vehicle has a driver
	//[SyncVar]
	public int passengerLimit; //limits the number of passengers for the vehicle

	//TODO is this necessary??
	protected const int TYPENUM = 27;

	public Color color;
	// The original vehicle color
	public Collider c;
	// vehicle's collider

	protected static int vehicleToughness;
	/* Sets damage threshold for collision detection.
	 * Higher int means car resists collisions more. Specifically, a higher int 
	 * lowers the likelihood of damage being applied and also lowers the amount of damage
	 * applied in the event of a collision.
	*/

	private static string[] rSmallFirst = {
		"Generic",
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Vehicle", };


	void Start ()
	{
		if (isServer) {
			cost = 5000;
			fire = false;
			baseCost = cost;
			baseCondition = 100;
			condition = 100;
			ruin = false;
			upkeep = 100; // ADD UPKEEP HERE
			typeName = "Vehicle";
			vehicleName = nameGen ();
			vehicleOccupied = false;
			vehicleToughness = 3;
			passengerLimit = 2;
		}

		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		type = TYPENUM;
		if (vehicleDamageParticleSystem == null) {
			vehicleDamageParticleSystem = gameObject.transform.Find ("Helpers").Find ("VehicleDamageParticles").gameObject;
		}
		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}
	}

	/// <summary>
	/// Override as needed in sub-classes for special Update behaviour (such as Food Trucks)
	/// </summary>
	protected virtual void Update ()
	{
		if (vehicleOccupied) {
			if (Input.GetKeyDown (KeyCode.Mouse0) && !horn.isPlaying) {
				horn.Play ();
			}
			if (Input.GetKeyUp (KeyCode.Mouse0)) {
				horn.Stop ();
			}
			if (Input.GetKeyDown (KeyCode.F)) {
				Player p = gameObject.GetComponentInChildren<Player> ();
				if (p != null && p.eligibleToExitVehicle) {
					ExitVehicle (p);
				}
			}
		}
		if (isServer) {
			CheckCondition ();
		}
	}





	/* MAIN METHODS */

	/// <summary>
	/// Starts the vehicle.
	/// </summary>
	/// <param name="p">P.</param>
	public void StartVehicle (Player p)
	{
		NetworkInstanceId netId = p.netId;
		if (isServer) {
			EnableVehicle (netId, true); //Enables vehicle sounds and controls
			p.playerNotVisible = true; //Hides player model
		} else {
			EnableVehicle (netId, true); //Enables vehicle sounds and controls
			p.CmdSetPlayerVisibility (netId, true);
		}
		if (p.isLocalPlayer) {
			StartCoroutine (DelayToExit (netId)); //Coroutine to prevent immediate exit with "F"
		}
	}

	/// <summary>
	/// Exits the vehicle.
	/// </summary>
	/// <param name="p">P.</param>
	public void ExitVehicle (Player p)
	{
		NetworkInstanceId netId = p.netId;
		if (isServer) {
			p.playerNotVisible = false; //Unhides player model
		} else {
			p.CmdSetPlayerVisibility (netId, false);
		}
		EnableVehicle (netId, false); //Enables vehicle sounds and controls
		if (p.isLocalPlayer) {
			StartCoroutine (DelayToEnter (p)); //Coroutine to prevent immediate exit
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
		if (!ruin) {;
			if (play.isLocalPlayer) {
				CarController carC = GetComponent<CarController> ();
				carC.enabled = active;
				ToggleVehicleCam (play, active);
			}
			play.CmdSetVehicleOccupied (this.netId, active);
		} else {
			ToggleVehicleCam (play, active);
			play.CmdSetVehicleOccupied (this.netId, active);
		}
	}

	/// <summary>
	/// Toggles the vehicle camera
	/// </summary>
	/// <param name="active">If set to <c>true</c> active.</param>
	protected virtual void ToggleVehicleCam (Player p, bool active)
	{
		Camera vehicleCam = GetComponentInChildren<Camera> ();
		vehicleCam.enabled = active;
		if (active) {
			Transform parent = this.gameObject.transform;
			if (isServer) {
				p.gameObject.transform.SetParent (parent);
				p.RpcSetNewParent (netId, true);
			} else {
				p.CmdSetNewParent (netId, true);
			}
		} else {
			if (isServer) {
				p.gameObject.transform.SetParent (null);
				p.RpcSetNewParent (netId, false);
			} else {
				p.CmdSetNewParent (netId, false);
			}
		}
		p.ToggleVehicleControls (active, GetComponent<NetworkIdentity>().netId);
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




	/* CONDITION AND DAMAGE METHODS */


	//TODO: Look at togglevisualize damage false for networking
	//TODO: Can server handle all toggling of damage on and off*/

	/// <summary>
	/// Checks the condition of the vehicle to toggle damage visualization and ruin state
	/// </summary>
	protected virtual void CheckCondition ()
	{
		if (condition <= 25 && !ruin) { //Toggles visualization of damage if less than 25% condition
			ToggleVisualizeDamage (true);
			if (!fire) {
				if (Random.Range (0f, 10f) >= 8f) {
					setFire ();
				}
			}
		} else if (condition > 25) {
			ToggleVisualizeDamage (false);
		}
		if (condition <= 0) { //Triggers ruin state if vehicle reaches 0
			if (!ruin) {
				ToggleRuinedState (true); //toggles unusable car if becoming ruin from unruined state
			}
			ruin = true;
		} else {
			if (ruin) {
				ToggleRuinedState (false); //toggles usable car if previous ruin and becoming non-ruin
			}
			ruin = false;
		}
	}


	/// <summary>
	/// Toggles the ruined state of the vehicle.
	/// No audio. No ability to drive it.
	/// </summary>
	/// <param name="notRuined">If set to <c>true</c> not ruined.</param>
	protected virtual void ToggleRuinedState (bool isRuined)
	{
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = !isRuined;
		}
		CarController carC = GetComponent<CarController> ();
		carC.enabled = !isRuined;
	}


	/// <summary>
	/// Visualizes condition damage below 25 with black smoke from the
	/// vehicle prefab's hood
	/// </summary>
	protected virtual void ToggleVisualizeDamage (bool damaged)
	{
		if (isServer) {
			RpcToggleVisualizeDamage (damaged);
		}
	}

	/// <summary>
	/// RPC the toggle visualize damage.
	/// </summary>
	/// <param name="damaged">If set to <c>true</c> damaged.</param>
	[ClientRpc]
	protected void RpcToggleVisualizeDamage (bool damaged)
	{
		vehicleDamageParticleSystem.SetActive (damaged);
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
	/// Repair the vehicle to 100 condition.
	/// </summary>
	public override void repair ()
	{
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
	public override void repairByPoint (int numPoints)
	{
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
				FireKiller fk = tmp.GetComponent<FireKiller> ();
				ft.onFire = true; //Tells the fire transform that it is on fire. All fts must report back OnFire = false for advance month to consider the building not on fire!
				fk.myTransform = ft; //sets the FireKiller's firetransform, which allows it to update the FT about the state of the fire!
				fk.setObject (gameObject.GetComponent<Vehicle> ());
				NetworkServer.Spawn (tmp);
			}
		}
	}



	/* SOUND METHODS */


	/// <summary>
	/// Toggles the vehicle's ambient audio loop (like stereo system), if it has one.
	/// This is NOT used for food truck audio, which is toggled when they're doing business.
	/// </summary>
	/// <param name="enabled">If set to <c>true</c> enabled.</param>
	protected void ToggleVehicleSounds (bool occupied)
	{
		vehicleOccupied = occupied;
		if (vehicleOccupied) {
			if (!ruin) {
				ToggleCoreVehicleSounds (true);
				if (vehicleAmbientLoop != null) {
					ToggleAmbientVehicleLoop (true);
				}
			} else {
				ToggleCoreVehicleSounds (false);
				if (vehicleAmbientLoop != null) {
					ToggleAmbientVehicleLoop (false);
				}
			}
		} else {
			ToggleCoreVehicleSounds (false);
			if (vehicleAmbientLoop != null) {
				ToggleAmbientVehicleLoop (false);
			}
		}
	}

	protected void ToggleAmbientVehicleLoop (bool enabled)
	{
		if (enabled) {
			vehicleAmbientLoop.enabled = true;
		} else {
			vehicleAmbientLoop.enabled = false;
		}
	}


	/// <summary>
	/// Enables core vehicles sounds, like engine and tires.
	/// </summary>
	/// <param name="active">If set to <c>true</c> active.</param>
	protected virtual void ToggleCoreVehicleSounds (bool active)
	{
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = active;
		}
		CarAudio carA = GetComponent<CarAudio> ();
		carA.enabled = active;
		if (active) {
			vehicleSounds [2].Play ();
			vehicleSounds [0].Play ();
		}
	}



	/* UTILITY */

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



	/// <summary>
	/// Delay to prevent immediate enter/exit from vehicle on pressing F
	/// </summary>
	/// <returns>The to exit.</returns>
	private IEnumerator DelayToExit (NetworkInstanceId netId)
	{
		Player p = getPlayer (netId);
		yield return new WaitForSeconds (2f);
		p.eligibleToExitVehicle = true;

	}


	/// <summary>
	/// Delay to prevent immediate enter/exit from vehicle on pressing F
	/// </summary>
	/// <returns>The to enter.</returns>
	private IEnumerator DelayToEnter (Player p)
	{
		yield return new WaitForSeconds (2f);
		p.eligibleToExitVehicle = false;
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
	/// Gets the vehicle's toughness (damage tolerance)
	/// </summary>
	/// <returns>The vehicle toughness.</returns>
	public int getVehicleToughness ()
	{
		return vehicleToughness;
	}

}
