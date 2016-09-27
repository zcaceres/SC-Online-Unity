using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

//Should require junkyard tenant here

public class Junkyard : Building {
	const int ATTRACTIVENESS_EFFECT = 0;
	public bool activePark;
	private Player auctionPlayer;

	private static string[] rSmallFirst = {
		"Smelly",
		"Creepy",
		"Nasty",
		"Dirty",
		"Depressing",
	};

	private static string[] rSmallLast = { "Junkyard" };

	// Use this for initialization
	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;

		if (isServer) {
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			//Cost cannot be function of 0 rent, changed to integer
			cost = 10000;
			baseCost = cost;
			//Upkeep is integer
			upkeep = 10;
			officeName = "None";
			type = 6;
			typeName = buildingTypes [type];
			buildingName = nameGen ();
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			//notForSale = true;
			buildingNum++;
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
				localLot.addObject(this.netId);
			}
			setup ();
			//updateNeighborhoodValue ();
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
	/// Sets up mods and rent for the park. Created because of a timing error on networking.
	/// </summary>
	private void setup () {
		updateRent ();
	}



	/// <summary>
	/// Generates a name for the building from the park string above.
	/// </summary>
	/// <returns>The gen.</returns>
	private string nameGen () {
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}


/// <summary>
/// Removes the junkyard modifier once the park is bought and cleaned. Adds positive Pretty Park modifier
/// </summary>
	private void modGenerator () {
		modManager.addModUnique ("Junkyard", .8f, 1f, .9f, 42f, 26);
	}


	/// <summary>
	/// Starts an auction if the park is unowned and all surrounding buildings are owned.
	/// </summary>
	/// <param name="buildingId">Building identifier.</param>
	public void ParkAuction (NetworkInstanceId buildingId) {
		if (isServer) {
			auctionPlayer = GameObject.FindGameObjectWithTag ("Player").GetComponent<Player> ();
			auctionPlayer.CmdAuction (buildingId);
		}
	}




	/// <summary>
	/// Advances the month, applies condition damage, updates the rent, causes fire damage.
	/// </summary>
	public override void advanceMonth () {
		if (isServer) {
			if (condition > 25) {
				damageBuilding (1); 
			}
			if (safety < 100) {
				damageBuildingSafety (-1); // recover 1 safety each month
			}
				
			if (validOwner()) {
				activePark = true;
			}
			if (!validOwner()) { 
				notForSale = false;
			} else if (occupied) {                         // occupied, apply effects from the tenant
				tenant.clearButtons ();
				damageBuilding (tenant.condition ());
				tenant.applyEffects ();
				paying = tenant.willPay ();

				if (!paying) {
					this.messageOwner (tenant.resident.residentName + " failed to pay rent this month!");
				}
			}
			//	CheckParkLevel ();

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
					RpcMakeRuin ();
				}
			} else {
				//flipOccupancy ();
				updateRent ();
			}
			modGenerator ();
			parkChecker ();
		}
	}
		
	/// <summary>
	/// Checks all buildings surrounding Park properties for ownership. If they are owned, game puts the park on auction
	/// </summary>
	private void parkChecker () {
		if (!activePark) {
			int buildingList = 0;
			int ownedBuildings = 0;
			if (isServer) {
				var radius = 40;
				Collider[] colliding = Physics.OverlapSphere (transform.position, radius);
				foreach (Collider o in colliding) {
					Building buildingTest = o.gameObject.GetComponent<Building> ();
					if (buildingTest != null && buildingTest.id != id) {
						buildingList += 1;
						if (buildingTest.validOwner()) {
							ownedBuildings += 1;
						}
					}
				}//Triggers park auction
				if (buildingList == ownedBuildings) {
					ParkAuction (GetComponent<NetworkIdentity> ().netId);
				}
			}
		}
	}
}