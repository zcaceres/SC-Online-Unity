using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class DamageableObject : OwnableObject {
	[SyncVar]
	public int baseCondition;
	[SyncVar]
	public int condition;
	[SyncVar]
	public bool fire;             // Is the building on fire?
	protected FireTransform[] fireTrans; //The number of fire transforms connected to the building

	// Use this for initialization
	void Start () {
		if (isServer) {
			condition = 100;
			baseCondition = condition;
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
			}
		}
	}

	/// <summary>
	/// Sets the building on fire.
	/// </summary>
	public virtual void setFire() {
		if (isServer) {
			fire = true;
			GameObject fireObj = (GameObject)Resources.Load ("HouseFire");
			FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform>();
			if (fireTrans.Length < 1) {
				GameObject tmp = (GameObject)Instantiate (fireObj, new Vector3 (gameObject.transform.position.x, getHighest(), gameObject.transform.position.z), fireObj.transform.rotation);
				NetworkServer.Spawn (tmp);
				Debug.LogError ("Object is on fire but has no transforms");
			}
			foreach (FireTransform ft in fireTrans) {
				GameObject tmp = (GameObject)Instantiate (fireObj, ft.transform.position, fireObj.transform.rotation);
				FireKiller fk = tmp.GetComponent<FireKiller> ();
				ft.onFire = true; //Tells the fire transform that it is on fire. All fts must report back OnFire = false for advance month to consider the building not on fire!
				fk.myTransform = ft; //sets the FireKiller's firetransform, which allows it to update the FT about the state of the fire!
				fk.setBuilding (gameObject.GetComponent<Building> ());
				NetworkServer.Spawn (tmp);
			}
		}
	}

	/// <summary>
	/// Ends the fire.
	/// </summary>
	public virtual void endFire() {
		if (isServer) {
			fire = false;
		}
	}

	/// <summary>
	/// Spreads fire to neighbors.
	/// </summary>
	protected virtual void spreadFire() {
		Collider c = GetComponent<Collider> ();
		Collider[] colliding = Physics.OverlapSphere(c.transform.position, 5);
		foreach (Collider hit in colliding) {
			DamageableObject b = hit.GetComponent<DamageableObject> ();

			if (b != null && !b.fire) {
				if (Random.value < .1f) {
					b.setFire ();
				}
			}
		}
	}

	/// <summary>
	/// Damages the building. Decrements base condition & condition--base condition manages the condition without considering modifiers
	/// </summary>
	/// <param name="damage">Damage.</param>
	protected virtual void damageObject(int damage) {
		if (isServer) {
			if ((baseCondition - damage) > 100) {      // don't go above 100
				baseCondition = 100;
				condition = 100;
			} else if ((baseCondition - damage) < 0) { // don't go below 0
				baseCondition = 0;
				condition = 0;
			} else {
				baseCondition -= damage;
				condition -= damage;
			}
		}
	}

	/// <summary>
	/// Gets the cost to restore the building to 100 condition
	/// </summary>
	/// <returns>The repair cost.</returns>
	public virtual int getRepairCost() {
		int repairCost;
		repairCost = (100 - baseCondition) * (baseCost / 100);
		return repairCost;
	}

	/// <summary>
	/// Gets the cost of repairing a single point of condition.
	/// </summary>
	/// <returns>The point repair cost.</returns>
	public virtual int getPointRepairCost() {
		int repairCost;
		repairCost = baseCost / 100;
		return repairCost;
	}

	/// <summary>
	/// Repair the building to 100 condition.
	/// </summary>
	public virtual void repair() {

		if (isServer) {
			condition = 100;
			baseCondition = 100;
		}
	}

	/// <summary>
	/// Repairs the by point.
	/// </summary>
	/// <param name="numPoints">Number points.</param>
	public virtual void repairByPoint(int numPoints) {
		if (isServer) {
			condition += numPoints;
			baseCondition += numPoints;
		}
	}
}
