using UnityEngine;
using System.Collections;

public class RaceCar : Vehicle {
	private static string[] rSmallFirst = {
		"Fast",
		"Sporty",
		"Striped",
		"Damn Quick",
	};

	//Names for Vehicles
	private static string[] rSmallLast = { "Race Car" };

	// Use this for initialization
	void Start () {
		passengerLimit = 2;
		AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
		horn = vehicleSounds [1];
		vehicleDamageParticleSystem = gameObject.transform.Find ("Helpers").Find ("VehicleDamageParticles").gameObject;

		foreach (AudioSource aSources in vehicleSounds) {
			aSources.enabled = false;
		}

		if (isServer) {
			cost = 15000;
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
			passengers = 0;
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
