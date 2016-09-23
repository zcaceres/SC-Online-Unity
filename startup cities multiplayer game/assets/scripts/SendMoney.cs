using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class SendMoney : MonoBehaviour {

	private Button send;
	private Button cancel;
	private InputField input;
	private Player localPlayer;
	private GameObject target;

	public void setTarget(GameObject g) {
		send = transform.Find ("Send").GetComponent<Button> ();
		cancel = transform.Find ("Cancel").GetComponent<Button> ();
		input = transform.Find ("Input").GetComponent<InputField> ();
		localPlayer = FindObjectOfType<Player> ().localPlayer;
		cancel.onClick.AddListener (delegate {
			selfDestruct();
		});
		target = g;
		string name = "";
		Player tmp = g.GetComponent<Player> ();

		if (tmp != null) {
			name = tmp.playerName;
		}

		transform.Find ("Message").GetComponent<Text> ().text = "Enter the amount of money you would like to send to " + name + ".";
		send.onClick.AddListener (delegate {
			localPlayer.CmdGiveMoney(localPlayer.netId, target.GetComponent<NetworkIdentity>().netId, int.Parse(input.text));
			selfDestruct();
		});
	}

	public void selfDestruct() {
		localPlayer.closePanel ();
		Destroy (this.gameObject);
	}
}
