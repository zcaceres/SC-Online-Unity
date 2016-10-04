using UnityEngine;
using System.Collections;

public class PimpCar : Vehicle {
	private static string[] rSmallFirst = {
		"Swag",
		"Pimp",
		"Cherry",
		"Sweet Ass",
		"Stylish"
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Wagon", "Mobile", "Car" };


	void Start () {
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		vehicleDamageParticleSystem = gameObject.transform.Find ("Helpers").Find ("VehicleDamageParticles").gameObject;

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}

		if (isServer) {
			cost = 7000;
			fire = false;
			baseCost = cost;
			baseCondition = 100;
			condition = 100;
			ruin = false;
			upkeep = 0; // ADD UPKEEP HERE
			type = TYPENUM;
			typeName = "Car";
			vehicleName = nameGen ();
			vehicleOccupied = false;
			vehicleToughness = 1;
			passengerLimit = 2;
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
