
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ElectionManager : NetworkBehaviour {

	Player p;
	GameObject panel;
	static MonthManager mm;
	// Use this for initialization
	void Start () {
		p = GetComponent<Player> ();
		mm = FindObjectOfType<MonthManager> ();

	}
	
	// Update is called once per frame
	void Update () {
		if (!isLocalPlayer)
			return;

		if (panel != null) {
			if (Input.GetKeyDown (KeyCode.C) || !mm.isElectionSeason) {
				Destroy (panel);
			} else if (p.targetObject == null && !(p.targetObject is CityHall)) {
				Destroy (panel);
			} else if (Input.GetKeyDown (KeyCode.Alpha1)) {
				if (p.targetObject != null && (p.targetObject is CityHall)) {
					CmdDonateToCandidate (this.netId, p.targetObject.netId, 0);
				}
			} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
				if (p.targetObject != null && (p.targetObject is CityHall)) {
					CmdDonateToCandidate (this.netId, p.targetObject.netId, 1);
				}
			} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
				if (p.targetObject != null && (p.targetObject is CityHall)) {
					CmdDonateToCandidate (this.netId, p.targetObject.netId, 2);
				}
			}
		} else {
			if (Input.GetKeyDown (KeyCode.C) && mm.isElectionSeason && p.targetObject != null && (p.targetObject is CityHall)) {
				SpawnPanel ();
			}
		}
	}

	private void SpawnPanel() {
		Region r = p.targetObject.GetComponent<CityHall> ().governedRegion;
		Politician teal = r.GetCandidateAt (0);
		Politician grey = r.GetCandidateAt (1);
		Politician yellow = r.GetCandidateAt (2);

		GameObject prefab = (GameObject)Resources.Load ("uiElements/ElectionPanel");
		panel = (GameObject)Instantiate (prefab);
		panel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
		panel.transform.Find ("TealImg").transform.Find ("Body").GetComponent<Text> ().text = teal.ElectionFormatString ();
		panel.transform.Find ("GreyImg").transform.Find ("Body").GetComponent<Text> ().text = grey.ElectionFormatString ();
		panel.transform.Find ("YellowImg").transform.Find ("Body").GetComponent<Text> ().text = yellow.ElectionFormatString ();
	}

	[Command]
	private void CmdDonateToCandidate(NetworkInstanceId pid, NetworkInstanceId cid, int index) {
		Player p;
		CityHall c;

		if (isClient) {
			p = ClientScene.FindLocalObject (pid).GetComponent<Player>();
			c = ClientScene.FindLocalObject (cid).GetComponent<CityHall>();
		} else {
			p = NetworkServer.FindLocalObject (pid).GetComponent<Player>(); 
			c = NetworkServer.FindLocalObject (pid).GetComponent<CityHall>(); 
		}

		if (c.governedRegion.candidates.Count == 3) {
			Politician mayor = c.governedRegion.GetCandidateAt (index);
			mayor.AddFunds (1000, p);
			p.RpcMessage ("Donated $1000 to " + mayor.residentName + "'s mayoral campaign!");
		}
	}
}
