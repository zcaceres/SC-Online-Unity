using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Tenant))]

public class SuperMart : Restaurant {
	const int ATTRACTIVENESS_EFFECT = -30;
	const int TYPENUM = 14;
	private static string[] rSmallFirst = {
		"Bargain",
		"Whole",
		"Super",
		"Huge Deals",
		"Wally's",
		"Dan's Club",
		"Such-A-Deal",
		"Big Box",
	};
	private static string[] rSmallLast = { "Supermart",};

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;

		//Higher radius for SuperMart, Grocery store is 60
		radius = 70;
		//Get DeliveryJobActivator to enable jobs on occupancy
		deliveryJobActivator = gameObject.transform.Find("DeliveryJobActivator").gameObject;
		deliveryJobActivator.SetActive (false);
		type = TYPENUM;

		if (isServer) {
			//Higher rent for SuperMart than Grocery Store
			baseRent = 500;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 10000;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			neededWorkers = 16;
			buildingName = nameGen ();
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			objectNum++;
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
				localLot.addObject(this.netId);
			}
			malusGenerator ();
			updateRent ();
		}
		typeName = buildingTypes [type];
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
			modManager.addMalus ("Health Code Violations", .5f, 1, 1, 1000, 1);
		} else if (Random.value < .15f) {
			modManager.addMalus ("Rats!", .5f, 1, 1, 1000, 1);
		} else if (Random.value < .15f) {
			modManager.addMalus ("Anti SuperMart Protesters", .8f, 1, .9f, 3000, 4); 
		}
	}
}
