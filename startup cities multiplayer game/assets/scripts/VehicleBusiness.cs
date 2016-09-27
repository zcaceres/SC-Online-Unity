using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class VehicleBusiness : NetworkBehaviour
{
	const int COST_PER_VISIT = 20;
	private IceCreamTruckVehicle iceCreamTruck;
	protected const int CHANNEL = 1;
	//const int TYPENUM = 3;

	void Start ()
	{
		iceCreamTruck = GetComponent<IceCreamTruckVehicle> ();
	}

	/// <summary>
	/// Function called from BusinessVisitNode when a resident enters the collider around the business.
	/// Overridden by child business classes like this one to discriminate based on resident skill level and other factors.
	/// </summary>
	/// <param name="res">Resident</param>
	public void visitBusiness (Resident res)
	{
		Player p = iceCreamTruck.getPlayerOwner ();
		if (res.skill <= 1) { //Checks to make sure resident has less than 2 skill level (cheap restaurant type)
			if (Random.Range (0, 100) <= iceCreamTruck.condition) {
				if (res.spendingMoney >= GetCostOfVisit ()) {
					int spent = GetCostOfVisit ();
					res.spendingMoney -= spent;
					p.CmdAddMoney (p.netId, spent);
					Debug.Log ("Sent " + spent + " to owner " + p.netId);
				}
			} else {
				Debug.Log ("condition too crappy for sale");
			}
		}
	}

	public int GetCostOfVisit ()
	{
		return COST_PER_VISIT;
	}

}
