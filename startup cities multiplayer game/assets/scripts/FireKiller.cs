using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class FireKiller : NetworkBehaviour {

	[SyncVar]
	public int fireLife; // the 'hp' of the fire which is decremented via Hose's particle colliders

	private Building myBuilding;
	private Vehicle myVehicle;
	public FireTransform myTransform; //The fire transform that this fire object spawned on

	void Start () {
		Collider[] colliding = Physics.OverlapSphere(gameObject.transform.position, 5);
		if (isServer) { //Have server set the fire's life
			fireLife = UnityEngine.Random.Range (150, 300); //Original was 200-350
		}
	}

	void Update() {
		if (isServer) {
			if ((myBuilding != null) && !myBuilding.fire) {
				RpcSelfDestruct ();
			} else if (fireLife <= 0 && myBuilding != null) { //Kills the fire if player sprays enough water from hose PS on the fire's collider
				myTransform.onFire = false; //Tells the FT to report back to the building that one fire in its AdvanceMonth calculation
				Building b = myTransform.GetComponentInParent<Building> ();
				b.RpcMessageOwner ("You put out a fire!");
				b.CheckFireState (); //Checks if this is the last fire on the building. If it is, ends the fire state for the building
				RpcSelfDestruct ();
			}
			if ((myVehicle != null) && !myVehicle.fire) {
				RpcSelfDestruct ();
			} else if (fireLife <= 0 && myVehicle != null) { //Kills the fire if player sprays enough water from hose PS on the fire's collider
				myTransform.onFire = false; //Tells the FT to report back to the building that one fire in its AdvanceMonth calculation
				Vehicle v = myTransform.GetComponentInParent<Vehicle> ();
				v.RpcMessageOwner ("You put out a fire!");
				v.CheckFireState (); //Checks if this is the last fire on the building. If it is, ends the fire state for the building
				RpcSelfDestruct ();
			}
			if (myVehicle != null) {
				transform.position = myTransform.transform.position;
			}
		}
	}

	/// <summary>
	/// Sets the building which is on fire.
	/// </summary>
	/// <param name="b">The building.</param>
	public void setBuilding(Building b) {
		myBuilding = b;
	}


	public void setVehicle (Vehicle v) {
		myVehicle = v;
	}

	[ClientRpc]
	private void RpcSelfDestruct() {
		Destroy (gameObject);
	}
}
