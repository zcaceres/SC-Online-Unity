using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using System.Collections;


public class TrashQuestManager : NetworkBehaviour
{

	const int DEFAULT_REWARD = 50;

	private GameObject[] trashCanWeb;
	private GameObject trashCanBeacon;
	private GameObject trashCanParticleMarker;
	private Vector3 trashCanBeaconLocation;
	private CapsuleCollider trashCollider;
	//	private bool occupiedChecker;
	private bool jobActive;
	private TrashCanNode linkMarker;
	private NetworkInstanceId localPlayerID;
	//Set rewards for delivery jobs here!
	private int rewardXP = 10;
	[SyncVar]
	private int rewardMoney = 50;

	void Start ()
	{
		//Get trash quest start object location here
		//trashQuestStart //= transform.parent.GetComponentInParent<Restaurant> ();
	}

	void OnTriggerEnter (Collider playerCollider)
	{
		bool playerChecker;
		playerChecker = playerCollider.CompareTag ("Player");
		Player p = playerCollider.GetComponent<Player> ();
		;
		if (playerChecker && p != null && p.isLocalPlayer) {
			if (!jobActive) { //Checks to make sure Player does not have a job from the same trashgiver already
				BeginDeliveryQuest (p);
			} else if (jobActive) {
				p.showMessage ("You already work for the city collecting trash!");
			}
		}
	}

	public void BeginDeliveryQuest (Player localPlayer)
	{
		//Finds spawn point array (attached to building prefabs)
		trashCanWeb = GameObject.FindGameObjectsWithTag ("Trashcan");
		GenerateDeliveryLocation (trashCanWeb, localPlayer);
	}

	//Takes random spawn point position and check that it is a residential property (house, apartment building etc)
	void GenerateDeliveryLocation (GameObject[] trashCanWeb, Player localPlayer)
	{
		List<GameObject> trashCans = new List<GameObject> ();
		foreach (GameObject g in trashCanWeb) {
			trashCans.Add (g);
		}
		trashCanBeacon = (GameObject)trashCans.ElementAt (Random.Range (0, trashCans.Count)); //Randomly selects a trash can
		trashCanBeaconLocation = trashCanBeacon.transform.position;
		//Prevents getting multiple jobs from the same job activator
		jobActive = true;
		SpawnDeliveryQuest (trashCanBeaconLocation, localPlayer); //Spawns the delivery quest
	}

	//If occupied and a house, spawns a delivery quest
	void SpawnDeliveryQuest (Vector3 questBeaconLocation, Player localPlayer)
	{
		localPlayer.showMessage ("Collect the trash!");
		trashCanParticleMarker = (GameObject)Resources.Load ("TrashQuestMarker");
		//Spawns a quest beacon (visual marker of trash quest)
		if (localPlayer.isLocalPlayer) {
			localPlayer.spawnNotifications ();
			GameObject tmp = (GameObject)Instantiate (trashCanParticleMarker, new Vector3 (trashCanBeaconLocation.x, trashCanBeaconLocation.y + 22, trashCanBeaconLocation.z), Quaternion.identity);
			linkMarker = tmp.GetComponent<TrashCanNode> (); //Grabs TrashCanNode from the marker
			linkMarker.LinkJobMarker (this.gameObject); //Sends the current trashcanquest activator to the quest completor marker so that they are linked
		}
	}

	public void CompleteDeliveryQuest (GameObject winningPlayer)
	{
		//When a player collides with the delivery quest beacon, awards them money and experience
		Player p = winningPlayer.GetComponent<Player> (); //The player that collided with the QuestCompletor's trigger collider is found here
		localPlayerID = p.GetComponent<NetworkIdentity> ().netId;
		if (p.isLocalPlayer) {
			p.showMessage ("You earned " + rewardMoney + " dollars and " + rewardXP + " experience.");
			p.CmdAddExperience (localPlayerID, rewardXP);
			p.CmdAddMoney (localPlayerID, rewardMoney);
			jobActive = false; //Allows for a new job to be taken from the activator
		}
	}

	public void setDefault ()
	{
		rewardMoney = DEFAULT_REWARD;
	}

	public void addExtraReward (int i)
	{
		rewardMoney += i;
	}
}
