using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Tenant))]

public class Restaurant : Business {
	const int ATTRACTIVENESS_EFFECT = -20;
	const int TYPENUM = 3;
	const int COST_PER_VISIT = 100;
	public GameObject deliveryJobActivator;
	private static string[] rSmallFirst = {
		"Mom's",
		"Dad's",
		"McFonald's",
		"Hungry",
		"Greasy Spoon",
		"Takeout",
		"Vegan",
		"The Hippie",
		"Italianni's",
		"Scooter's",
		"Taco",
		"Fatso's",
		"Diabetes", 
		"Chompy's",
		"Gomper's",
		"Family",
		"Belly",
		"Food",
		"Takeout",
		"Meat",
		"Gluttony",
		"Maple",
		"Gourmet",
		"La Casa"
	};
	private static string[] rSmallLast = { "Place", "Eatery", "Restaurant", "Restaurant", "Restaurant", "Restaurant", "Restaurant", "Restaurant",
		"Diner", "Diner", "Diner", "Diner"};
	
	public int radius;

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		radius = 50;
		//Get DeliveryJobActivator to enable jobs on occupancy
		deliveryJobActivator = gameObject.transform.Find("DeliveryJobActivator").gameObject;
		deliveryJobActivator.SetActive (false);
		type = TYPENUM;

		if (isServer) {
			skillLevel = 0;
			baseRent = 100;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 4500;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			type = 3;
			neededWorkers = 5;
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

	public override void updateRent() {
		if (isServer) {
			if (ruin) {
				rent = 0;
			} else {
				// set all to pre-modifier values first
//				rent = baseRent;
//				Collider[] colliding = Physics.OverlapSphere(c.transform.position, radius);
//
//				foreach (Collider hit in colliding) {
//					Building b = hit.GetComponent<Building> ();
//					if ((b != null) && b.occupied && !(b is Business)) {
//						rent += b.rent / 10;
//					}
//				}
//
//				condition = baseCondition;
//				safety = baseSafety;
//
//				// then apply the modifiers
//				modManager.apply ();
//				rent = (int)(rent * (condition / 100f) * (workers.Count / (float)neededWorkers));
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
	/// Advances the month, applies condition damage, updates the rent, causes fire damage.
	/// </summary>
	public override void advanceMonth() {
		if (isServer) {
			if (condition > 25) {
				damageBuilding (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner() && !validCompany()) { 
				notForSale = false;
			} else if (occupied) {                         // occupied, apply effects from the tenant
				tenant.clearButtons();
				damageBuilding(tenant.condition());
				tenant.applyEffects ();
				paying = tenant.willPay ();
				if (!ruin) {
					//offerDeliveryJob (occupied);
				}
				if (!paying) {
					messageOwner(tenant.resident.residentName + " failed to pay rent this month!");
				}
				if (earnings > 0) {
					messageOwner(buildingName + " brought in $" + earnings + " this turn!");
					getPlayerOwner ().budget += earnings;
				}
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
		earnings = 0;
		//offerDeliveryJob (occupied);
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
	}

	public virtual void offerDeliveryJob(bool occupied) {
		RpcSetActive (occupied);
		if (deliveryJobActivator.activeInHierarchy) {
		} else {
			messageOwner(buildingName + " needs a manager to offer jobs.");
		}
	}


	/// <summary>
	/// Gets the cost of visit by pedestrian
	/// </summary>
	/// <returns>The cost of visit.</returns>
	public override int GetCostOfVisit() {
		return COST_PER_VISIT;
	}

	// RPC to control 
	[ClientRpc(channel=CHANNEL)]
	private void RpcSetActive(bool occupied) {
		if (deliveryJobActivator != null) {
			deliveryJobActivator.SetActive (occupied);
		}
	}
}
