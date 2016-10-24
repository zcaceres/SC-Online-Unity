using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using UnityStandardAssets.Vehicles.Car;

public class FoodTruck : Vehicle
{
	protected CarController carController;
	protected AudioSource megaPhoneLoop; // audio for loop
	protected Megaphone mega; //used for mobile collection of $$ from customers

	//Names for Vehicles
	private static string[] rSmallFirst = {
		"Food",
	};
		
	private static string[] rSmallLast = { "Truck", };


	void Start ()
	{
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
			vehicleOccupied = false;
			vehicleToughness = 2;
			passengers = 0;
		}


		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		vehicleDamageParticleSystem = gameObject.transform.Find ("VehicleDamageParticles").gameObject;
		//vehicleOccupied = false;
		mega = GetComponentInChildren<Megaphone> ();

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}
			
		carController = GetComponent<CarController> ();
		//Used to get currentspeed for food truck mobile business collider
	}


	protected override void Update ()
	{
		if (vehicleOccupied) {
			if (Input.GetKeyDown (KeyCode.F)) {
				ExitToggle ();
			}
			FoodTruckSpeedCheck ();
		}



		if (isServer) {
			CheckCondition ();
		}
	}

	protected virtual void FoodTruckSpeedCheck() {
		if (/*vehicleOccupied && */carController.CurrentSpeed <= 15f && !ruin) {
			mega.ToggleFoodTruck (true);
		} else {
			mega.ToggleFoodTruck (false);
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

}