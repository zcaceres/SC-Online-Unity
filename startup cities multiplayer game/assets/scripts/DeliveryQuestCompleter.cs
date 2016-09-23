using UnityEngine;
//using UnityEngine.Networking;
using System.Collections;

public class DeliveryQuestCompleter : MonoBehaviour {//NetworkBehaviour {
	public Building building;
	private CapsuleCollider beaconCollider;
	private bool playerChecker;
	private GameObject winningPlayer;
	private GameObject deliveryQuest;
	private DeliveryQuest deliveryQuestController;

	//This function links the DeliveryJobNode instance outside a restaurant that activated the job to the marker for the job
	public void LinkJobMarker(GameObject activator) {
		deliveryQuestController = activator.GetComponent<DeliveryQuest> ();

	}

	void OnTriggerEnter (Collider playerCollider) {
		//Checks for collision on the Beacon object in the scene. Makes sure it is a player that has collided
		playerChecker = playerCollider.CompareTag ("Player");
		if (playerChecker) {
			if (playerCollider.GetComponentInParent<Player> ().isLocalPlayer) {
				winningPlayer = playerCollider.gameObject;
				Player tmp = winningPlayer.GetComponent<Player> ();
				if (building != null) { // remove the building from the player's list of delivery destinations
					foreach (Building b in tmp.deliveryDestinations) {
						if (b.id == building.id) {
							tmp.deliveryDestinations.Remove (b);
							tmp.spawnNotifications ();
							if (b.id == tmp.destinationBuilding) {
								tmp.overlandMap.makeBeacon (b);
								tmp.destinationBuilding = -1;
							}
							break;
						}
					}
				}
				//Calls the CompleteDeliveryQuest function from DeliveryQuest script
				Destroy (this.gameObject); //Destroys the beacon object
				deliveryQuestController.CompleteDeliveryQuest (winningPlayer);
			} else {
				return;
			}
		} else {
			return;
		}
	}
}

