using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Tenant))]

public class CheapRestaurant : Business {
	const int ATTRACTIVENESS_EFFECT = -20;
	const int COST_PER_VISIT = 40;
	const int TYPENUM = 3;
	public GameObject deliveryJobActivator;
	private static string[] rSmallFirst = {
		"Hot Dog",
		"Skeevy",
		"Smiley",
		"Yummy",
		"We Promise",
		"True Quality",
		"Questionable",
		"Health Code Violation",
		"All Night",
		"Shattered Dreams",
		"End of the Line",
		"Last Stop",
	};
	private static string[] rSmallLast = { "Hot Dogs", "Pizzeria", "Eatery", "Diner", "Diner", "Diner"};
	
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
			baseRent = 40;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 2000;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			neededWorkers = 3;
			buildingName = nameGen ();
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			objectNum++;
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
				damageObject (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner()) { 
				notForSale = false;
			} else if (occupied) {                         // occupied, apply effects from the tenant
				tenant.clearButtons();
				damageObject(tenant.condition());
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
					damageObject (50);
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


	/// <summary>
	/// Function called from BusinessVisitNode when a resident enters the collider around the business.
	/// Overridden by child business classes like this one to discriminate based on resident skill level and other factors.
	/// </summary>
	/// <param name="res">Resident</param>
	public override void visitBusiness(Resident res) {
		if (res.skill <= 1) { //Checks to make sure resident has less than 2 skill level (cheap restaurant type)
			if (Random.Range (0, 100) <= condition) {
				if (res.spendingMoney >= GetCostOfVisit ()) {
					int spent = GetCostOfVisit ();
					res.spendingMoney -= spent;
					addMoney (spent);
				}
			}
		}
	}



	public virtual void offerDeliveryJob(bool occupied) {
		RpcSetActive (occupied);
		if (deliveryJobActivator.activeInHierarchy) {
		} else {
			messageOwner(buildingName + " needs a manager to offer jobs.");
		}
	}

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
