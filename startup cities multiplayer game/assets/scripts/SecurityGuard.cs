using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;

public class SecurityGuard : Building {
	const int ATTRACTIVENESS_EFFECT = 0;
	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;

		if (isServer) {
			lowestSkill = 0;
			buildingName = "Security Guard";
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 400;

			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
			upkeep = cost / UPKEEP_PORTION; //edited for monthly salary!
			officeName = "None";
			type = 18;
			typeName = buildingTypes [type];
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
			GameObject tmpRegion = getLocalInstance (region);
			if (tmpRegion != null) {
				localRegion = tmpRegion.GetComponent<Region> ();
			} else if (localRegion != null) {
				region = localRegion.netId;
				localRegion.AddItem (this.netId);
			}
			updateRent ();
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

	}

	/// <summary>
	/// Sets the building on fire.
	/// </summary>
	public override void setFire() {
		
	}
}
