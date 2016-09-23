using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Decoration : Building {
	const int ATTRACTIVENESS_EFFECT = 10;
	const int TYPENUM = 18;
	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;

		if (isServer) {
			buildingName = buildingNum.ToString();
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = rent * 12;
			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			buildingNum++;
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

	/// <summary>
	/// override for advanceMonth removing standard stuff which is not applicable for decorations
	/// put vehicle/other node spawning here
	/// </summary>
	public override void advanceMonth() {
		if (!validOwner() && !validCompany()) { 
			notForSale = false;
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
	/// empty override for fire starting so that trees don't catch on fire
	/// </summary>
	public override void setFire() {
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadout(NetworkInstanceId pid) {
		string s;
		s = "Decoration" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT;
		return s;
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadoutText(NetworkInstanceId pid) {
		string s;
		s = "Decoration" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT;
		return s;
	}
}
