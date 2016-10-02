using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

public class Office : Business {
	const int ATTRACTIVENESS_EFFECT = -20;
	const int TYPENUM = 26;
	private static string[] rSmallFirst = { "Technology", "Banking", "Beemer", "Prestige", "Titan", "Accounting", "Apple", "Law", "Legal", "Embezzlement", "Wealth", "Software", "Trade"};
	private static string[] rSmallLast = { "Firm", "Office Building", "Offices", "Company", "Corporate", "Building" };

	void Start() {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;

		if (isServer) {
			skillLevel = 2;
			baseRent = 5000;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = baseRent * 12;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			buildingName = nameGen ();
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			objectNum++;
			neededWorkers = 8;
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

	/// <summary>
	/// Generates a set of bad starting modifiers for the building
	/// </summary>
	private void malusGenerator() {
		if (Random.value < .15f) {
			modManager.addMalus ("Lead Paint", .95f, 1, 1, 200, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Cockroach Infestation", .85f, 1, 1, 550, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Faulty Wiring", 1, .9f, 1, 400, 2);
		}
	}

	/// <summary>
	/// returns the effect the building has on a lot's attractiveness
	/// </summary>
	/// <returns>The attractiveness effect.</returns>
	public override int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	private string nameGen() {
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}
}
