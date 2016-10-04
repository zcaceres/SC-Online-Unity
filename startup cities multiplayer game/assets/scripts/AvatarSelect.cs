using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AvatarSelect : NetworkBehaviour {

	private GameObject panel;
	private Button player;
	private Button spectator;
	private List<GameObject> prefabs;
	// Use this for initialization
	void Start () {
		if (isServer) {
			prefabs = new List<GameObject> ();
			prefabs.Add((GameObject)Resources.Load ("The_Boss"));
			prefabs.Add((GameObject)Resources.Load ("Spectator"));
		}
		if (!isLocalPlayer) {
			return;
		}
		panel = (GameObject)Instantiate(Resources.Load ("uiElements/AvatarChoicePanel"));
		panel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		player = panel.transform.Find ("Play").GetComponent<Button> ();
		spectator = panel.transform.Find ("Observe").GetComponent<Button> ();

		player.onClick.AddListener (delegate { // pick to enter as player
			Destroy(panel);
			CmdSwitchPrefab(0);
		});

		spectator.onClick.AddListener (delegate { // pick to enter as spectator
			Destroy(panel);
			CmdSwitchPrefab(1);
		});
	}

	[Command]
	private void CmdSwitchPrefab(int i) {
		GameObject player = (GameObject)Instantiate (prefabs[i], transform.position, transform.rotation);
		NetworkServer.Spawn (player);
		NetworkServer.ReplacePlayerForConnection (this.GetComponent<NetworkIdentity> ().connectionToClient, player, this.GetComponent<NetworkIdentity> ().playerControllerId);
		Destroy (this.gameObject);
	}
}
