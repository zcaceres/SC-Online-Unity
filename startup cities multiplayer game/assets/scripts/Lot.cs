﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class Lot : Building {
	const int TYPENUM = 17;
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

	protected static int lotNum = 1;
	[SyncVar]
	protected int lotAttractiveness;
	[SyncVar]
	protected NetworkInstanceId neighborhood;
	protected List<Building> lotBuildings;
	public SyncListNetId lotItems = new SyncListNetId();
	private GameObject lotSizeMarker;

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;
		//Gets LotSizeMarker, which shows size as overlay. Toggles it off to start.
		lotSizeMarker = gameObject.transform.Find ("LotSizeMarker").gameObject;
		lotSizeMarker.SetActive (false);

		if (isServer) {
			buildingName = "Lot " + lotNum.ToString();
			lotNum++;
			baseRent = 0;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 600;
			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			typeName = buildingTypes [type];
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			buildingNum++;
			if (lotBuildings == null) { // do this only if items weren't added before Start() was run
				lotSetup (); 
			} else {
				cost = calcPrice ();
			}
			updateRent ();
		}
	}

	private void lotSetup() {
		lotBuildings = new List<Building> ();
		lotAttractiveness = 100;
	}

	public override bool isDestructable() {
		return false;
	}

	/// <summary>
	/// override for advanceMonth removing standard stuff which is not applicable for lots (condition damage, etc)
	/// put vehicle/other node spawning here
	/// </summary>
	public override void advanceMonth() {
		if (!validOwner() && !validCompany()) { 
			notForSale = false;
		}	
	}

	/// <summary>
	/// tallies up the rent value of all buildings on the lot
	/// </summary>
	/// <returns>The total rent.</returns>
	public int calcPrice() {
		int price = 600; // base cost of the lot
		foreach (NetId id in lotItems) { // add cost from each building
			Building b = getLocalInstance (id.id).GetComponent<Building> ();
			price += b.appraise();
		}
		return price;
	}

	/// <summary>
	/// Calculates the revenue from the neighborhood.
	/// </summary>
	/// <returns>The rents.</returns>
	public int calcRents() {
		int rents = 0;
		List<Building> buildings = getBuildings ();

		foreach(Building b in buildings) {
			rents += b.getRent ();
		}

		return rents;
	}

	/// <summary>
	/// Calculates the upkeep from the neighborhood.
	/// </summary>
	/// <returns>The upkeep.</returns>
	public int calcUpkeep() {
		int u = 0;
		List<Building> buildings = getBuildings ();

		foreach(Building b in buildings) {
			u += b.upkeep;
		}

		return u;
	}

	public override int appraise() {
		return calcPrice ();
	}
	/// <summary>
	/// checks if the building is contained within a neighborhood
	/// </summary>
	/// <returns><c>true</c>, if part of a neighborhood, <c>false</c> otherwise.</returns>
	public override bool inNeighborhood() {
		bool b = false;
		if (!neighborhood.IsEmpty () && !(neighborhood == NetworkInstanceId.Invalid)) {
			b = true;
		}
		return b;
	}

//	public void calcAttractiveness() {
////		lotAttractiveness = 100;
////		foreach (Building b in lotBuildings) {
////			lotAttractiveness += b.attractiveness;
////		}
//	}

	/// <summary>
	/// empty override for fire starting so that lots don't catch on fire
	/// </summary>
	public override void setFire() {
	}

	/// <summary>
	/// Sets the neighborhood for the lot.
	/// </summary>
	/// <returns><c>true</c>, if neighborhood was close enough to assign to the lot, <c>false</c> otherwise.</returns>
	/// <param name="n">Neighborhood id.</param>
	/// <param name="t">Transform of the neighborhood.</param>
	public bool setNeighborhood(NetworkInstanceId n, Transform t) {
		bool valid = false;
		float distance = Vector3.Distance (this.transform.position, t.position);
		if (distance < 50f) {
			valid = true;
			neighborhood = n;
		} else {
			RpcMessageOwner ("This lot is not close enough to that neighborhood.");
		}
		return valid;
	}

	public void unsetNeighborhood() {
		Neighborhood n = getNeighborhood ();
		if (n != null) {
			n.removeLot (this);
			neighborhood = NetworkInstanceId.Invalid;
		}
	}

	public override Neighborhood getNeighborhood() {
		Neighborhood hood = null;
		GameObject n = getLocalInstance (neighborhood);
		if (n != null) {
			hood = n.GetComponent<Neighborhood> ();
		}
		return hood;
	}

	new public int getAttractiveness() {
		return lotAttractiveness;
	}

	public void addObject(NetworkInstanceId id) {
		lotItems.addId (id);
		//calcAttractiveness ();
		Building tmp = getLocalInstance (id).GetComponent<Building> ();
		if (lotBuildings == null) {
			lotSetup ();
		}
		lotBuildings.Add (tmp);
		if (!validOwner ()) {
			cost = calcPrice ();
		}
		lotAttractiveness += tmp.getAttractEffect();
		//updateNeighborhoodValue ();
	}

	public void removeObject(NetworkInstanceId id) {
		lotItems.removeId(id);
		Building tmp = getLocalInstance (id).GetComponent<Building> ();
		lotAttractiveness -= tmp.getAttractEffect();
		lotBuildings.Remove (tmp);
		//updateNeighborhoodValue ();
	}

	/// <summary>
	/// Shows the size of the lot. Toggled from month manager for overlay map.
	/// </summary>
	public void showLotSize(bool toggleOn) {
		if (toggleOn) {
			lotSizeMarker.SetActive (true);
		} else {
			lotSizeMarker.SetActive (false);
		}
	}

	public List<Building> getBuildings() {
		List<Building> buildings;
		if (isServer) {
			buildings = lotBuildings;
		} else {
			buildings = new List<Building> ();
			foreach (NetId n in lotItems) {
				Building tmp = getLocalInstance (n.id).GetComponent<Building> ();
				buildings.Add (tmp);
			}
		}
		return buildings;
	}

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
			foreach (NetId netId in lotItems) {
				NetworkInstanceId id = netId.id;
				Building b = getLocalInstance (id).GetComponent<Building> ();
				Player tmp = b.getPlayerOwner ();
				if (tmp != null) {
					b.getPlayerOwner ().owned.removeId (id);
				}
				b.setOwner(newOwner);
				b.notForSale = true;
			}
		}
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public override string getReadout(NetworkInstanceId pid) {
		string s;
		updateRent ();
		modManager.clearButtons ();
		Neighborhood tmp = getNeighborhood ();
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + getCost ();;

		if (tmp != null) {
			s += "\nNeighborhood: " + tmp.buildingName;
		}
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
		Neighborhood tmp = getNeighborhood ();
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + getCost ();
		if (tmp != null) {
			s += "\nNeighborhood: " + tmp.buildingName;
		}
		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
		s += "\nAttractiveness: " + getAttractiveness ();
		return s;
	}

	/// <summary>
	/// City reclaims the building and pays the player the base cost (for sub lot items too)
	/// </summary>
	public override void repo () {
		if (validOwner ()) {
			foreach (Building b in lotBuildings) {
				b.repo ();
			}
			Player p = getPlayerOwner ();
			p.budget += baseCost;
			p.owned.removeId (this.netId);
			owner = NetworkInstanceId.Invalid;
			notForSale = false;
			cost = appraise ();
		}
	}
	/// <summary>
	/// recalculates the value of the lot's neighborhood. use when adding/removing items on the lot
	/// </summary>
	protected override void updateNeighborhoodValue() {
		Neighborhood n = getNeighborhood ();
		if (n != null) {
			n.calcPrice ();
		}
	}
}