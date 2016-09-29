using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DemographicsPanelButton : MonoBehaviour {

	private GameObject panelPrefab;
	private GameObject panel;
	private Text body;

	// Use this for initialization
	void Start () {
		panelPrefab = (GameObject)Resources.Load ("DemographicsPanel");
		gameObject.GetComponent<Button> ().onClick.AddListener (delegate {
			if (panel == null) {
				spawnPanel();
			} else {
				Destroy(panel);
			}
		});
	}

	private void spawnPanel() {
		Button close;
		int residents = 0;
		int lowlives = 0;
		int lowjob = 0;
		int medjob = 0;
		int highjob = 0;
		int lowhome = 0;
		int medhome = 0;
		int highhome = 0;

		panel = (GameObject)Instantiate (panelPrefab);
		body = panel.transform.Find ("Body").GetComponent<Text> ();
		close = panel.transform.Find ("CloseButton").GetComponent<Button> ();

		close.onClick.AddListener (delegate {
			Destroy(panel);
		});

		panel.transform.SetParent (GameObject.Find ("Canvas").transform, false);

		Resident[] allResidents = FindObjectsOfType<Resident>();
		foreach (Resident r in allResidents) {
			residents++;
			if (r.lowlife) {
				lowlives++;
			}
			if (r.skill == 0) {
				if (r.isHomeless ()) {
					lowhome++;
				}
				if (!r.employed ()) {
					lowjob++;
				}
			} else 	if (r.skill == 1) {
				if (r.isHomeless ()) {
					medhome++;
				}
				if (!r.employed ()) {
					medjob++;
				}
			} else if (r.skill == 2) {
				if (r.isHomeless ()) {
					highhome++;
				}
				if (!r.employed ()) {
					highjob++;
				}
			}
		}

		body.text = residents + "\n\n" + lowlives + "\n\n" + lowjob + "\n\n" + medjob + "\n\n" + highjob +
		"\n\n" + lowhome + "\n\n" + medhome + "\n\n" + highhome;
	}
}
