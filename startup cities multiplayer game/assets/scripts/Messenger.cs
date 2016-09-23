using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Messenger : NetworkBehaviour {
	
	private CanvasManager ui;
	[SyncVar]
	string message;

	void Start() {
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		if (isServer) {
			message = "Test";
		}
	}

	void Update() {
		ui.updateCityStatus (message);
	}

	[Command]
	public void CmdMessage() {
		InputField tmp = GameObject.Find ("TestField").GetComponent<InputField>();
		Debug.Log ("Server Reached: sent " + tmp.text);
		message += "\n" + tmp.text;
	}
}