using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class NeighborhoodChoice : MonoBehaviour {
	const int BUTTON_HEIGHT = 25;

	GameObject content;
	GameObject buttonPrefab;
	Player localPlayer;
	Button cancel;
	List<Neighborhood> hoods;
	float ypos;

	// Use this for initialization
	void Start () {
		content = transform.Find ("Viewport/Content").gameObject;
		buttonPrefab = (GameObject)Resources.Load ("NButton");
		localPlayer = FindObjectOfType<Player> ().localPlayer;
		localPlayer.controlsAllowed (false);
		cancel = content.transform.Find ("Cancel").GetComponent<Button> ();
		hoods = localPlayer.getNeighborhoods ();
		ypos = cancel.transform.position.y;

		cancel.onClick.AddListener (delegate {
			localPlayer.controlsAllowed(true);
			Destroy(this.gameObject);
		});

		foreach (Neighborhood n in hoods) {
			ypos -= BUTTON_HEIGHT;
			GameObject tmp = (GameObject)Instantiate(buttonPrefab);
			tmp.transform.SetParent (content.transform, false);
			tmp.transform.position = new Vector3 (cancel.transform.position.x, ypos, cancel.transform.position.z);
			Button tmpButton = tmp.GetComponent<Button> ();
			Text t = tmpButton.transform.Find ("Text").GetComponent<Text> ();
			t.text = n.buildingName;
			Neighborhood tmpN = n;
			tmpButton.onClick.AddListener(delegate{
				localPlayer.CmdAddToNeighborhood(localPlayer.netId, localPlayer.targetBuilding.netId, tmpN.netId);
				localPlayer.controlsAllowed(true);
				Destroy(this.gameObject);
			});
		}
	}
}
