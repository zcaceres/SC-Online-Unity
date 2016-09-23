using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class Neighborhood : Building {
	private static string[] rSmallFirst = { "Happy", "Pleasant", "Central", "Maple", "Pine", "Riverside", "Pleasantview", "Charming", "Ocean", "Mystic", "Oak", "Willow" };
	private static string[] rSmallLast = { "Street", "Meadows", "District", "View", "Boulevard", "Gardens", "Road", "Zone" };
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

	private SyncListNetId lots = new SyncListNetId();
	//private List<Lot> neighborhoodLots;
	private int nAttractiveness;
	private TextMesh text;

	[SyncVar]
	private bool hasManager;
	// Use this for initialization
	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		text = gameObject.transform.Find ("Name").GetComponent<TextMesh> ();
		color = text.color;
		text.text = buildingName;
		if (isServer) {
			//neighborhoodLots = new List<Lot> ();
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			nAttractiveness = 0;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 0;
			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			type = 21;
			typeName = buildingTypes [type];
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			buildingNum++;
			updateRent ();
			calcPrice ();
		}
	}
	
	/// <summary>
	/// override for advanceMonth removing standard stuff which is not applicable for lots (condition damage, etc)
	/// put vehicle/other node spawning here
	/// </summary>
	public override void advanceMonth() {
		if (!validOwner() && !validCompany()) { 
			notForSale = false;
		}	

		if (hasManager) {
			manageNeighborhood ();
			upkeep = getManagerCost ();
		}
		calcAttractiveness ();
	}

	public void setManager(bool b) {
		hasManager = b;
		if (hasManager) {
			upkeep = getManagerCost ();
		} else {
			upkeep = 0;
		}
	}

	public bool isManaged() {
		return hasManager;
	}

	public override int appraise() {
		return calcPrice ();
	}

	private int getManagerCost() {
		int i = 0;
		if (hasManager) {
			i = getManagerSalary();
		}
		return i;
	}

	private void calcAttractiveness() {
		nAttractiveness = 0;
		foreach (NetId netId in lots) {
			Lot b = getLocalInstance (netId.id).GetComponent<Lot> ();
			nAttractiveness += b.getAttractiveness();
		}
	}
		
	public int calcPrice() {
		int newCost = 0;
		foreach (NetId lot in lots) {
			Lot l = getLocalInstance (lot.id).GetComponent<Lot> ();
			newCost += l.calcPrice();
		}
		return newCost;
	}

	/// <summary>
	/// Calculates the revenue from the neighborhood.
	/// </summary>
	/// <returns>The rents.</returns>
	public int calcRents() {
		int rents = 0;
		List<Lot> lots = getLots ();
		foreach (Lot l in lots) {
			List<Building> buildings = l.getBuildings ();
			foreach(Building b in buildings) {
				rents += b.getRent ();
			}
		}
		return rents;
	}

	/// <summary>
	/// Calculates the upkeep from the neighborhood.
	/// </summary>
	/// <returns>The upkeep.</returns>
	public int calcUpkeep() {
		int u = 0;
		List<Lot> lots = getLots ();
		foreach (Lot l in lots) {
			List<Building> buildings = l.getBuildings ();
			foreach(Building b in buildings) {
				u += b.upkeep;
			}
		}
		return u;
	}

	/// <summary>
	/// Gets a list of the lots referred to by the netids in the lots synclist
	/// </summary>
	/// <returns>The lots.</returns>
	public List<Lot> getLots() {
		List<Lot> tmp = new List<Lot> ();

		foreach (NetId netId in lots) {
			Lot l = getLocalInstance (netId.id).GetComponent<Lot> ();
			tmp.Add (l);
		}
		return tmp;
	}

	/// <summary>
	/// Gets the number of lots contained within the neighborhood.
	/// </summary>
	/// <returns>The number of lots.</returns>
	public int numLots() {
		return lots.Count;
	}

	public void addLot(Lot l) {
		bool valid = l.setNeighborhood (this.netId, this.transform); // checks if the lot is nearby
		if (valid) {
			lots.addId (l.netId);
			//neighborhoodLots.Add (l);
			setPosition ();
		}
		calcPrice ();
	}

	public void removeLot(Lot l) {
		lots.removeId (l.netId);
		if (lots.Count < 1) {
			if (validOwner ()) {
				getPlayerOwner ().owned.removeId (this.netId);
			}
			Destroy (this.gameObject);
		} else {
			setPosition ();
		}
		calcPrice ();
	}

	public int getManagerSalary() {
		return calcPrice () / 36;
	}
	/// <summary>
	/// empty override for fire starting so that neighborhoods don't catch on fire
	/// </summary>
	public override void setFire() {
	}

	/// <summary>
	/// Sets the owner of the neighborhood & all lots contained within the neighborhood.
	/// </summary>
	/// <param name="newOwner">New owner's netid.</param>
	public override void setOwner(NetworkInstanceId newOwner) {
		if (isServer) {
			if (newOwner == owner)
				return;
			Player oldOwner = this.getPlayerOwner ();
			if (oldOwner != null) {
				oldOwner.owned.removeId (this.netId);
			}
			Player p = getLocalInstance (newOwner).GetComponent<Player> ();
			p.owned.addId (this.netId);
			owner = newOwner;
			foreach (NetId netId in lots) {
				NetworkInstanceId id = netId.id;
				Lot b = getLocalInstance (id).GetComponent<Lot> ();
				Player tmp = b.getPlayerOwner ();
				if (tmp != null) {
					if (tmp.id != p.id) {
						tmp.owned.removeId (id);
					} else {
						continue;
					}
				}
				b.setOwner(newOwner);
			}
			notForSale = true;
		}
	}

	/// <summary>
	/// Adds a color overlay to the building
	/// </summary>
	/// <param name="newColor">New color.</param>
	public override void setColor(Color newColor) {
		text.color = newColor;
	}

	public void setName(string s) {
		buildingName = s;
		TextMesh tmp = gameObject.transform.Find("Name").GetComponent<TextMesh>();
		tmp.text = s;
	}

	public static string nameGen() {
		string name;
		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}

	private void setPosition() {
		if (lots.Count > 1) {
			List<Lot> tmp = getLots ();
			float highestX;
			float lowestX;
			float highestZ;
			float lowestZ;
			float x;
			float z;
			Transform highest;
			Transform lowest;
			highestX = tmp.OrderByDescending (t => (t.transform.position.x)).ToList () [0].transform.position.x;
			lowestX = tmp.OrderBy (t => (t.transform.position.x)).ToList () [0].transform.position.x;
			highestZ = tmp.OrderByDescending (t => (t.transform.position.z)).ToList () [0].transform.position.z;
			lowestZ = tmp.OrderBy (t => (t.transform.position.z)).ToList () [0].transform.position.z;
			tmp.OrderBy (t => (t.transform.position.z + t.transform.position.x)).ToList ();
			lowest = tmp.First ().transform;
			highest = tmp.Last ().transform;
			x = (highestX + lowestX) / 2;
			z = (highestZ + lowestZ) / 2;
			this.transform.rotation = Quaternion.FromToRotation (new Vector3(lowestX, transform.position.y, lowestZ), new Vector3(highestX, transform.position.y, highestZ));
			this.transform.position = new Vector3 (x, transform.position.y, z);
		}
	}

	private void manageNeighborhood() {
		int cost = 0;
		Player p = getPlayerOwner ();
		List<Lot> tmpLots = getLots ();
		foreach (Lot l in tmpLots) {
			List<Building> buildings = l.getBuildings ();
			foreach (Building b in buildings) {
				if (!b.fire) {
					cost += b.getRepairCost ();
					b.repair ();
				}

				if (b.modManager.mods.Count > 0) {
					cost += b.modManager.removeAllMods ();	
				}

				if (!b.isOccupied ()) {
					if (b.tenant.availableTenants.Count > 0) {
						p.setTenant (0, b);
					}
				}
			}
		}
		p.budget -= cost;
		RpcMessageOwner ("$" + cost + " was spent to maintain " + buildingName);
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadout(NetworkInstanceId pid) {
		string s;
		updateRent ();
		modManager.clearButtons ();
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + getCost ();
		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		s += "\nAttractiveness: " + getAttractiveness ();
		return s;
	}

	/// <summary>
	/// Returns the data associated with the building, does not do anything with buttons
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadoutText(NetworkInstanceId pid) {
		string s;
		updateRent ();
		modManager.clearButtons ();
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + getCost ();
		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		s += "\nAttractiveness: " + getAttractiveness ();
		return s;
	}

	public override void repo () {
		if (validOwner ()) {
			List<Lot> nLots = getLots ();
			foreach (Lot l in nLots) {
				l.repo ();
			}
			Player p = getPlayerOwner ();
			p.owned.removeId (this.netId);
			owner = NetworkInstanceId.Invalid;
			cost = appraise ();
			notForSale = false;
			hasManager = false;
		}
	}
}
