using UnityEngine;
using System.Collections;

/// <summary>
/// Class for school bus vehicle
/// </summary>
public class SchoolBus : Vehicle {
		private static string[] rSmallFirst = {
			"School",
		};

		//Names for Vehicles
		private static string[] rSmallLast = { "Bus",};
		//public int passengerLimit = 12; //set in each child class for proper number of seats

		void Start () {
			AudioSource[] vehicleSounds = GetComponents<AudioSource> ();
			horn = vehicleSounds [1];
			vehicleDamageParticleSystem = gameObject.transform.Find ("Helpers").Find ("VehicleDamageParticles").gameObject;
			passengerLimit = 12;
			foreach (AudioSource aSources in vehicleSounds) {
				aSources.enabled = false;
			}

			if (isServer) {
				cost = 30000;
				fire = false;
				baseCost = cost;
				baseCondition = 100;
				condition = 100;
				ruin = false;
				upkeep = 0; // ADD UPKEEP HERE
				type = TYPENUM;
				typeName = "Bus";
				vehicleName = nameGen ();
				vehicleOccupied = false;
				vehicleToughness = 3;
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
