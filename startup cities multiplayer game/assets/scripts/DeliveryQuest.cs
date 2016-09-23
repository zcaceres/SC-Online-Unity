using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
//using UnityEngine.Networking;
using System.Collections;

public class DeliveryQuest : NetworkBehaviour {
	const int DEFAULT_REWARD = 100;

	private GameObject[] questBeaconWeb;
	private GameObject questBeacon;
	private GameObject questParticleMarker;
	private Vector3 questBeaconLocation;
	private CapsuleCollider beaconCollider;
	private House deliveryHouse;
	private bool occupiedChecker;
	private bool playerChecker;
	private bool jobActive;
	private Player localPlayer;
	private DeliveryQuestCompleter linkMarker;
	private NetworkInstanceId localPlayerID;
	private Restaurant restaurant;
	//Set rewards for delivery jobs here!
	private int rewardXP = 10;
	[SyncVar]
	private int rewardMoney = 100;

	void Start() {
		restaurant = transform.parent.GetComponentInParent<Restaurant> ();
	}

	void OnTriggerEnter (Collider playerCollider) {
		//Checks for collision on the deliveryquest object in scene
		playerChecker = playerCollider.CompareTag ("Player");
		if (playerChecker) {
			localPlayer = playerCollider.GetComponentInParent<Player> ();
		}
		if (localPlayer != null && localPlayer.isLocalPlayer) {
			if (playerChecker && !jobActive) { //Checks to make sure Player does not have a job from the same restaurant already
				BeginDeliveryQuest (localPlayer);
			} else if (jobActive) {
				localPlayer.showMessage("You already have a delivery job from this business!");
			}
		}
	}

	public void BeginDeliveryQuest (Player localPlayer)
	{
		//Finds spawn point array (attached to building prefabs)
		questBeaconWeb = GameObject.FindGameObjectsWithTag ("SpawnPoint");
		GenerateDeliveryLocation (questBeaconWeb, localPlayer);
	}

	//Takes random spawn point position and check that it is a residential property (house, apartment building etc)
	void GenerateDeliveryLocation (GameObject[] questBeaconWeb, Player localPlayer)
	{
		List<GameObject> occupiedResidences = new List<GameObject> ();
		List<Transform> lots = new List<Transform> ();
		List<Transform> parents = new List<Transform> ();
		List<Transform> levels = new List<Transform> ();
		List<GameObject> go = new List<GameObject> ();
		List<Building> buildings = new List<Building> ();

		foreach (GameObject g in questBeaconWeb) { //Takes all properties with SpawnPoints as children and finds if they are a house
			if (g.gameObject.GetComponent<DeliveryNode> ().readyForDelivery == true) {
				occupiedResidences.Add (g.gameObject);
				Debug.Log ("Ready for delivery added " + g.gameObject.name);
			}
		}

		if (occupiedResidences.Count == 0) { //If all houses are unoccupied, no delivery jobs can be found
			if (localPlayer.isLocalPlayer) {
				localPlayer.showMessage("No jobs available. Try again later or talk to the owner.");
				jobActive = false;
				return;
			}
		} else {
			questBeacon = (GameObject)occupiedResidences.ElementAt (Random.Range (0, occupiedResidences.Count)); //Randomly selects an occupied house
			questBeaconLocation = questBeacon.transform.position;
			jobActive = true; //Prevents getting multiple jobs from the same job activator
			SpawnDeliveryQuest (questBeaconLocation, localPlayer, questBeacon.transform.parent.GetComponentInChildren<Building>()); //Spawns the delivery quest
		}
	}

	//If occupied and a house, spawns a delivery quest
	void SpawnDeliveryQuest (Vector3 questBeaconLocation, Player localPlayer, Building b) {
		localPlayer.showMessage("Beginning delivery job!");
		questParticleMarker = (GameObject) Resources.Load ("DeliveryQuestMarker");

		//Spawns a quest beacon (visual marker of delivery quest)
		if (localPlayer.isLocalPlayer) {
			localPlayer.deliveryDestinations.Add (b);
			localPlayer.spawnNotifications ();
			GameObject tmp = (GameObject)Instantiate (questParticleMarker, new Vector3 (questBeaconLocation.x, questBeaconLocation.y + 22, questBeaconLocation.z), Quaternion.identity);
			linkMarker = tmp.GetComponent<DeliveryQuestCompleter> (); //Grabs QuestCompletor from the market
			linkMarker.building = b;
			linkMarker.LinkJobMarker (this.gameObject); //Sends the current DeliveryJob activator to the quest completor marker so that they are linked
			//NetworkServer.Spawn (tmp);
		}
	}

	public void CompleteDeliveryQuest (GameObject winningPlayer) {
		//When a player collides with the delivery quest beacon, awards them money and experience
		localPlayer = winningPlayer.GetComponent<Player>(); //The player that collided with the QuestCompletor's trigger collider is found here
		localPlayerID = localPlayer.GetComponent<NetworkIdentity>().netId;
		if (localPlayer.isLocalPlayer) {
			localPlayer.showMessage("You earned " + rewardMoney + " dollars and " + rewardXP + " experience.");
			localPlayer.CmdAddExperience (localPlayerID, rewardXP);
			localPlayer.CmdAddMoney (localPlayerID, rewardMoney);
			if (restaurant != null) {
				if (restaurant.getOwner () != -1) {
					localPlayer.CmdAddMoney (restaurant.getPlayerOwner().GetComponent<NetworkIdentity>().netId, rewardMoney);
					localPlayer.CmdMessage(restaurant.getPlayerOwner().GetComponent<NetworkIdentity>().netId, "You earned " + rewardMoney + " from a delivery.");
				}
			}
			jobActive = false; //Allows for a new job to be taken from the activator
		}
	}

	public void setDefault() {
		rewardMoney = DEFAULT_REWARD;
	}

	public void addExtraReward(int i) {
		rewardMoney += i;
	}
}
