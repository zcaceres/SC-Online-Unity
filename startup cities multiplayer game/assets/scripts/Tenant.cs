using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Tenant : NetworkBehaviour {
	public struct NetId {
		public NetworkInstanceId id;
	}

	public class SyncListNetId : SyncListStruct<NetId> {	
		public void addId(NetworkInstanceId id) {
			NetId w;
			w.id = id;
			this.Add (w);
		}

		public void removeId(NetworkInstanceId id) {
			this.Remove (this.Where(w => (w.id == id)).ToList()[0]);
		}
	}
	const int LEASE_MONTHS = 12;

	protected static System.Random rng = new System.Random ();

	[SyncVar(hook = "setActive")]
	public NetworkInstanceId activeTenant;
	public Resident resident;
	public SyncListNetId availableTenants = new SyncListNetId();

	protected Building building;
	protected BuildingModifier mod;
	protected List<GameObject> buttons = new List<GameObject>();
	[SyncVar]
	protected int months;
	private MonthManager monthManager;

	// Use this for initialization
	void Start () {
		building = GetComponent<Building> ();
		mod = GetComponent<BuildingModifier> ();

		if (isServer) {
			monthManager = GameObject.Find ("Clock").GetComponent<MonthManager> ();
		}

	}

	/// <summary>
	/// Returns the data associated with a person as a string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="p">The person.</param>
	public virtual string personToString() {
		string s = resident.personToString();
		return s;
	}

	public virtual string showTenants() {
		string s = "";
		foreach (NetId p in availableTenants) {
			Resident tmp = getResident (p.id);
			s += tmp.personToString ();
		}
		return s;
	}

	public int getLeaseMonths() {
		return (LEASE_MONTHS - months);
	}

	/// <summary>
	/// Spawns buttons for interested tenants.
	/// </summary>
	public virtual void setButtons() {
		int i = 0;
		foreach (NetId id in availableTenants) {
			Resident p = getResident (id.id);
			if (p != null) {
				GameObject obj = (GameObject)Instantiate (Resources.Load ("TenantPanel"));

				obj.GetComponent<TenantPanel> ().person = p.personToString ();
				
				obj.GetComponent<TenantPanel> ().portNum = p.portrait;
				obj.transform.SetParent (GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").transform, false);
				obj.transform.position = new Vector3 (obj.transform.position.x + (101 * i), obj.transform.position.y, obj.transform.position.z);

				int tmp = i;
				Building tmpBuilding = gameObject.GetComponent<Building> ();

				if (tmpBuilding.validOwner ()) {
					obj.transform.Find ("Button").GetComponent<Button> ().onClick.AddListener (delegate {
						tmpBuilding.getPlayerOwner ().CmdSetTenant (tmp, gameObject.GetComponent<NetworkIdentity> ().netId);
					});
				}
				buttons.Add (obj);
				i++;
			}
		}
	}

	/// <summary>
	/// checks if the tenant is currently on their initial lease, which means they're protected from eviction and stuff
	/// </summary>
	/// <returns><c>true</c>, if lease was oned, <c>false</c> otherwise.</returns>
	public bool onLease() {
		bool b = false;
		if (months < LEASE_MONTHS) {
			b = true;
		}
		return b;
	}

	/// <summary>
	/// Spawns a button for the active tenant.
	/// </summary>
	public void showActive() {
		if (building.occupied && (resident != null)) {
			GameObject obj = (GameObject)Instantiate (Resources.Load ("TenantPanel"));
			if (resident.criminal) {
				obj.GetComponent<Image> ().color = Color.red;
			} else {
				obj.GetComponent<Image> ().color = Color.blue;
			}

			obj.GetComponent<TenantPanel> ().person = resident.personToString ();
			
			if (resident.employed()) {
				obj.GetComponent<TenantPanel> ().job = "Works at " + resident.jobBuilding.buildingName;
			} else {
				obj.GetComponent<TenantPanel> ().job = "Unemployed";
			}
			obj.GetComponent<TenantPanel> ().portNum = resident.portrait;
			obj.transform.SetParent (GameObject.Find ("Canvas").transform.Find ("ReadoutPanel").transform, false);
			obj.transform.position = new Vector3 (obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);
			if ((building.getOwner () != -1) && (building.getPlayerOwner().id == building.getPlayerOwner().localPlayer.id)) {
				obj.transform.Find ("Button").GetComponent<Button> ().onClick.AddListener (delegate {
					confirmEvict ();
				});
			}
			buttons.Add (obj);
		}
	}

	/// <summary>
	/// Spawns a confirmation box for an eviction.
	/// </summary>
	public void confirmEvict() {
		if (months >= LEASE_MONTHS) {
			GameObject obj = (GameObject)Instantiate (Resources.Load ("Confirmation"));
			obj.transform.SetParent (GameObject.Find ("Canvas").transform, false);
			obj.transform.position = new Vector3 (obj.transform.position.x, obj.transform.position.y, obj.transform.position.z);
			obj.transform.Find ("ConfirmMessage").GetComponent<Text> ().text = "Evict " + resident.residentName + " from " + building.buildingName + "? " +
			"This will cost $" + (building.baseRent * 2) + ".";
			if (building.getOwner () != -1) {
				obj.transform.Find ("Ok").GetComponent<Button> ().onClick.AddListener (delegate {
					building.getPlayerOwner().CmdEvict (building.getOwnerNetId(), building.GetComponent<NetworkIdentity> ().netId);
					Destroy (obj);
				});
			} 
			obj.transform.Find ("Cancel").GetComponent<Button> ().onClick.AddListener (delegate {
				Destroy (obj);
			});
		} else {
			messageOwner ("This tenant still has " + (LEASE_MONTHS - months) + " month(s) on their lease and cannot be evicted.");
		}
	}

	/// <summary>
	/// Returns the damage caused by the tenant each month to the building.
	/// Negative if the tenant repairs the building, ie if they're a handyman
	/// </summary>
	public int condition() {
		return resident.condition ();
	}

	public void applyEffects() {
		months++;
		resident.applyEffects ();
	}

	public bool willPay() {
		return resident.willPay ();
	}

	public void leaveJob() {
		if (resident != null) {
			resident.leaveJob ();
		}
	}

	public bool isNone() {
		bool b = false;
		if (resident == null) {
			b = true;
		} 
		return b;
	}
	/// <summary>
	/// Deletes all the tenant buttons.
	/// </summary>
	public void clearButtons() {
		foreach (GameObject b in buttons) {
			if (b != null) {
				b.GetComponent<TenantPanel> ().buttonDestroy ();		
			}
		}
		buttons.Clear ();
	}

	public float buildingScore () {
		int marketRent = monthManager.GetAverageRent (building.type);
		int marketCond = (monthManager.dictCondition[building.type] / monthManager.dictNumberOfType[building.type]);;
		int marketSafety = (monthManager.dictSafety[building.type] / monthManager.dictNumberOfType[building.type]);
		//Get attractiveness from other method
		//int attractivenessScore = 10;
		Building[] buildings = FindObjectsOfType<Building> ();
		float score = 0f;

		float rentEffect = ((float)marketRent / building.rent);
		if (rentEffect != 1) {
			rentEffect = rentEffect + ((rentEffect - 1) * 3); // add rent multiplier
		}

		float attractivenessEffect = 1f;
		score = 100f * rentEffect;     // (building.rent - marketRent) - (marketSafety - building.safety) - (marketCond - building.condition);
		if (!building.lot.IsEmpty()) { // add attractiveness multiplier
			attractivenessEffect = (building.getLot().getAttractiveness() / 100f);
			score *= attractivenessEffect;
		}
		//score += attractivenessScore;
		//score *= 10;
		return score;
	}

	/// <summary>
	/// Sets the active tenant.
	/// </summary>
	/// <param name="p">The new active tenant.</param>
	public virtual void setActive(NetworkInstanceId p) {
		activeTenant = p;
		if (activeTenant != NetworkInstanceId.Invalid) {
			resident = getResident (p);
			if (resident != null) {
				resident.setResidence (building.netId);
				building.occupied = true;
				if (building.getOwner () > -1)  {
					Player local = FindObjectOfType<Player> ().localPlayer;
					if (local != null) {
						local.updateUI ();
					}
				}
			} else {
				activeTenant = NetworkInstanceId.Invalid;
				building.occupied = false;
			}
		} else {
			resident = null;
		}
	}

	/// <summary>
	/// Evict the tenant and set the building to unoccupied.
	/// </summary>
	public virtual void evict() {
		if (resident != null) {
			resident.leaveHome ();
		}
		building.occupied = false;
		activeTenant = NetworkInstanceId.Invalid;
		resident = null;
		months = 0;
		clearButtons ();
	}

	public void messageOwner(string s) {
		if (building.validOwner()) { // notify the player if owned by an individual, else notify the company members
			if (building.getPlayerOwner().localPlayer != null && building.getPlayerOwner().id == building.getPlayerOwner().localPlayer.id) {
				building.getPlayerOwner().showMessage (s);
			}
		} 
	}

	/// <summary>
	/// returns the number of jobs available in the city
	/// </summary>
	/// <returns>The # of jobs.</returns>
	protected int availableJobs() {
		int i = 0;
		List<Business> jobs = new List<Business>();
		Business[] businesses = FindObjectsOfType<Business>();
		jobs = businesses.Where (b => ((b.workers.Count < b.neededWorkers) && (b.occupied))).ToList<Business>();
		i = jobs.Count;
		return i;
	}

	protected Resident getResident(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		Resident r = null;
		if (g != null) {
			r = g.GetComponent<Resident> ();
		}
		return r;
	}
}