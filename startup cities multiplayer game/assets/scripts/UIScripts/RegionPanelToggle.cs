using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RegionPanelToggle : MonoBehaviour {

	GameObject regionPanel;
	GameObject regionButton;
	GameObject panel;
	Image selected;

	void Start() {
		regionPanel = (GameObject)Resources.Load ("uiElements/RegionPanel");
		regionButton = (GameObject)Resources.Load ("uiElements/RegionButton");
		gameObject.GetComponent<Button> ().onClick.AddListener (delegate {
			TogglePanel();
		});
	}

	public void TogglePanel() {
		if (panel == null) {
			int ypos = 0;
			panel = (GameObject)Instantiate (regionPanel);
			panel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			Region[] regions = FindObjectsOfType<Region> ();
			Player localPlayer = FindObjectOfType<Player> ().localPlayer;

			foreach (Region r in regions) {
				GameObject tmp = (GameObject)Instantiate (regionButton);
				tmp.transform.SetParent (panel.transform, false);
				tmp.transform.position = new Vector3 (tmp.transform.position.x, tmp.transform.position.y + ypos, tmp.transform.position.z);
				ypos -= 30;
				tmp.transform.Find ("Text").GetComponent<Text> ().text = r.regionName;
				if (localPlayer != null && r.cityHall.ownedBy (localPlayer)) {
					Button tmpButton = tmp.transform.Find ("Activate").GetComponent<Button> ();
					if (r.cityHall.id == localPlayer.activeCityHall.id) {
						selected = tmpButton.GetComponent<Image> ();
						selected.color = Color.red;
					}

					tmpButton.onClick.AddListener (delegate {
						if (r.cityHall.id == localPlayer.activeCityHall.id) {
							
						} else {
							if (selected != null) {
								selected.color = Color.black;
							}
							localPlayer.activeCityHall = r.cityHall;
							localPlayer.showMessage(r.regionName + " is now your active region.");
							selected = tmpButton.GetComponent<Image>();
							selected.color = Color.red;
						}
					});
				} else {
					tmp.transform.Find ("Activate").gameObject.SetActive (false);
				}
			}
		} else {
			Destroy (panel);
		}
	}
}
