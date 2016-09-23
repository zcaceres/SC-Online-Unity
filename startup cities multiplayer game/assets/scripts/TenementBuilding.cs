using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TenementBuilding : Building {
	const int ATTRACTIVENESS_EFFECT = -10;
	const int TYPENUM = 9;
	private static string[] rSmallFirst = System.IO.File.ReadAllLines (@"Assets\names\residentialSmallFirst.txt");
	private static string[] rSmallLast = { "Tenement",};

	public void Start () {
		c = GetComponent<Collider> ();

		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;

		if (isServer) {
			lowestSkill = 0;
			baseRent = 75;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = rent * 12;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			buildingName = nameGen ();
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			buildingNum++;
			if (!upgrade) {
				malusGenerator ();
			}
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
				localLot.addObject(this.netId);
			}
			updateRent ();
			//updateNeighborhoodValue ();
		}
		typeName = buildingTypes [type];
	}

	private void Setup () {
		malusGenerator ();
		updateRent ();
	}

	/// <summary>
	/// returns the effect the building has on a lot's attractiveness
	/// </summary>
	/// <returns>The attractiveness effect.</returns>
	public override int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	/// <summary>
	/// Generates a name for the building from the residential names file.
	/// </summary>
	/// <returns>The gen.</returns>
	private string nameGen() {
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}


	/// <summary>
	/// Generates a set of bad starting modifiers for the building
	/// </summary>
	private void malusGenerator() {
		if (Random.value < .15f) {
			modManager.addMalus ("Bad Plumbing", .9f, 1, 1, 500, 8);
		}
		if (Random.value < .18f) {
			modManager.addMalus ("Former Drug Den", 1, 1, .5f, 450, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Termites", .9f, .7f, 1, 600, 40);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Dated Decor", .95f, 1, 1, 200, 31);
		}
		if (Random.value < .18f) {
			modManager.addMalus ("Cockroach Infestation", .85f, 1, 1, 550, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Faulty Wiring", 1, .9f, 1, 400, 2);
		}
		if (Random.value < .18f) {
			modManager.addMalus ("Asbestos", 1f, .8f, 1f, 300, 17);
		}
		if (Random.value < .18f) {
			modManager.addMalus ("Lead Paint", 1f, .8f, 1f, 300, 17);
		}
	}
}
