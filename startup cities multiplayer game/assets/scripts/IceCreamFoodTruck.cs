using UnityEngine;
using System.Collections;
using UnityStandardAssets.Vehicles.Car;

public class IceCreamFoodTruck : FoodTruck {
	private static string[] rSmallFirst = {
		"Yumster's",
		"Frozen",
		"Cold",
		"Dairy",
		"Icey"
	};

	private static string[] rSmallLast = { "Ice Cream", "Delights", "Pops" };


	void Start () {
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
			vehicleToughness = 3;
			passengers = 0;
		}

		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		vehicleDamageParticleSystem = gameObject.transform.Find ("Helpers").Find ("VehicleDamageParticles").gameObject;
		vehicleOccupied = false;
		mega = GetComponentInChildren<Megaphone> ();

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}
			
		carController = GetComponent<CarController> ();
		//Used to get currentspeed for food truck mobile business collider
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
