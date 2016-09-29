﻿using UnityEngine;
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
		vehicleOccupied = false;
		eligibleToExit = false;

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
		}
	}
	

	void Update () {
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