using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Trailer : Building {
	const int ATTRACTIVENESS_EFFECT = -15;
	const int TYPENUM = 19;
	protected int upgradeCounter;
	protected Building building;

	private static string[] rSmallFirst = {
		"Smelly",
		"Creepy",
		"Nasty",
		"Dirty",
		"Depressing",
		"Pathetic",
		"Filthy",
		"Squalid",
		"Disgusting",
		"Run-down",
		"Crummy",
		"Miserable"
	};
	private static string[] rSmallLast = { "Trailer" };

	void Start () {
		c = GetComponent<Collider> ();
		lowestSkill = 0;
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;

		if (isServer) {
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
			if (!upgrade) {
				malusGenerator ();
			}
			updateRent ();
			//updateNeighborhoodValue ();
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
			modManager.addMalus ("Former Meth Lab", 1, 1, .5f, 450, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Cockroach Infestation", .85f, 1, 1, 550, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Hillbilly Decor", .90f, 1, 1, 200, 2);
		}
	}
}
