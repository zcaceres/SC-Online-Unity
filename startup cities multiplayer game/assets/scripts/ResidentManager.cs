using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ResidentManager : NetworkBehaviour
{
	const int SPAWN_INTERVAL = 1;

	private List<Resident> residents;
	private Object residentPrefab;
	private List<Transform> spawnPoints;
	private GameObject[] bus;
	private Object criminalPrefab;
	private int lowSkillJobs;
	private int medSkillJobs;
	private int highSkillJobs;
	private float time;
	private bool noJobs;
	private bool noHomes;
	public bool isElectionSeason;
	public GameObject[] loiterPoints;

	void Start () {
		spawnPoints = new List<Transform> ();
		lowSkillJobs = 0;
		medSkillJobs = 0;
		highSkillJobs = 0;
		residentPrefab = Resources.Load ("Resident");
		criminalPrefab = Resources.Load ("Criminal");
		loiterPoints = GameObject.FindGameObjectsWithTag ("loiterWayPoint");
		bus = GameObject.FindGameObjectsWithTag ("Bus");
		foreach (GameObject go in loiterPoints) {
			spawnPoints.Add (go.transform);
		}
		if (isServer) {
			residents = new List<Resident> ();
		}
	}

	void Update() {
		if (isServer) {
			time += Time.deltaTime;
			if ((((int)time % SPAWN_INTERVAL) == 0) && (int)time > 0) {
				if (highSkillJobs > 0) {
					spawnResident (2, false);
					time = 0;
					highSkillJobs--;
				} else if (medSkillJobs > 0) {
					spawnResident (1, false);
					time = 0;
					medSkillJobs--;
				} else if (lowSkillJobs > 0) {
					spawnResident (0, false);
					time = 0;
					lowSkillJobs--;
				} 
			}
		}
	}

	public void advanceMonth ()
	{
		if (isServer) {
			Business[] businesses = FindObjectsOfType<Business> ();
			lowSkillJobs = 0;
			medSkillJobs = 0;
			highSkillJobs = 0;
			foreach (Business b in businesses) {
				if ((b.validOwner() || (b is CityHall)) && !b.ruin) {
					if (b.occupied) {
						if (b.skillLevel == 0) {
							lowSkillJobs += (b.neededWorkers - b.workers.Count);
						} else if (b.skillLevel == 1) {
							medSkillJobs += (b.neededWorkers - b.workers.Count);
						} else if (b.skillLevel == 2) {
							highSkillJobs += (b.neededWorkers - b.workers.Count);
						}
					} else if (b.tenant.availableTenants.Count < 3) {
						if (b.skillLevel == 0) {
							lowSkillJobs += 1;
						} else if (b.skillLevel == 1) {
							medSkillJobs += 1;
						} else if (b.skillLevel == 2) {
							highSkillJobs += 1;
						}
					}
				}
			}
			List<Resident> toRemove = new List<Resident> ();
			bool doOnce = false;
			foreach (Resident r in residents) {
				if (r != null) {
					if (!doOnce) {
						doOnce = true;
						r.sortBuildings (); // sort the buildings once a month
					}
					r.advanceMonth ();

				} else {
					toRemove.Add (r);
				}
			}
			foreach (Resident r in toRemove) {
				residents.Remove (r);
			}

			if (noJobs) {
				messageAll ("Residents are leaving the city since they can't find jobs.");
				noJobs = false;
			}
			if (noHomes) {
				messageAll ("Residents are leaving the city since they can't find housing.");
				noHomes = false;
			}
		}
	}

	/// <summary>
	/// Spawns the resident.
	/// </summary>
	/// <param name="skill">Skill level of the resident</param>
	public void spawnResident (int skill, bool lowlife)
	{
		GameObject spawnPoint = bus [Random.Range (0, bus.Length - 1)];
		GameObject newResident = (GameObject)Instantiate (residentPrefab, spawnPoint.transform.position, Quaternion.identity);
		//newResident.transform.SetParent (bus, false);
		Resident tmp = newResident.GetComponent<Resident> ();
		tmp.skill = skill;
		tmp.lowlife = lowlife;
		NetworkServer.Spawn (newResident);
		residents.Add (tmp);
	}

	public void SpawnPolitician(int p, Region r) {
		GameObject spawnPoint = bus [Random.Range (0, bus.Length - 1)];
		GameObject newResident = (GameObject)Instantiate (Resources.Load("Politician"), spawnPoint.transform.position, Quaternion.identity);
		Politician tmp = newResident.GetComponent<Politician> ();
		tmp.party = p;
		tmp.runningFor = r;
		NetworkServer.Spawn (newResident);
		residents.Add (tmp);
	}

	/// <summary>
	/// If it's night, spawns the crime event from the Month Manager.
	/// Spawns a criminal which then heads to burglarize a property on the map.
	/// </summary>
	public void spawnCrime ()
	{
		float crimeThreshold = 9.8f; //How likely is it that criminal is spawned?
		GameObject[] criminalSpawnPoints = GameObject.FindGameObjectsWithTag ("wayPoint");
		foreach (GameObject go in criminalSpawnPoints) {
			if (go.GetComponentInParent<Building> () != null) { //avoids null refs in building check
				if (!go.GetComponentInParent<Building> ().occupied) { //checks if a building is occupied
					if (Random.Range (0, 10f) >= crimeThreshold) {    //Probability that an abandoned property will spawn a criminal (higher crime threshold is lower probability of crime)
						GameObject criminal = (GameObject)Instantiate (criminalPrefab);
						criminal.transform.SetParent (go.transform, false);
						NetworkServer.Spawn (criminal);
					}
				}
			}
		}
	}


	/// <summary>
	/// Sets the resident's commute.
	/// </summary>
	public void setResidentCommute (bool isDay) {
		GameObject[] loiterPoints = GameObject.FindGameObjectsWithTag ("loiterWayPoint");
		foreach (Resident r in residents) {
			if (r != null) {
				r.GetComponent<ResidentAI> ().GetResidentPoints (isDay);
			}
		}
	}

	/// <summary>
	/// If it's day, despawns all criminals. Called from MonthManager.
	/// </summary>
	public void killCrime() {
		Criminal[] criminals = GameObject.FindObjectsOfType<Criminal> ();
		foreach (Criminal c in criminals) {
			Destroy (c.gameObject);
		}
	}

	public void flagLeavingHomeless() {
		noHomes = true;
	}

	public void flagLeavingJobless() {
		noJobs = true;
	}

	public void StartElectionSeason() {
		Region[] regions = FindObjectsOfType<Region> ();
		foreach (Region r in regions) { // spawn 3 candidates, one for each party, in every region
			SpawnPolitician (1, r);
			SpawnPolitician (2, r);
			SpawnPolitician (3, r);
		}
	}
	private void messageAll(string s) {
		Player player = FindObjectOfType<Player>();
		if (player != null) {
			player.RpcMessageAll (s);
		}
	}
}
