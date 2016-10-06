using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Tenant))]

public class GroceryStore : Restaurant {
	const int TYPENUM = 8;
	private static string[] rSmallFirst = {
		"Bargain",
        "Whole",
        "Super",
        "Lion",
        "Jimmy's",
		"Family",
        "Foodie",
		"Save-A-Ton",
	};
	private static string[] rSmallLast = { "Grocery", "Foods",};

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;

		//Higher radius for grocery store
		radius = 60;
		//Get DeliveryJobActivator to enable jobs on occupancy
		deliveryJobActivator = gameObject.transform.Find("DeliveryJobActivator").gameObject;
		deliveryJobActivator.SetActive (false);
		type = TYPENUM;

		if (isServer) {
			//Higher rent for grocery store
			baseRent = 250;
			baseCondition = 100;
			baseSafety = 100;
			neededWorkers = 8;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 8000;
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
			malusGenerator ();
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
				localLot.addObject(this.netId);
			}
			GameObject tmpRegion = getLocalInstance (region);
			if (tmpRegion != null) {
				localRegion = tmpRegion.GetComponent<Region> ();
			} else if (localRegion != null) {
				region = localRegion.netId;
				localRegion.AddItem (this.netId);
			}
			updateRent ();
		}
		typeName = buildingTypes [type];
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
		}
        else if (Random.value < .15f)
        {
            modManager.addMalus("Rats!", .5f, 1, 1, 1000, 1);
        }
	}

	public override void offerDeliveryJob(bool occupied) {
		RpcSetActive (occupied);
		if (deliveryJobActivator.activeInHierarchy) {
		} else {
			messageOwner(buildingName + " needs a manager to offer jobs.");
		}
	}

	//RPC to control 
	[ClientRpc]
	private void RpcSetActive(bool occupied) {
		if (deliveryJobActivator != null) {
			deliveryJobActivator.SetActive (occupied);
		}
	}
}
