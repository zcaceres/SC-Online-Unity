using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AuctionManager : NetworkBehaviour {

	const float AUCTION_LENGTH = 10;

	[SyncVar]
	public float time;
	[SyncVar]
	public int currentBid;
	[SyncVar]
	public string leaderName;
	[SyncVar]
	private string buildingText;
	[SyncVar]
	private string buildingName;

	private Text auctionInfo;
	private Text bidText;
	private Text timerText;
	private Building building;
	public NetworkInstanceId leaderId;
	private bool doOnce;
	private GameObject cam;
	// Use this for initialization
	void Start () {
		transform.SetParent (GameObject.Find ("Canvas").transform, false);
		if (isServer) {
			time = AUCTION_LENGTH;
			currentBid = 0;
			leaderName = "No Bids";
		} else {
			//time = AUCTION_LENGTH;
		}

		doOnce = true;
		auctionInfo = transform.Find ("AuctionInfo").GetComponent<Text> ();
		bidText = transform.Find ("CurrentBid").GetComponent<Text> ();
		timerText = transform.Find ("AuctionTimer").GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (isServer) {
			time -= Time.deltaTime;
		}
			

		if (building != null) {
			if (doOnce) {
				GameObject AuctionCam = (GameObject)Resources.Load ("AuctionCam");
				cam = (GameObject)Instantiate (AuctionCam, new Vector3(building.transform.position.x, building.transform.position.y + 50, building.transform.position.z), 
					AuctionCam.transform.rotation);
				NetworkServer.Spawn (cam);
				doOnce = false;
			}

			if (time <= 0) {
				if (currentBid > 0) {
					if (isServer) {
						RpcBuyerMessage ();
						GameObject p;
						building.cost = currentBid;
						p = NetworkServer.FindLocalObject (leaderId);
						if (p == null) {
							p = ClientScene.FindLocalObject (leaderId);
						}

						building.onAuction = false;
						building.notForSale = false;

						if (p.GetComponent<Player> () != null) {
							Player player = p.GetComponent<Player> ();
							player.CmdBuy (leaderId, building.gameObject.GetComponent<NetworkIdentity> ().netId);
						}
					}
				} else {
					building.onAuction = false;
					RpcNoBuyerMessage ();
					//selfDestruct ();
				}
			}
			updatePanel ();
		} else {
			updatePanel ();
		}
	}

	/// <summary>
	/// Sets the building for the auction.
	/// </summary>
	/// <param name="buildingId">Building identifier.</param>
	public void setBuilding(NetworkInstanceId buildingId) {
		GameObject b;

		b = NetworkServer.FindLocalObject (buildingId);
		if (b == null) {
			b = ClientScene.FindLocalObject (buildingId);
		}

		building = b.GetComponent<Building> ();
	}

	/// <summary>
	/// Resets the time to the max auction length, used when a bid is placed.
	/// </summary>
	public void resetTime() {
		if (isServer) {
			time = AUCTION_LENGTH;
		}
	}

	/// <summary>
	/// Sets the listeners for the buttons for the passed player.
	/// </summary>
	/// <param name="pid">The player's net id.</param>
	public void setButtons(NetworkInstanceId pid) {
		GameObject p;

		p = NetworkServer.FindLocalObject (pid);
		if (p == null) {
			p = ClientScene.FindLocalObject (pid);
		}

		Player player = p.GetComponent<Player> ();

		transform.Find ("Close").GetComponent<Button> ().onClick.AddListener (delegate {
			hide();
		});
		transform.Find ("Bid100").GetComponent<Button> ().onClick.AddListener (delegate {
			player.CmdAdd100 (gameObject.GetComponent<NetworkIdentity> ().netId, pid);
		});

		transform.Find ("Bid1000").GetComponent<Button> ().onClick.AddListener (delegate {
			player.CmdAdd1000 (gameObject.GetComponent<NetworkIdentity> ().netId, pid);
		});

		transform.Find ("Bid10000").GetComponent<Button> ().onClick.AddListener (delegate {
			player.CmdAdd10000 (gameObject.GetComponent<NetworkIdentity> ().netId, pid);
		});
	}

	public void sendMessage(string s) {
		GameObject error = (GameObject)Instantiate (Resources.Load ("ErrorPanel"));
		error.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		Text tmp = error.transform.Find ("ErrorMessage").GetComponent<Text> ();
		tmp.text = s;
	}

	/// <summary>
	/// Updates the auction display panel.
	/// </summary>
	private void updatePanel() {
		if (isServer) {
			buildingText = building.getReadoutAuction();
			buildingName = building.buildingName;
		}

		auctionInfo.text = buildingText;
		bidText.text = "Current Bid: " + currentBid + "\n(" + leaderName + ")";
		timerText.text = "Time Remaining: " + ((int)time + 1).ToString ();;
	}

	/// <summary>
	/// kill the auction panel and the auction camera
	/// </summary>
	private void selfDestruct() {
		Destroy (cam);
		Destroy (gameObject);
	}

	/// <summary>
	/// destroy the camera and hide the panel
	/// </summary>
	private void hide() {
		Destroy (cam);
		gameObject.transform.position = new Vector3 (0, -4000, 0);
	}

	[ClientRpc]
	private void RpcNoBuyerMessage() {
		sendMessage (buildingName + " auction ended. No bids were placed.");
		selfDestruct ();
	}

	[ClientRpc]
	private void RpcBuyerMessage() {
		sendMessage (buildingName + " sold for " + currentBid + " to " + leaderName + "!");
		selfDestruct ();
	}
}
