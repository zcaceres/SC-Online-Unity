using UnityEngine;
using System.Collections;


/// <summary>
/// Class to handle driver entering vehicle from driver's side door
/// </summary>
public class EnterVehicle : MonoBehaviour {
	private Vehicle vehicle;
	protected bool canEnter;
	protected Player p;
	void Start() {
		vehicle = GetComponentInParent<Vehicle> ();
	}

	void Update() {
		if (Input.GetKeyDown (KeyCode.F)) {
			if (canEnter && p != null) {
				vehicle.StartVehicle (p);
				canEnter = false;
			}
		}
	}

	/// <summary>
	/// Notifies player that they can get into a vehicle that they own
	/// </summary>
	/// <param name="coll">Coll.</param>
	protected virtual void OnTriggerEnter (Collider coll) {
		if (coll.CompareTag("Player")) { //Check player ownership here
			p = coll.gameObject.GetComponent<Player> ();
			if (vehicle.getOwner () == p.id) {
				p.message = "Press F to drive.";
				canEnter = true;
			}
		}
	}

	protected void OnTriggerExit(Collider coll) {
		if (coll.CompareTag ("Player")) {
			canEnter = false;
			p = null;
		}
	}
}

