using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Junk : OwnableObject {
	const int ATTRACTIVENESS_EFFECT = -30;
	const int TYPENUM = 25;
	void Start () {

		if (isServer) {
			cost = 1000;
			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
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
		}
	}

	/// <summary>
	/// overrided appraise function--junk has no cost
	/// </summary>
	public override int appraise() {
		return 0;
	}

	/// <summary>
	/// returns the effect the building has on a lot's attractiveness
	/// </summary>
	/// <returns>The attractiveness effect.</returns>
	public override int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadout(NetworkInstanceId pid) {
		string s;
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else  {
			ownerName = getPlayer(owner).getName();
		}
		s = "Junk" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT + "\nOwner: " + ownerName;
		return s;
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadoutText(NetworkInstanceId pid) {
		string s;
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else  {
			ownerName = getPlayer(owner).getName();
		}
		s = "Junk" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT + "\nOwner: " + ownerName;
		return s;
	}
}
