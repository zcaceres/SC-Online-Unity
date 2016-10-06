using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Billboard : Business {
//	public GameObject deliveryJobActivator;
	const int ATTRACTIVENESS_EFFECT = -30;
	const int TYPENUM = 15;
	const int COST_PER_VISIT = 10;
	public int radius;
	private static string[] rSmallFirst = {
		"Rentable"
	};
	private static string[] rSmallLast = { 
		"Billboard" 
	};

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		radius = 50;
		type = TYPENUM;

		if (isServer) {
			skillLevel = 0;
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 4000;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			neededWorkers = 0;
			buildingName = nameGen ();
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = true;
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
			GameObject tmpRegion = getLocalInstance (region);
			if (tmpRegion != null) {
				localRegion = tmpRegion.GetComponent<Region> ();
			} else if (localRegion != null) {
				region = localRegion.netId;
				localRegion.AddItem (this.netId);
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
//				// set all to pre-modifier values first
//				rent = baseRent;
//				Collider[] colliding = Physics.OverlapSphere(c.transform.position, radius);
//				foreach (Collider hit in colliding) {
//					Building b = hit.GetComponent<Building> ();
//					if ((b != null) && b.occupied) {
//						//Lower portion of surrounding rent than restaurant
//						rent += b.rent / 15;
//					}
//				}
//				condition = baseCondition;
//				safety = baseSafety;
//
//				// then apply the modifiers
//				modManager.apply ();
//				rent = (int)(rent * (condition / 100f));
				rent = 0;
			}
		}
	}

	/// <summary>
	/// returns the effect the building has on a lot's attractiveness
	/// </summary>
	/// <returns>The attractiveness effect.</returns>
	public override int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	/// <summary>
	/// Returns an up-to-date rent amount, or 0 if the building lacks tenants
	/// </summary>
	/// <returns>The rent.</returns>
//	public override int getRent() {
//		int currentRent = 0;
//		//Temporarily set as occupied so always generates rent
//		if (occupied) {
//			updateRent ();
//			currentRent = rent;
//		} 
//
//		return currentRent;
//	}

	/// <summary>
	/// Advances the month, applies condition damage, updates the rent, causes fire damage.
	/// </summary>
	public override void advanceMonth() {
		if (isServer) {
			if (condition > 25) {
				damageObject (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner()) { 
				notForSale = false;
			}

			if (earnings > 0) {
				messageOwner(buildingName + " brought in $" + earnings + " this turn!");
				getPlayerOwner ().budget += earnings;
			}

			if (fire) {
				if (condition <= 0) {
					endFire ();
				} else {
					spreadFire ();
					damageObject (50);
				}
			}

			if (condition <= 0) {
				if (!ruin) {
					RpcMakeRuin ();
				}
			} else {
				updateRent ();
			}
		}
		earnings = 0;
	}

	public override void setFire() {
	}

	public override void visitBusiness (Resident res)
	{
		if (res.spendingMoney >= GetCostOfVisit ()) {
			if (condition >= 25) {
				int spent = GetCostOfVisit ();
				res.spendingMoney -= spent;
				addMoney (spent);
			}
		}
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
	/// Gets the cost of visit by pedestrian
	/// </summary>
	/// <returns>The cost of visit.</returns>
	public override int GetCostOfVisit() {
		return COST_PER_VISIT;
	}
}