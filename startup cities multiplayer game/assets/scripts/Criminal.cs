using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Criminal : NetworkBehaviour
{
	
	/// <summary>
	/// Triggers the crime event. Called before criminal despawns.
	/// </summary>
	public void TriggerCrimeEvent ()
	{
		bool playerNotNotified = true; //Has player already received a message? Used to prevent duplicate notifications.
		Object tmp = Resources.Load ("CrimeEvent");
		GameObject crimeEvent = (GameObject)Instantiate (tmp);
		NetworkServer.Spawn (crimeEvent);
		int amount = Random.Range (12, 35); //Random lowering of safety
		Collider[] colliding = Physics.OverlapSphere (gameObject.transform.position, 35);
		foreach (Collider hit in colliding) {
			Building b = hit.GetComponent<Building> ();
			if (b != null && playerNotNotified && b.validOwner()) {
				if (b.inNeighborhood ()) { //First try to notify neighborhood owner
					Neighborhood n = b.getNeighborhood ();
					if (playerNotNotified) {
						n.RpcMessageOwner ("A crime has been commited in the vicinity of " + n.buildingName + "!");
						playerNotNotified = false;
					}
				} else if (b.GetComponent<Lot> () != null) { //Then try to notify lot owner
					Lot l = b.GetComponent<Lot> ();
					if (playerNotNotified) {
						l.RpcMessageOwner ("A crime has been commited in the vicinity of " + l.buildingName + "!");
						playerNotNotified = false;
					}
				} else { //If all else fails, notify building owner
					if (playerNotNotified && b.gameObject.GetComponent<SecurityGuard>() == null) {
						b.RpcMessageOwner ("A crime has been commited in the vicinity of " + b.buildingName + "!");
						playerNotNotified = false;
					}
				}
			} if (b != null) {
				b.damageBuildingSafety (amount);
			}
		}
	}


	/// <summary>
	/// When criminal is caught in the SecurityRadius, this happens!
	/// </summary>
	public void CaughtBySecurity () {
		//Player surrender animation anim.Play()
		Destroy(gameObject);

	}


}
