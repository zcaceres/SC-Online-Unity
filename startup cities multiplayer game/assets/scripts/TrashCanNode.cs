using UnityEngine;
using System.Collections;

public class TrashCanNode : MonoBehaviour
{
	public Building building;
	private CapsuleCollider beaconCollider;
	private bool playerChecker;
	private GameObject winningPlayer;
	private TrashQuestManager trashQuestManager;

	//This function links the TrashQuestNode instance that activated the job to the marker for the job
	public void LinkJobMarker (GameObject activator)
	{
		trashQuestManager = activator.GetComponent<TrashQuestManager> ();

	}

	void OnTriggerEnter (Collider playerCollider)
	{
		//Checks for collision on the Beacon object in the scene. Makes sure it is a player that has collided
		playerChecker = playerCollider.CompareTag ("Player");
		Player p = playerCollider.gameObject.GetComponent<Player> ();
		if (playerChecker) {
			if (p.isLocalPlayer) {
				winningPlayer = playerCollider.gameObject;
				Player tmp = winningPlayer.GetComponent<Player> ();
				//Calls the CompleteDeliveryQuest function from TrashQuestManager script
				trashQuestManager.CompleteDeliveryQuest (winningPlayer);
				Destroy (this.gameObject); //Destroys the beacon object
			}
		} else {
			return;
		}
	}
}
