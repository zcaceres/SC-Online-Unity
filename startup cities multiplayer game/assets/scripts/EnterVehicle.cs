using UnityEngine;
using System.Collections;

public class EnterVehicleDriver : MonoBehaviour {
	private Vehicle vehicle;

	void Start() {
		vehicle = GetComponentInParent<Vehicle> ();
	}


	/// <summary>
	/// Notifies player that they can get into a vehicle that they own
	/// </summary>
	/// <param name="coll">Coll.</param>
	void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag("Player")) { //Check player ownership here
			Player p = coll.gameObject.GetComponent<Player> ();
			if (vehicle.getOwner () == p.id) {
				p.message = "Press F to get in.";
			}
		}
	}

	/// <summary>
	/// Permits player to enter vehicle while in EnterVehicle collider area.
	/// </summary>
	/// <param name="coll">Coll.</param>
	void OnTriggerStay (Collider coll)
	{
		if (coll.CompareTag ("Player")) { // check player ownership
			Player p = coll.gameObject.GetComponent<Player> ();
			if (Input.GetKeyDown (KeyCode.F)) {
				if (vehicle.getOwner () == p.id && !vehicle.eligibleToExit) {
					Debug.LogError ("eligible to enter vehicle");
					vehicle.StartVehicle (p);
				}
			}
		}
	}


}
