using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class ButtonSetter : MonoBehaviour {

	Button buyButton;
	Button sellButton;
	Button repairButton;
	Button priceIncreaseButton;
	Button priceDecreaseButton;
	Button neighborhood;
	InputField priceInput;

	// Use this for initialization
	void Start () {
		GameObject tmp = GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").transform.Find ("RentSet").gameObject;
		priceInput = GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").transform.Find ("RentSet").transform.Find ("PriceInput").GetComponent<InputField> ();
		neighborhood = GameObject.Find ("Canvas/ReadoutPanel/Neighborhood").GetComponent<Button> ();
		priceIncreaseButton = tmp.transform.Find ("IncreasePrice").GetComponent<Button> ();
		priceDecreaseButton = tmp.transform.Find ("DecreasePrice").GetComponent<Button> ();
		//Player p = FindObjectOfType<Player> ().localPlayer;
		//setButtons (p);
	}

	public void setButtons(Player p) {
		buyButton = GameObject.Find ("Canvas").transform.Find("ReadoutPanel/Buy").gameObject.GetComponent<Button> ();
		sellButton = GameObject.Find ("Canvas").transform.Find("ReadoutPanel/Sell").gameObject.GetComponent<Button> ();
		repairButton = GameObject.Find ("Canvas").transform.Find("ReadoutPanel/Repair").gameObject.GetComponent<Button> ();
		neighborhood = GameObject.Find ("Canvas").transform.Find("ReadoutPanel/Neighborhood").GetComponent<Button> ();
		buyButton.onClick.AddListener (delegate {
			p.buy();
		});
		sellButton.onClick.AddListener (delegate {
			p.sellChoice();
		});
		neighborhood.onClick.AddListener (delegate {
			p.targetNeighborhood();
		});
	}

	public void setRepair(NetworkInstanceId pid, NetworkInstanceId buildingId) {
		GameObject p;

		p = NetworkServer.FindLocalObject (pid);
		if (p == null) {
			p = ClientScene.FindLocalObject (pid);
		}

		Player player = p.GetComponent<Player> ();

		if (repairButton != null) {
			repairButton.onClick.RemoveAllListeners ();
			repairButton.onClick.AddListener (delegate {
				player.CmdRepair (pid, buildingId);
			});
		}
	}

	public void setRentPrice(NetworkInstanceId pid, NetworkInstanceId buildingId, int rent) {
		GameObject p;

		p = NetworkServer.FindLocalObject (pid);
		if (p == null) {
			p = ClientScene.FindLocalObject (pid);
		}

		Player player = p.GetComponent<Player> ();

		if (priceInput != null) {
			priceInput.text = rent.ToString ();
			priceInput.onValueChanged.RemoveAllListeners ();
			priceInput.onValueChanged.AddListener (delegate {
				player.getRentComparison(buildingId, priceInput.text);
			});
			player.getRentComparison (buildingId, priceInput.text);

			priceInput.onEndEdit.RemoveAllListeners ();
			priceInput.onEndEdit.AddListener (delegate {
				player.CmdSetRent(priceInput.text, buildingId);
			});
				
		}

		if (priceIncreaseButton != null) {
			priceIncreaseButton.onClick.RemoveAllListeners ();
			priceIncreaseButton.onClick.AddListener (delegate {
				if (string.IsNullOrEmpty(priceInput.text)) {
					priceInput.text = "1";
				} else {
					int tmp = int.Parse(priceInput.text);
					tmp++;
					priceInput.text = tmp.ToString();
				}
			});
		}

		if (priceDecreaseButton != null) {
			priceDecreaseButton.onClick.RemoveAllListeners ();
			priceDecreaseButton.onClick.AddListener (delegate {
				if (string.IsNullOrEmpty(priceInput.text)) {
					priceInput.text = "0";
				} else {
					int tmp = int.Parse(priceInput.text);
					tmp--;
					priceInput.text = tmp.ToString();
				}
			});
		}
	}

//	public void setNeighborhood(Neighborhood n, Player p) {
//		neighborhood.onClick.RemoveAllListeners ();
//		Text t = neighborhood.transform.Find ("Text").GetComponent<Text>();
//		if (p.targetBuilding is Neighborhood) {
//			t.text = p.targetBuilding.buildingName;
//		} else {
//			t.text = "Part of " + n.buildingName;
//			neighborhood.onClick.AddListener (delegate {
//				p.targetBuilding = n;
//				t.text = n.buildingName;
//				p.updateUI ();
//			});
//		}
//	}
//
//	public void unsetNeighborhood() {
//		if (neighborhood != null) {
//			Text t = neighborhood.transform.Find ("Text").GetComponent<Text> ();
//			t.text = "No neighborhood";
//			neighborhood.onClick.RemoveAllListeners ();
//		}
//	}
}
