using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Decoration : OwnableObject {
	const int ATTRACTIVENESS_EFFECT = 10;
	const int TYPENUM = 18;
	void Start () {

		if (isServer) {
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
		s = "Decoration" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT + "\nOwner: " + ownerName;
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
		s = "Decoration" + "\nAttractiveness Effect: " + ATTRACTIVENESS_EFFECT + "\nOwner: " + ownerName;
		return s;
	}
}
