using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Tenant))]

public class House : Building {
	const int ATTRACTIVENESS_EFFECT = 0;
	const int TYPENUM = 1;
	protected static Sprite[] icons;
	protected int upgradeCounter;
	protected Building building;

	private static string[] rSmallFirst = System.IO.File.ReadAllLines (@"Assets\names\residentialSmallFirst.txt");
	private static string[] rSmallLast = { "House", "Abode", "Place", "Residence", "Home" };

	public void Start () {
		lowestSkill = 1;
		c = GetComponent<Collider> ();
			
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		building = gameObject.GetComponent<Building> ();
		type = TYPENUM;

		if (icons == null) {
			icons = Resources.LoadAll<Sprite> ("Icons and Portraits/64 flat icons/png/32px");
		}

		if (isServer) {
			baseRent = 125;
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

	private void Setup () {
		malusGenerator ();
		updateRent ();
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
		if (Random.value < .15f) {
			modManager.addMalus ("Former Drug Den", 1, 1, .5f, 450, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Termites", .9f, .7f, 1, 600, 40);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Dated Decor", .95f, 1, 1, 200, 31);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Cockroach Infestation", .85f, 1, 1, 550, 1);
		}
		if (Random.value < .15f) {
			modManager.addMalus ("Faulty Wiring", 1, .9f, 1, 400, 2);
		}
	}
}
