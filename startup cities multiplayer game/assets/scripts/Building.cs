using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(BuildingModifier))]
[RequireComponent(typeof(Tenant))]

public class Building : NetworkBehaviour {
	const int ATTRACTIVENESS_EFFECT = 0;
	public int type;
	const int TYPENUM = 0;

	[SyncVar]
	public string buildingName;
	[SyncVar]
	public string typeName;
	[SyncVar]
	public int safety;
	[SyncVar]
	public int condition;
	[SyncVar]
	public int rent;
	[SyncVar]
	public int baseRent;
	[SyncVar]
	public int cost;
	[SyncVar]
	public int upkeep;
	[SyncVar]
	public int id;              // Unique building id: not the name
	[SyncVar]
	public string officeName;
	[SyncVar]
	public bool occupied;
	[SyncVar]
	public bool notForSale;
	[SyncVar]
	public bool onAuction;
	[SyncVar]
	protected NetworkInstanceId owner;
	[SyncVar]
	public NetworkInstanceId lot;
	[SyncVar]
	public int rentOffset;
	[SyncVar]
	public int playerSetRent;

	public Lot localLot;
	public Color color;           // The original building color
	public Collider c;            // Building's collider
	private FireTransform[] fireTrans; //The number of fire transforms connected to the building

	[SyncVar]
	public NetworkInstanceId company;
	public BuildingModifier modManager; // The modmanager attached to the building
	public Tenant tenant;

	[SyncVar]
	public bool fire;             // Is the building on fire?
	[SyncVar]
	public bool ruin;             // Ruined buildings provide no rent and cannot have occupants
	[SyncVar]
	public bool upgrade;          // Upgrade buildings don't start with maluses 
	[SyncVar]
	public int lowestSkill;       // lowest skilled residents who will live at the building. 
	[SyncVar]
	public int baseCondition;
	[SyncVar]
	protected int baseSafety;
	[SyncVar]
	protected int baseCost;
	[SyncVar]
	protected bool paying;
	protected const int UPKEEP_PORTION = 4;
	protected static int buildingNum = 0;
	protected static string[] buildingTypes = { "Generic Building", "House", "Apartment Building", "Restaurant", "Hardware Store", "Park", "Junkyard", "Abandoned Lot", "Grocery Store", "Tenement", 
		"Small Apartment Building", "Medium Apartment Building", "Large Apartment Building", "Small Mixed-Use Building", "SuperMart", "Billboard", "City Hall", "Lot", "Decoration", "Trailer", "Factory", "Neighborhood", "Sidewalk", "Dirt Path",
		"Neighborhood Services", "Junk", "Office"};

	void Start () {
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		type = TYPENUM;

		if (isServer) {
			lowestSkill = 0;
			buildingName = buildingNum.ToString();
			baseRent = 100;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = rent * 12;
			//The price AI will ask for the initial sale and the price that repairs are based off of
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			id = buildingNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			buildingNum++;
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
			}
			updateRent ();
		}
		typeName = buildingTypes [type];
	}

	public virtual int getAttractEffect() {
		return ATTRACTIVENESS_EFFECT;
	}

	/// <summary>
	/// Applies any modifiers to the rent
	/// </summary>
	public virtual void updateRent() {
		if (isServer) {
			if (ruin) {
				rent = 0;
			} else {
				// set all to pre-modifier values first
				rent = baseRent;
				condition = baseCondition;
				safety = baseSafety;

				// then apply the modifiers
				modManager.apply ();
				rent += rentOffset;
				playerSetRent = rent;
				rent = (int)(rent * (condition / 100f));
			}
		}
	}


	/// <summary>
	/// Returns an up-to-date rent amount, or 0 if the building lacks tenants
	/// </summary>
	/// <returns>The rent.</returns>
	public virtual int getRent() {
		int currentRent = 0;

		if (occupied && paying) {
			updateRent ();
			currentRent = rent;
		} 

		return currentRent;
	}

	/// <summary>
	/// Advances the month, applies condition damage, updates the rent, causes fire damage.
	/// </summary>
	public virtual void advanceMonth() {
		if (isServer) {
			if (condition > 25) {
				damageBuilding (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner() && !validCompany()) { 
				notForSale = false;
			} else if (occupied) {                         // occupied, apply effects from the tenant
				tenant.clearButtons();
				damageBuilding(tenant.condition());
				paying = tenant.willPay ();

				if (!paying) {
					RpcMessageOwner(tenant.resident.residentName + " failed to pay rent this month!");
				}
				tenant.applyEffects ();
			}
				
			if (fire) {
				if (condition <= 0) {
					endFire ();
				}
				else {
					spreadFire ();
					damageBuilding (50);
				}
			}
				
			if (condition <= 0) {
				if (!ruin) { // building is destroyed: make it a ruin over RPC and make the tenant leave the building and quit their job
					makeRuin();
				}
			} else {
				//flipOccupancy ();
				updateRent ();
			}
		}
	}


	/// <summary>
	/// Checks the state of the fire.
	/// </summary>
	public void CheckFireState () {
		int fires = 0;
		FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform> ();
		foreach (FireTransform ft in fireTrans) {
			if (ft.onFire) {
				fires += 1;
			}
		}
		if (fires == 0) {
			endFire ();
		}
	}

	public virtual int getCost() {
		return cost;
	}

	public virtual int getBaseCost() {
		return baseCost;
	}

	public virtual bool isDestructable() {
		return true;
	}

	public virtual Neighborhood getNeighborhood() {
		Neighborhood n = null;
		if (validLot ()) {
			n = getLot ().getNeighborhood ();
		}
		return n;
	}

	protected void makeRuin() {
		if (isServer) {
			tenant.leaveJob ();
			RpcMakeRuin ();
		}
	}
	/// <summary>
	/// Spreads fire to neighbors.
	/// </summary>
	protected void spreadFire() {
		Collider[] colliding = Physics.OverlapSphere(c.transform.position, 5);
		foreach (Collider hit in colliding) {
			Building b = hit.GetComponent<Building> ();

			if (b != null && !b.fire) {
				if (Random.value < .1f) {
					b.setFire ();
				}
			}
		}
	}

	/// <summary>
	/// Repair the building to 100 condition.
	/// </summary>
	public void repair() {

		if (isServer) {
			if (ruin) {
				RpcFixRuin ();
			} else {
				condition = 100;
				baseCondition = 100;
				updateRent ();
			}
		}
	}

	public void repairByPoint(int numPoints) {
		if (isServer && !ruin) {
			condition += numPoints;
			baseCondition += numPoints;
		}
	}
	/// <summary>
	/// Sets the building on fire.
	/// </summary>
	public virtual void setFire() {
		if (isServer) {
			if (validOwner()) {
				RpcMessageOwner( buildingName + " is on fire!");
			}
			fire = true;
			GameObject fireObj = (GameObject)Resources.Load ("HouseFire");
			FireTransform[] fireTrans = gameObject.GetComponentsInChildren<FireTransform>();
			if (fireTrans.Length < 1) {
				GameObject tmp = (GameObject)Instantiate (fireObj, new Vector3 (gameObject.transform.position.x, getHighest(), gameObject.transform.position.z), fireObj.transform.rotation);
				NetworkServer.Spawn (tmp);
				Debug.LogError ("Building is on fire but has no transforms");
			}
			foreach (FireTransform ft in fireTrans) {
				GameObject tmp = (GameObject)Instantiate (fireObj, ft.transform.position, fireObj.transform.rotation);
				FireKiller fk = tmp.GetComponent<FireKiller> ();
				ft.onFire = true; //Tells the fire transform that it is on fire. All fts must report back OnFire = false for advance month to consider the building not on fire!
				fk.myTransform = ft; //sets the FireKiller's firetransform, which allows it to update the FT about the state of the fire!
				fk.setBuilding (gameObject.GetComponent<Building> ());
				NetworkServer.Spawn (tmp);
			}
		}
	}

	/// <summary>
	/// Ends the fire.
	/// </summary>
	public void endFire() {
		if (isServer) {
			fire = false;
		}
	}
		
	/// <summary>
	/// returns the owner ID (player number) or -1 if unowned
	/// </summary>
	/// <returns>The owner ID.</returns>
	public int getOwner() {
		int id;

		if (!validOwner()) {
			id = -1;
		} else {
			id = getPlayer(owner).id;
		}

		return id;
	}

	public int getAttractiveness() {
		int a = 0;
		GameObject tmp = getLocalInstance (lot);
		if (tmp != null) {
			Lot l = tmp.GetComponent<Lot> ();
			a += l.getAttractiveness ();
		}
		return a;
	}

	/// <summary>
	/// Returns the data associated with the building
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadout(NetworkInstanceId pid) {
		string s;
		updateRent ();
		modManager.clearButtons ();
		string ownerName = "";
		GameObject l = getLocalInstance (lot);
		if (!validOwner()) {
			ownerName = "None";
		} else {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes[type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + cost + "\nCondition: " + conditionToString() + "\nSafety: " + safetyToString() +  "\nRent: " + rent + "\n" + officeName;

		if (occupied) {
			if (!tenant.isNone()) {
				s += "\nOccupant: " + tenant.resident.residentName;
			}
		} else {
			s += "\nNot Occupied";
		}

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}
			
		if (l != null) {
			Lot tmp = l.GetComponent<Lot> ();
			s += "\nAttractiveness: " + tmp.getAttractiveness ();
		}
		if (modManager != null) {
			s += modManager.readout (pid);
		}
		return s;
	}
		
	/// <summary>
	/// Returns the data associated with the building, does not do anything with buttons
	/// </summary>
	/// <returns>The readout.</returns>
	public virtual string getReadoutText(NetworkInstanceId pid) {
		string s;
		string ownerName = "";
		if (!validOwner()) {
			ownerName = "None";
		} else  {
			ownerName = getPlayer(owner).getName();
		}
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nOwner: " + ownerName + "\nPrice: " + cost + "\nCondition: " + conditionToString () + "\nSafety: " + safetyToString () +  "\nRent: " + rent + "\n" + officeName;

		if (occupied) {
			if (!tenant.isNone()) {
				s += "\nOccupant: " + tenant.resident.residentName;
			}
		} else {
			s += "\nNot Occupied";
		}

		if (notForSale) {
			s += "\nNot for sale";
		} else {
			s += "\n<color=#00ff00ff>For Sale</color>";
		}

		GameObject l = getLocalInstance (lot);
		if (l != null) {
			Lot tmp = l.GetComponent<Lot> ();
			s += "\nAttractiveness: " + tmp.getAttractiveness ();
		}

		if (modManager.mods.Count > 0) {
			s += "\nModifiers: ";
		}
		return s;
	}

	/// <summary>
	/// Gets the worker text. For base class, there are no workers, so returns empty.
	/// </summary>
	/// <returns>The worker text.</returns>
	public virtual string getWorkerText() {
		string s = "";
		return s;
	}

	/// <summary>
	/// Returns a shorter version of the normal readout for use in auctions
	/// </summary>
	/// <returns>The readout.</returns>
	public string getReadoutAuction() {
		string s;
		s = "Type: " + buildingTypes [type] + "\nName : " + buildingName + "\nCondition: " + condition + "\nSafety: " + safety + "\nPotential Rent: " + rent + "\n";

		return s;
	}

	/// <summary>
	/// Small readout for the ledger
	/// </summary>
	/// <returns>The ledger readout for the building.</returns>
	public string getLedgerReadout() {
		string s = "\n" + buildingName + "\n";
		if (occupied) {
			s += "\tRent: $" + getRent ();
		} else {
			s += "\tRent: $0 (no tenant)";
		}
		s += "\n\tUpkeep: $" + upkeep;
		if ((tenant != null)  && (tenant.resident != null)) {
			s += "\n\tTenant: " + tenant.resident.residentName;
		}
		return s;
	}

	/// <summary>
	/// Adds a color overlay to the building
	/// </summary>
	/// <param name="newColor">New color.</param>
	public virtual void setColor(Color newColor) {
		if (c != null) {
			c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color = newColor;
		}
	}

	/// <summary>
	/// Returns the building to its original color, or black if its a ruin
	/// </summary>
	public void resetColor() {
		if (ruin) {
			setColor (Color.black);
		} else {
			setColor(color);
		}
	}

	/// <summary>
	/// did the tenant pay last month?
	/// </summary>
	public bool paid() {
		bool paid = paying;
		return paid;
	}

	/// <summary>
	/// Returns the highest point of the building's mesh.
	/// </summary>
	/// <returns>Highest point.</returns>
	public float getHighest()
	{
		if ((c != null) && (c.gameObject.GetComponent<MeshCollider>() != null) && (c.gameObject.GetComponent<MeshCollider>().sharedMesh != null)) {
			Vector3[] verts = c.gameObject.GetComponent<MeshCollider> ().sharedMesh.vertices;
			Vector3 topVertex = new Vector3 (0, float.NegativeInfinity, 0);
			for (int i = 0; i < verts.Length; i++) {
				Vector3 vert = transform.TransformPoint (verts [i]);
				if (vert.y > topVertex.y) {
					topVertex = vert;
				}
			}

			return topVertex.y;
		} else {
			return 0;
		}
	}

	/// <summary>
	/// Gets the cost to restore the building to 100 condition
	/// </summary>
	/// <returns>The repair cost.</returns>
	public int getRepairCost() {
		int repairCost;
		if (ruin) {
			repairCost = baseCost;
		} else {			
			repairCost = (100 - baseCondition) * (baseRent / 24);
		}
		return repairCost;
	}

	/// <summary>
	/// Gets the cost of repairing a single point of condition.
	/// </summary>
	/// <returns>The point repair cost.</returns>
	public int getPointRepairCost() {
		int repairCost;
		if (ruin) {
			repairCost = baseCost;
		} else {
			repairCost = baseRent / 24;
		}
		return repairCost;
	}
	/// <summary>
	/// Turns the building to a ruin--used when condition is 0.
	/// </summary>
	[ClientRpc]
	protected void RpcMakeRuin() {
		setColor (Color.black);
		if (isServer) {
			ruin = true;
			occupied = false;
			tenant.clearButtons ();
			tenant.availableTenants.Clear ();
			endFire ();
			updateRent ();
		}
	}

	/// <summary>
	/// Repair a ruined building
	/// </summary>
	[ClientRpc]
	protected void RpcFixRuin() {
		if (ruin) {
			if (isServer) {
				condition = 100;
				baseCondition = 100;
				ruin = false;
				updateRent ();
			}
			setColor (color);
		}
	}

	/// <summary>
	/// Messages the owner without using the message syncvar
	/// </summary>
	/// <param name="s">message string.</param>
	[ClientRpc]
	public void RpcMessageOwner(string s) {
		if (validOwner()) {
			getPlayer(owner).showMessage(s);
		}
	}

	/// <summary>
	/// Damages the building. Decrements base condition & condition--base condition manages the condition without considering modifiers
	/// </summary>
	/// <param name="damage">Damage.</param>
	protected void damageBuilding(int damage) {
		if (isServer) {
			if ((baseCondition - damage) > 100) {      // don't go above 100
				baseCondition = 100;
				condition = 100;
			} else if ((baseCondition - damage) < 0) { // don't go below 0
				baseCondition = 0;
				condition = 0;
			} else {
				baseCondition -= damage;
				condition -= damage;
			}
		}
	}

	/// <summary>
	/// Damages the building's safety. Decrements base safety & safety--base safety manages the condition without considering modifiers
	/// </summary>
	/// <param name="damage">Damage.</param>
	public void damageBuildingSafety(int damage) {
		if (isServer) {
			if ((baseSafety - damage) > 100) {      // don't go above 100
				baseSafety = 100;
				safety = 100;
			} else if ((baseSafety - damage) < 0) { // don't go below 0
				baseSafety = 0;
				safety = 0;
			} else {
				baseSafety -= damage;
				safety -= damage;
			}
		}
	}

	/// <summary>
	/// Messages the owner.
	/// </summary>
	/// <param name="s">Message.</param>
	public void messageOwner(string s) {
		if (isServer) {
			if (validOwner()) {
				getPlayer(owner).message = s;
			}
		}
	}

	/// <summary>
	/// Gives the owner money.
	/// </summary>
	/// <param name="money">amount of money.</param>
	public void giveOwnerMoney(int money) {
		if (isServer) {
			if (validOwner()) {
				getPlayer(owner).budget += money;
			}
		}
	}

	/// <summary>
	/// Checks if the building is occupied
	/// </summary>
	/// <returns><c>true</c>, if occupied , <c>false</c> otherwise.</returns>
	public bool isOccupied() {
		bool b = false;
		if (occupied && (tenant != null) && (tenant.resident != null)) {//&& (tenant.resident.residentName != null) && (tenant.resident.residentName != "None")) {
			b = true;
		}
		return b;
	}
		
	protected string safetyToString() {
		string s = "";
		if (safety == 100) {
			s = "<color=#00ff00ff>Very Safe</color>";
		} else if (safety > 84) {
			s = "<color=#00ff00ff>Safe</color>";
		} else if (safety > 74) {
			s = "<color=#fff5f5dc>Unsafe</color>";
		} else if (safety > 49) {
			s = "<color=#ffa52a2a>Very Unsafe</color>";
		} else if (safety > 24) {
			s = "<color=#ffcd5c5c>Crime-stricken</color>";
		} else if (safety > 1) {
			s = "<color=#ff0000ff>Extremely Dangerous</color>";
		} else {
			s = "<color=#ff0000ff>Warzone</color>";
		}
		return s;
	}

	protected string conditionToString() {
		string s = "";
		if (condition == 100) {
			s = "<color=#00ff00ff>Perfect</color>";
		} else if (condition > 84) {
			s = "<color=#00ff00ff>Good</color>";
		} else if (condition > 74) {
			s = "<color=#fff5f5dc>Fair</color>";
		} else if (condition > 49) {
			s = "<color=#ffa52a2a>Damaged</color>";
		} else if (condition > 24) {
			s = "<color=#ffcd5c5c>Badly Damaged</color>";
		} else {
			s = "<color=#ff0000ff>Condemned</color>";
		}
		return s;
	}

	public virtual bool inNeighborhood() {
		bool b = false;
		if (validLot ()) {
			if (getLot ().inNeighborhood ()) {
				b = true;
			}
		}
		return b;
	}

	/// <summary>
	/// gives the base cost of the building (cost before modifiers/condition changes/rent changes)
	/// </summary>
	public virtual int appraise() {
		return baseCost;
	}

	public virtual Player getPlayer(NetworkInstanceId playerId) {
		GameObject tmp = getLocalInstance (playerId);
		Player p;
		if (tmp != null) {
			p = tmp.GetComponent<Player> ();
		} else {
			p = null;
		}
		return p;
	}

	public virtual NetworkInstanceId getOwnerNetId() {
		return owner;
	}

	public virtual Player getPlayerOwner() {
		Player p;
		if (validOwner()) {
			p = getPlayer (owner);
		} else {
			p = null;
		}
		return p;
	}

	public virtual Lot getLot() {
		Lot l;

		if (this is Lot) {
			l = this.GetComponent<Lot> ();
		} else {
			if (validLot ()) {
				l = getLocalInstance (lot).GetComponent<Lot> ();
			} else {
				l = null;
			}
		}
		return l;
	}

	public virtual void setRent(int newRent) {
		rentOffset = newRent - baseRent;
		updateRent ();
	}

	/// <summary>
	/// Sets the owner and removes the building from the owned list of its previous owner.
	/// </summary>
	/// <param name="newOwner">New owner's id.</param>
	public virtual void setOwner(NetworkInstanceId newOwner) {
		if (newOwner == owner)
			return;
		Player oldOwner = getPlayerOwner ();
		if (oldOwner != null) {
			oldOwner.owned.removeId (this.netId);
		}
		Player p = getLocalInstance (newOwner).GetComponent<Player> ();
		p.owned.addId (this.netId);
		owner = newOwner;
	}

	/// <summary>
	/// Checks ownership by netId
	/// </summary>
	/// <returns><c>true</c>, if owned by the object (company or player) whose netId was passed, <c>false</c> otherwise.</returns>
	/// <param name="o">Owner.</param>
	public virtual bool ownedBy(NetworkInstanceId o) {
		bool owned = false;
		if (validOwner()) {
			if (owner == o) {
				owned = true;
			}
		}
		return owned;
	}

	public virtual bool ownedBy(Player p) {
		bool owned = false;
		if (owner == p.netId) {
			owned = true;
		}
		return owned;
	}

	public virtual void unsetOwner() {
		owner = NetworkInstanceId.Invalid;
	}

	public virtual void unsetCompany() {
		company = NetworkInstanceId.Invalid;
	}

	/// <summary>
	/// City reclaims the building and pays the player the base cost
	/// </summary>
	public virtual void repo() {
		if (validOwner ()) {
			Player p = getPlayerOwner ();
			p.budget += this.appraise ();
			p.owned.removeId (this.netId);
			owner = NetworkInstanceId.Invalid;
			notForSale = false;
			if (occupied) {
				tenant.evict ();
			}
		}
	}

	public virtual bool validOwner() {
		bool isValid = false;
		if (!owner.IsEmpty() && (owner != NetworkInstanceId.Invalid) && (getLocalInstance(owner) != null)) {
			isValid = true;
		}
		return isValid;
	}

	public virtual bool validCompany() {
		bool isValid = false;
		if (!company.IsEmpty() && (company != NetworkInstanceId.Invalid) && (getLocalInstance(company) != null)) {
			isValid = true;
		}
		return isValid;
	}

	public virtual bool validLot() {
		bool isValid = false;
		if (!lot.IsEmpty() && (lot != NetworkInstanceId.Invalid) && (getLocalInstance(lot) != null)) {
			isValid = true;
		}
		return isValid;
	}

	protected virtual void updateNeighborhoodValue() {
		if (validLot ()) {
			Neighborhood n = getLot ().getNeighborhood ();
			if (n != null) {
				n.calcPrice ();
			}
		}
	}

	protected GameObject getLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}


	//BEGIN EARTHQUAKE STUFF

	/// <summary>
	/// Begins earthquake damage cycle
	/// </summary>
	/// <param name="prob">probability from Month Manager</param>
	/// <param name="explosion">Explosion prefab</param>
	/// <param name="delay">Delay to wait before explosion</param>
	public void earthQuakeDamage (float prob, GameObject explosion, float delay) {
		damageBuilding (30);
		if (prob <= 3.0f) {
			StartCoroutine (earthQuakeDelay(delay, explosion));
		}
	}


	/// <summary>
	/// Creates a delay and then decides whether the building explodes
	/// </summary>
	/// <returns>The quake delay.</returns>
	/// <param name="delay">Delay.</param>
	/// <param name="explosion">Explosion.</param>
	IEnumerator<WaitForSeconds> earthQuakeDelay(float delay, GameObject explosion) {
		yield return new WaitForSeconds (delay);
		if (Random.Range (0, 10) < 3) {
			earthQuakeExplosion (explosion);
		} else {
			setFire ();
		} 
	}


	/// <summary>
	/// Sets off an explosion in a building, causing a fire and additional damage
	/// </summary>
	/// <param name="explosion">Explosion.</param>
	private void earthQuakeExplosion (GameObject explosion) {
		GameObject tmp = (GameObject)Instantiate (explosion, gameObject.transform.position, gameObject.transform.rotation);
		NetworkServer.Spawn (tmp);
		damageBuilding (15);
		setFire ();
	}

}