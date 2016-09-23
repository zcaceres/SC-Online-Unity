using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

public class CityHall : Business {
	const int TYPENUM = 16;
	private static string[] rSmallFirst = {
		"City"
	};
	private static string[] rSmallLast = { "Hall"};

	void Start() {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;
		if (isServer) {
			skillLevel = 0;
			baseRent = 1000;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 100000;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			buildingName = nameGen ();
			id = buildingNum;
			fire = false;
			ruin = false;
			//Temporarily set true to test available of jobs without player ownership of building
			occupied = true;
			onAuction = false;
			paying = false;
			buildingNum++;
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
	public override void updateRent() {
		if (isServer) {
			if (ruin) {
				rent = 0;
			} else {
				// set all to pre-modifier values first
				rent = baseRent;
				condition = baseCondition;
				safety = baseSafety;

				// then apply the modifiers
				modManager.apply ();
				rent = (int)(rent * (condition / 100f));
			}
		}
	}

	/// <summary>
	/// Returns an up-to-date rent amount, or 0 if the building lacks tenants
	/// </summary>
	/// <returns>The rent.</returns>
	public override int getRent() {
		int currentRent = 0;
		//Temporarily set as occupied so always generates rent
		if (occupied) {
			updateRent ();
			currentRent = rent;
		} 

		return currentRent;
	}

	/// <summary>
	/// Advances the month, applies condition damage, updates the rent, causes fire damage.
	/// </summary>
	public override void advanceMonth() {
		if (isServer) {
			occupied = true;
			if (condition > 25) {
				damageBuilding (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner() && !validCompany()) { 
				notForSale = false;
			}

			if (fire) {
				if (condition <= 0) {
					endFire ();
				} else {
					spreadFire ();
					damageBuilding (50);
				}
			}

			if (condition <= 0) {
				if (!ruin) {
					makeRuin ();
				}
			} else {
				updateRent ();
			}
		}
	}

	public override bool isDestructable() {
		return false;
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

}