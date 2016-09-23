using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// Month manager applies events/modifiers on a monthly basis and triggers the player's
/// revenue collection. It also handles any time-related actions such as the monthly countdown
/// ui element.
/// </summary>
public class MonthManager : NetworkBehaviour {

	// Month length in seconds
	const int MONTH_LENGTH = 25;
	const int TURNS_UNTIL_NIGHT = 6;
	//access for SetSunLight and AutoIntensity class other classes to MONTH_LENGTH
	public float turnTime;
	//access for other classes to TURNS_UNTIL_NIGHT
	public float dayCycleLength;

	public bool colorsOn;

	[SyncVar]
	private int monthsPassed;
	private CanvasManager ui;

	[SyncVar]
	private float time;

	private Player[] players;
	private Building[] buildings;
	private ResidentManager rm;
	public Dictionary<int, int> dictRent = new Dictionary<int, int> ();
	public Dictionary<int, int> dictNumberOfType = new Dictionary<int, int> ();
	public Dictionary<int, int> dictCondition = new Dictionary<int, int> ();
	public Dictionary<int, int> dictSafety = new Dictionary<int, int> ();

	[SyncVar(hook="SendWeather")]
	public int weatherType;
	public SimpleWeatherManager weatherManager;
	public bool isDay;
	private SunController sunController;
	private GameObject sun;

	// Use this for initialization
	void Start () {
		colorsOn = false;
		ui = GameObject.Find ("Canvas").GetComponent<CanvasManager> ();
		buildings = GameObject.FindObjectsOfType<Building> ();
		rm = GameObject.FindObjectOfType<ResidentManager> ();
		isDay = true;

		//Sets values to control the sun
		turnTime = MONTH_LENGTH;
		dayCycleLength = TURNS_UNTIL_NIGHT;
		if (isServer) {
			sunController = GameObject.FindObjectOfType<SunController> ();
			sunController.dayRotateSpeed.x = (-(360 / (turnTime * dayCycleLength))) / 2;
			sunController.nightRotateSpeed.x = (-(360 / (turnTime * dayCycleLength))) / 2;
			players = GameObject.FindObjectsOfType<Player> ();
			time = 0;
			monthsPassed = 1;
			foreach (Building building in buildings) {
				//Gets rents of all buildings of a type
				if (dictRent.ContainsKey (building.type)) {
					dictRent [building.type] += building.rent;
				} else { 
					dictRent.Add (building.type, building.rent);
				}

				//Gets condition for all buildings of a type
				if (dictCondition.ContainsKey (building.type)) {
					dictCondition [building.type] += building.condition;
				} else { 
					dictCondition.Add (building.type, building.condition);
				}

				//Gets safety for all buildings of a type
				if (dictSafety.ContainsKey (building.type)) {
					dictSafety[building.type] += building.safety;
				} else { 
					dictSafety.Add (building.type, building.safety);
				}

				//Gets a dictionary of number of buildings of each type
				if (dictNumberOfType.ContainsKey (building.type)) {
					dictNumberOfType [building.type] += 1;
				} else {
					dictNumberOfType.Add (building.type, 1);
				}
			}
		}
	}

	// Update is called once per frame
	void Update () {
		if (isServer) {
			time += Time.deltaTime;
		}
		if ((((int)time % MONTH_LENGTH) == 0)
		    && (int)time > 0) {
			advanceMonth ();
		} else {
			string s = "Seconds until next turn: " + (MONTH_LENGTH - Mathf.Floor (time));
			ui.updateCounter (s);
			string title = "Turn " + monthsPassed;
			ui.updateTitle (title);
		}
	}
		


	[ClientRpc]
	public void RpcUpdateColors () {
		Building[] buildings = FindObjectsOfType<Building> ();
		if (colorsOn) {
			foreach (Building b in buildings) {
				if (b.getOwner () != -1) {
					b.setColor (b.getPlayerOwner().color);
				}
			}
		} else {
			foreach (Building b in buildings) {
				b.resetColor ();
			}
		}
	}

	/// <summary>
	/// Turns the color overlay on or off
	/// </summary>
	public void toggleColors () {
		Building[] buildings = FindObjectsOfType<Building> ();
		Lot[] lots = FindObjectsOfType<Lot> ();
		colorsOn = !colorsOn;
		if (colorsOn) {
			foreach (Building b in buildings) {
				if (b.getOwner () != -1) {
					b.setColor (b.getPlayerOwner().color);
				}
			}
//			foreach (Lot l in lots) {
//				l.showLotSize (true);
//			}
		} else {
			foreach (Building b in buildings) {
				b.resetColor ();
			}
//			foreach (Lot l in lots) {
//				l.showLotSize (false);
//			}
		}
	}

	public int getMonth () {
		return monthsPassed;
	}

	public int getWeather () {
		return weatherType;
	}

	public int getSecond() {
		return (int)time;
	}

	/// <summary>
	/// Advances the month and applies any necessary changes to the buildings/players.
	/// </summary>
	private void advanceMonth () {
		ui.updateCityStatus ("");
		players = GameObject.FindObjectsOfType<Player> ();
		buildings = GameObject.FindObjectsOfType<Building> ().Where (b => (!(b is Neighborhood))).ToArray();
		//DictAttractiveness
		if (isServer) {
			dictRent.Clear ();
			dictSafety.Clear();
			dictNumberOfType.Clear();
			dictCondition.Clear();
			time = 0;
			monthsPassed++;
			SetWeatherAndTime (monthsPassed);
			Neighborhood[] neighborhoods = FindObjectsOfType<Neighborhood> ();
			foreach (Neighborhood n in neighborhoods) {
				n.advanceMonth ();
			}
			foreach (Building building in buildings) {
				if (!building.ruin) {
					//Gets rents of all buildings of a type
					if (dictRent.ContainsKey (building.type)) {
						dictRent [building.type] += building.rent;
					} else { 
						dictRent.Add (building.type, building.rent);
					}

					//Gets condition for all buildings of a type
					if (dictCondition.ContainsKey (building.type)) {
						dictCondition [building.type] += building.condition;
					} else { 
						dictCondition.Add (building.type, building.condition);
					}

					//Gets safety for all buildings of a type
					if (dictSafety.ContainsKey (building.type)) {
						dictSafety [building.type] += building.safety;
					} else { 
						dictSafety.Add (building.type, building.safety);
					}

					//Gets a dictionary of number of buildings of each type
					if (dictNumberOfType.ContainsKey (building.type)) {
						dictNumberOfType [building.type] += 1;
					} else {
						dictNumberOfType.Add (building.type, 1);
					}
				}
				building.tenant.availableTenants.Clear ();
				building.advanceMonth ();
			}

			foreach (Player p in players) {
				p.advanceMonth ();
			}
			if (Random.Range(0f, 10f) <= .01) { 
				Earthquake (buildings);
			}
			rm.advanceMonth (); // RESIDENTS
		}
	}

	public void Earthquake (Building[] buildings)
	{
		GameObject explosionObj = (GameObject)Resources.Load ("Explosion");
		foreach (Player p in players) {
			RpcEarthQuake ();
		}
		foreach (Building b in buildings) {
			if (!(b is Lot)) {
				float delay = Random.Range (5f, 30f);
				float probability = Random.Range (0f, 10f);
				b.earthQuakeDamage (probability, explosionObj, delay);
			}
		}
	}

	[ClientRpc]
	private void RpcEarthQuake () {
		Player p = GameObject.FindObjectOfType<Player> ().localPlayer;
		AudioClip quakeLoop = (AudioClip) Resources.Load ("Sounds/Earthquake/earthQuakeSound");
		GameObject cs = p.gameObject.GetComponentInChildren<EZCameraShake.CameraShaker> ().gameObject;
		p.message = "Uh oh...";
		AudioSource pa = p.GetComponentInChildren<AudioSource> ();
		pa.clip = quakeLoop;
		pa.Play ();
		cs.GetComponent<EZCameraShake.CameraShaker> ().ShakeOnce (10f, 10f, 15f, 15f);
	}


	/// <summary>
	/// Gets the average rent for a building type and returns it. Used in ConstructionController for
	/// market data about new construction.
	/// </summary>
	/// <returns>The average rent.</returns>
	/// <param name="buildingType">Building type.</param>
	public int GetAverageRent (int buildingType) {
		int marketRent = (dictRent[buildingType] / dictNumberOfType[buildingType]);
		return marketRent;
	}


	/// <summary>
	/// Sets the leaders on the leaderboard by NetWorth.
	/// </summary>
	/// <returns>The leaders.</returns>
	public string SetLeaders() {
		players = GameObject.FindObjectsOfType<Player> ();
		string s = "";
		foreach (Player p in players) {
			int networth = p.netWorth();
			string name = p.playerName;
			s += name + ": $" + networth.ToString();
			s += "\n";
		}
		return s;
	}


	/// <summary>
	/// Sets the weather and time.
	/// </summary>
	/// <param name="monthsPassed">Months passed.</param>
	private void SetWeatherAndTime (int monthsPassed) {
		if (monthsPassed % TURNS_UNTIL_NIGHT == 0  && !isDay) {
			weatherType = Random.Range(0,2);
			isDay = true;
			if (isServer) {
				rm.killCrime (); //Spawns criminals
				rm.setResidentCommute(isDay); //This triggers the go-to-work or go-home routine in resident's aiMovement script
			}
		}
		else if (monthsPassed % TURNS_UNTIL_NIGHT == 0 && isDay) {
			weatherType = Random.Range(2,4);
			isDay = false;
			if (isServer) {
				rm.spawnCrime (); //Spawns criminals
				rm.setResidentCommute(isDay); //This triggers the go-to-work or go-home routine in resident's aiMovement script
			}
		}
	}

	public void SendWeather (int w) {
		weatherType = w;
		weatherManager.TimeAndWeather (weatherType);
	}



}