using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;

public class IceCreamTruckVehicle : Vehicle
{
	private UnityStandardAssets.Vehicles.Car.CarController carController;
	private AudioSource megaPhoneLoop; // audio for loop
	private Megaphone mega; //used for mobile collection of $$ from customers

	//Names for Vehicles
	private static string[] rSmallFirst = {
		"Yumsters",
		"Frozen",
		"Cold",
		"Dairy",
		"Icey"
	};
		
	private static string[] rSmallLast = { "Ice Cream", "Delights", "Pops" };


	void Start ()
	{
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		vehicleOccupied = false;
		eligibleToExit = false;
		mega = GetComponentInChildren<Megaphone> ();

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}

		if (isServer) {
			cost = 10000;
			fire = false;
			baseCost = cost;
			baseCondition = 100;
			condition = 100;
			ruin = false;
			upkeep = 0; // ADD UPKEEP HERE
			type = TYPENUM;
			typeName = "Food Truck";
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

			if (carController.CurrentSpeed <= 15f && getOwner() != -1 && !ruin) {
				mega.ToggleFoodTruck (true);
			} else {
				mega.ToggleFoodTruck (false);
			}
		}
		CheckCondition ();
		ToggleVisualizeDamage ();
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

}
