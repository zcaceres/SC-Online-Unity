using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BankruptcyPanel : MonoBehaviour {
	const int TIME_LIMIT = 10;
	const int PREFAB_HEIGHT = 20;

	private Player p;
	private int timer;
	private int second;
	private MonthManager mm;
	private Transform content;
	private Text message;
	private GameObject prefab;
	private List<Building> hoods;
	private List<Building> lots;
	private List<Building> toSell;
	private int ypos;
	private int offered;
	private Button confirm;
	// Use this for initialization
	void Start () {
		p = FindObjectOfType<Player> ().localPlayer;
		p.controlsAllowed (false);
		mm = FindObjectOfType<MonthManager> ();
		offered = 0;
		content = transform.Find ("Viewport/Content");
		confirm = gameObject.transform.Find ("Button").GetComponent<Button> ();
		confirm.onClick.AddListener (delegate {
			submit();
		});
		message = gameObject.transform.Find ("Panel/Text").GetComponent<Text> ();
		toSell = new List<Building> ();
		if (p.budget + offered < 0) {
			message.text = "Select properties to pay off your debts.\nAmount needed: $" + p.budget + "\nAmount offered: $" + offered + "\n";
		} else {
			message.text = "Price met.";
		}
		second = mm.getSecond ();
		timer = 0;
		transform.SetParent (GameObject.Find ("Canvas").transform, false);
		prefab = (GameObject)Resources.Load ("BankruptcyToggle");
		List<Building> tmp = p.getBuildings ();
		hoods = tmp.Where (b => (b is Neighborhood)).ToList();
		lots = tmp.Where (b => ((b is Lot) && !b.inNeighborhood ())).ToList();
		ypos = 0;
		foreach (Building b in hoods) {
			spawnToggle (b);
		}
		foreach (Building b in lots) {
			spawnToggle (b);
		}
		if ((p.propertyValue () + p.budget) < 0) {
			seizeAll ();
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (second != mm.getSecond ()) {
			timer++;
			second = mm.getSecond ();
		}
		if (p.budget + offered < 0) {
			message.text = "Select properties to pay off your debts.\nAmount needed: $" + p.budget + "\nAmount offered: $" + offered + "\n";
		} else {
			message.text = "Price met. \nBudget after this deal: $" + (p.budget + offered);
		}
	}

	/// <summary>
	/// takes buildings automatically--used when player is too slow with their selection.
	/// </summary>
	public void seize() {
		int i = 0;
		toSell.Clear ();
		offered = 0;
		while ((i < hoods.Count) && ((p.budget + offered) < 0)) {
			toSell.Add (hoods [i]);
			offered += hoods[i].appraise ();
			i++;
		}
		Debug.Log (p.budget + offered);
		i = 0;
		while ((i < lots.Count) && ((p.budget + offered) < 0)) {
			toSell.Add (lots [i]);
			offered += lots[i].appraise ();
			i++;
		}
		submit ();
	}
		
	public void seizeAll() {
		toSell.Clear ();
		foreach (Building b in hoods) {
			toSell.Add (b);
			offered += b.appraise ();
		}

		foreach (Building b in lots) {
			toSell.Add (b);
			offered += b.appraise ();
		}
		p.showMessage ("All of your properties have been seized to pay off your debts!");
		submit ();
	}

	private void submit() {
		foreach (Building b in toSell) {
			p.CmdRepo (b.netId);
		}
		p.bankruptChoice = null;
		p.controlsAllowed(true);
		Destroy(this.gameObject);
	}

	private void spawnToggle(Building b) {
		GameObject tmp = (GameObject)Instantiate (prefab);
		Toggle t = tmp.GetComponent<Toggle> ();
		t.onValueChanged.AddListener (delegate {
			if (t.isOn) {
				offered += b.appraise();
				toSell.Add(b);
			} else {
				offered -= b.appraise();
				toSell.Remove(b);
			}
		});
		tmp.transform.SetParent (content, false);
		tmp.transform.position = new Vector3 (tmp.transform.position.x, tmp.transform.position.y + ypos, tmp.transform.position.z);
		tmp.transform.Find ("Label").GetComponent<Text> ().text = b.buildingName + " ($" + b.appraise() + ")";
		ypos -= PREFAB_HEIGHT;
	}
}
