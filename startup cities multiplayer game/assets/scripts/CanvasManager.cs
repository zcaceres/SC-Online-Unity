using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;


/// <summary>
/// Manager used for hiding ui elements and updating text fields
/// </summary>
public class CanvasManager : MonoBehaviour {
	const int LEDGER_BUTTON_HEIGHT = 25;
	private List<GameObject> readoutButtons = new List<GameObject>();

	private GameObject ledgerPanel;
	private GameObject canvas;
	private GameObject readoutPanel;
	private GameObject bottomPanel;
	private GameObject playerReadoutPanel;
	private GameObject setPricePanel;

	// These are all the text fields which the canvas need to manage
	private Text budget;
	private Text revenue;
	private Text title;
	private Text ledgerText;
	private Text buildingReadout;
	private Text playerReadout;
	private Text counter;
	private Text cityUpdates;
	private Text experience;
	private Text leaderboard;
	private Text workers;
	private Text marketPrice;
	private Text neighborhood;
	private Transform ledgerContentTransform;
	private MonthManager monthManager;
	private List<GameObject> ledgerButtons;

	bool ledgerShowing;
	// Use this for initialization
	void Start () {
		canvas = GameObject.Find ("Canvas");
		bottomPanel = canvas.transform.Find ("BottomBar").gameObject;
		ledgerButtons = new List<GameObject> ();
		budget = bottomPanel.transform.Find("Budget").GetComponent<Text>();
		revenue = bottomPanel.transform.Find("Revenue").GetComponent<Text>();
		title = bottomPanel.transform.Find("Title").GetComponent<Text>();
		counter = bottomPanel.transform.Find ("Counter").GetComponent<Text>();
		experience = bottomPanel.transform.Find ("Experience").GetComponent<Text> ();
		cityUpdates = canvas.transform.Find ("CityStatusUpdates").GetComponent<Text> ();

		leaderboard = gameObject.transform.Find ("LeaderBoard").transform.Find("Rank").GetComponent<Text> ();

		ledgerShowing = false;
		ledgerPanel = GameObject.Find ("Ledger");
		ledgerPanel.transform.Find ("Viewport/Content/Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
			ledgerToggle();
		});
		ledgerText = ledgerPanel.transform.Find("Viewport/Content/Text").GetComponent<Text> ();
		//ledgerPanelDimensions = ledgerText.gameObject.transform.parent.gameObject.GetComponent<RectTransform> ();
		ledgerPanel.SetActive (ledgerShowing);
		ledgerContentTransform = ledgerPanel.transform.Find ("Viewport/Content");

		neighborhood = GameObject.Find ("Canvas/ReadoutPanel/Neighborhood/Text").GetComponent<Text> ();
		readoutPanel = GameObject.Find ("ReadoutPanel");
		buildingReadout = readoutPanel.transform.Find("Readout Scroll/Viewport/ReadoutDisplay/BuildingReadout").GetComponent<Text>();
		workers = readoutPanel.transform.Find ("Readout Scroll/Viewport/ReadoutDisplay/Workers").GetComponent<Text>();

		setPricePanel = readoutPanel.transform.Find ("RentSet").gameObject;
		marketPrice = setPricePanel.transform.Find ("MarketRate").gameObject.GetComponent<Text> ();

		playerReadoutPanel = GameObject.Find ("PlayerReadoutPanel");
		playerReadout = playerReadoutPanel.transform.Find("Readout Scroll/Viewport/ReadoutDisplay/PlayerReadout").GetComponent<Text>();

		readoutPanel.SetActive (false);
		playerReadoutPanel.SetActive (false);
		setPricePanel.SetActive (false);
	}
		
	public void ledgerToggle() {
		ledgerShowing = !ledgerShowing;
		ledgerPanel.SetActive(ledgerShowing);

		foreach (GameObject g in ledgerButtons) {
			Destroy (g);
		}
		ledgerButtons.Clear ();

		if (ledgerShowing) {
			float ypos = -25;
			Player p = FindObjectOfType<Player> ().localPlayer;
			if (p == null)
				return;
			List<Building> tmp = p.getBuildings ();
			List<Building> hoods = tmp.Where (b => (b is Neighborhood)).ToList ();
			List<Building> lots = tmp.Where (b => ((b is Lot) && !b.inNeighborhood ())).ToList ();
			hoods.OrderByDescending (b => (b.appraise ()));
			lots.OrderByDescending (b => (b.appraise ()));

			foreach (Building b in hoods) {
				GameObject obj = (GameObject)Instantiate(Resources.Load("NButton"));
				obj.transform.SetParent (ledgerContentTransform, false);
				obj.transform.Find ("Text").GetComponent<Text> ().text = b.buildingName;
				obj.transform.position = new Vector3 (obj.transform.position.x, obj.transform.position.y + ypos, obj.transform.position.z);
				Neighborhood n = b.GetComponent<Neighborhood> ();
				obj.GetComponent<Button> ().onClick.AddListener (delegate {
					spawnNeighborhoodPanel(n);
					ledgerToggle();
				});
				ypos -= LEDGER_BUTTON_HEIGHT;
				ledgerButtons.Add (obj);
			}
			foreach (Building b in lots) {
				GameObject obj = (GameObject)Instantiate(Resources.Load("NButton"));
				obj.transform.SetParent (ledgerContentTransform, false);
				obj.transform.Find ("Text").GetComponent<Text> ().text = b.buildingName;
				obj.transform.position = new Vector3 (obj.transform.position.x, obj.transform.position.y + ypos, obj.transform.position.z);
				Lot l = b.GetComponent<Lot> ();
				obj.GetComponent<Button> ().onClick.AddListener (delegate {
					spawnLotPanel(l);
					ledgerToggle();
				});
				ypos -= LEDGER_BUTTON_HEIGHT;
				ledgerButtons.Add (obj);
			}
		} 
	}

	/// <summary>
	/// Replaces the current text in the budget UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateBudget(string s)
	{
		budget.text = s;
	}

	/// <summary>
	/// Replaces the current text in the revenue UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateRevenue(string s)
	{
		revenue.text = s;
	}

	/// <summary>
	/// Replaces the current text in the building readout UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateReadout(string s)
	{
		buildingReadout.text = s;
	}
		

	/// <summary>
	/// Replaces the current text in the title UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateTitle(string s)
	{
		title.text = s;
	}

	/// <summary>
	/// Replaces the current text in the ledger text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateLedger(string s)
	{
		ledgerText.text = s;
	}


	public void updateCounter(string s)
	{
		if ((s != null) && (counter != null)) { 
			counter.text = s;
		}
	}

	/// <summary>
	/// Replaces currnet text on bottom bar field with passed string
	/// </summary>
	/// <param name="s">S.</param>
	public void updateExperience(string s)
	{
		experience.text = s;
	}

	/// <summary>
	/// Replaces the current text in the city status UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updateCityStatus(string s)
	{
		cityUpdates.text = s;
	}



	public void updateLeaderBoard(string s)
	{
		leaderboard.text = s;
	}


	/// <summary>
	/// Concatenates the passed string to the current text in the city status text field
	/// rather than replacing it entirely
	/// </summary>
	/// <param name="s">The text to be added to the present text</param>
	public void concatCityStatus(string s)
	{
		cityUpdates.text += "\n" + s;
	}

	public void updateWorkers(string s) {
		workers.text = s;
	}
	/// <summary>
	/// Toggles the building readout panel
	/// </summary>
	/// <param name="show">If set to <c>true</c>, show the panel. If set to <c>false</c>, hide it</param>
	public void readoutToggle(bool show)
	{
		if (!show) {
			clearButtons ();
		}
		readoutPanel.SetActive (show);
	}
		
	/// <summary>
	/// Gets unique readout for player
	/// </summary>
	/// <param name="show">If set to <c>true</c> show.</param>
	public void playerReadoutToggle (bool show)
	{
		if (!show) {
			clearButtons ();
		}
		playerReadoutPanel.SetActive (show);

	}
		
	/// <summary>
	/// Displays window for setting the price of something
	/// </summary>
	/// <param name="show">If set to <c>true</c> show.</param>
	public void setPriceToggle (bool show)
	{
		if (!show) {
			clearButtons ();
		}
		setPricePanel.SetActive (show);
	}
		
	/// <summary>
	/// Replaces price displayed on readout with player's input
	/// </summary>
	/// <param name="s">S.</param>
	public void updatePriceReadout (int rateType, int s, int marketRate)
	{
		if (rateType == 0) {
			marketPrice.text = s.ToString () + " below market rate (" + marketRate.ToString () + ")";
		} else if (rateType == 1) {
			marketPrice.text = s.ToString() + " above market rate. (" + marketRate.ToString () + ")";
		}
	}

	/// <summary>
	/// Overloaded function to displayed correct note on readout when no market
	/// data exists!
	/// </summary>
	/// <param name="s">S.</param>
	public void updatePriceReadout (string s)
	{
		marketPrice.text = s;
	}

	public void updateNeighborhoodName(string s) {
		neighborhood.text = s;
	}

	/// <summary>
	/// Replaces the current text in the player readout UI text field with the passed string
	/// </summary>
	/// <param name="s">The new text</param>
	public void updatePlayerReadout(string s)
	{
		playerReadout.text = s;
	}

	public void addButton(GameObject button) {
		readoutButtons.Add (button);
	}

	public void clearButtons() 
	{
		foreach (GameObject button in readoutButtons) {
			if (button != null) {
				ModIcon tmpMod = button.GetComponent<ModIcon> ();
				TenantPanel tmpTenant = button.GetComponent<TenantPanel> ();

				if (tmpMod != null) {
					tmpMod.buttonDestroy ();
				} else if (tmpTenant != null) {
					tmpTenant.buttonDestroy ();
				} else {
					Destroy (button);
				}
			}
		}
		readoutButtons.Clear ();
	}

	private void spawnNeighborhoodPanel(Neighborhood n) {
		int ypos = -105;
		GameObject obj = (GameObject)Instantiate(Resources.Load("LedgerBuildingDetails"));
		obj.transform.SetParent (canvas.transform, false);
		obj.transform.Find ("Viewport/Content/Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
			Destroy(obj);
		});
		Button back = obj.transform.Find ("Viewport/Content/Back").GetComponent<Button> ();
		back.onClick.RemoveAllListeners ();
		back.onClick.AddListener (delegate {
			ledgerToggle();
			Destroy(obj);
		});
		obj.transform.Find ("Viewport/Content/Name").GetComponent<Text> ().text = n.buildingName;
		string body = "Revenue: $" + n.calcRents () + "\nUpkeep: $" + n.calcUpkeep() + "\nNumber of Lots: " + n.numLots ();
		if (n.isManaged ()) {
			body += "\nManager Cost: $" + n.getManagerSalary ();
		} else {
			body += "\nNo Manager";
		}
		obj.transform.Find ("Viewport/Content/Body").GetComponent<Text> ().text = body;
		List<Lot> tmp = n.getLots ();
		Transform p = obj.transform.Find ("Viewport/Content");
		foreach (Lot l in tmp) {
			GameObject lotObj = (GameObject)Instantiate(Resources.Load("NButton"));
			lotObj.transform.Find ("Text").GetComponent<Text> ().text = l.buildingName;
			lotObj.transform.SetParent (p, false);
			lotObj.transform.position = new Vector3 (lotObj.transform.position.x, lotObj.transform.position.y + ypos, lotObj.transform.position.z);
			Lot tmpLot = l;
			lotObj.GetComponent<Button> ().onClick.AddListener (delegate {
				spawnLotPanel(tmpLot);
				Destroy(obj);
			});
			ypos -= LEDGER_BUTTON_HEIGHT;
		}
	}

	private void spawnLotPanel(Lot l) {
		int ypos = -105;
		GameObject obj = (GameObject)Instantiate(Resources.Load("LedgerBuildingDetails"));
		obj.transform.SetParent (canvas.transform, false);
		obj.transform.Find ("Viewport/Content/Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
			Destroy(obj);
		});
		if (!l.inNeighborhood ()) {
			obj.transform.Find ("Viewport/Content/Back").GetComponent<Button> ().onClick.AddListener (delegate {
				ledgerToggle ();
				Destroy (obj);
			});
		} else {
			Neighborhood n = l.getNeighborhood ();
			obj.transform.Find ("Viewport/Content/Back").GetComponent<Button> ().onClick.AddListener (delegate {
				spawnNeighborhoodPanel(n);
				Destroy(obj);
			});
		}
		obj.transform.Find ("Viewport/Content/Name").GetComponent<Text> ().text = l.buildingName;
		obj.transform.Find ("Viewport/Content/Body").GetComponent<Text> ().text = "Revenue: $" + l.calcRents() + "\nUpkeep: $" + l.calcUpkeep() +
		"\nAttractiveness: " + l.getAttractiveness ();
		List<Building> tmp = l.getBuildings();
		Transform p = obj.transform.Find ("Viewport/Content");
		foreach (Building b in tmp) {
			GameObject lotObj = (GameObject)Instantiate(Resources.Load("NButton"));
			lotObj.transform.Find ("Text").GetComponent<Text> ().text = b.buildingName;
			lotObj.transform.SetParent (p, false);
			lotObj.transform.position = new Vector3 (lotObj.transform.position.x, lotObj.transform.position.y + ypos, lotObj.transform.position.z);
			Building tmpB = b;
			lotObj.GetComponent<Button> ().onClick.AddListener (delegate {
				spawnBuildingPanel(tmpB);
				Destroy(obj);
			});
			ypos -= LEDGER_BUTTON_HEIGHT;
		}
	}

	private void spawnBuildingPanel(Building b) {
		GameObject obj = (GameObject)Instantiate(Resources.Load("LedgerBuildingDetails"));
		obj.transform.SetParent (canvas.transform, false);
		obj.transform.Find ("Viewport/Content/Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
			Destroy(obj);
		});
		Lot l = b.getLot ();
		obj.transform.Find ("Viewport/Content/Back").GetComponent<Button> ().onClick.AddListener (delegate {
			spawnLotPanel(l);
			Destroy(obj);
		});

		obj.transform.Find ("Viewport/Content/Name").GetComponent<Text> ().text = b.buildingName;
		obj.transform.Find ("Viewport/Content/Body").GetComponent<Text> ().text = "Rent: $" + b.rent + "\nUpkeep: $" + 
			b.upkeep + "\nAttractiveness: " + l.getAttractiveness () +"\nAttractiveness Effect: " + b.getAttractEffect ();
	}
}
