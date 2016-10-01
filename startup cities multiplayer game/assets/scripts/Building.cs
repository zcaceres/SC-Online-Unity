using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(BuildingModifier))]
[RequireComponent(typeof(Tenant))]

public class Building : DamageableObject {
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
	public int rent;
	[SyncVar]
	public int baseRent;
	[SyncVar]
	public int upkeep;
	[SyncVar]
	public string officeName;
	[SyncVar]
	public bool occupied;
	[SyncVar]
	public bool onAuction;
	[SyncVar]
	public int rentOffset;
	[SyncVar]
	public int playerSetRent;

	public Color color;           // The original building color
	public Collider c;            // Building's collider

	public BuildingModifier modManager; // The modmanager attached to the building
	public Tenant tenant;

	[SyncVar]
	public bool ruin;             // Ruined buildings provide no rent and cannot have occupants
	[SyncVar]
	public bool upgrade;          // Upgrade buildings don't start with maluses 
	[SyncVar]
	public int lowestSkill;       // lowest skilled residents who will live at the building. 
	[SyncVar]
	protected int baseSafety;
	[SyncVar]
	protected bool paying;
	protected const int UPKEEP_PORTION = 4;
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
			buildingName = objectNum.ToString();
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
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			objectNum++;
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
	public override void advanceMonth() {
		if (isServer) {
			if (condition > 25) {
				damageObject (1); 
			}
			if (safety < 100) {
				damageBuildingSafety(-1); // recover 1 safety each month
			}
			if (!validOwner()) { 
				notForSale = false;
			} else if (occupied) {                         // occupied, apply effects from the tenant
				tenant.clearButtons();
				damageObject(tenant.condition());
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
					damageObject (50);
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

	protected void makeRuin() {
		if (isServer) {
			tenant.leaveJob ();
			RpcMakeRuin ();
		}
	}

	/// <summary>
	/// Repair the building to 100 condition.
	/// </summary>
	public override void repair() {

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

	public override void repairByPoint(int numPoints) {
		if (isServer && !ruin) {
			condition += numPoints;
			baseCondition += numPoints;
		}
	}

	/// <summary>
	/// Sets the building on fire.
	/// </summary>
	public override void setFire() {
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
				fk.setObject (gameObject.GetComponent<Building> ());
				NetworkServer.Spawn (tmp);
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
	public override string getReadoutText(NetworkInstanceId pid) {
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
	/// Gets the cost to restore the building to 100 condition
	/// </summary>
	/// <returns>The repair cost.</returns>
	public override int getRepairCost() {
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
	public override int getPointRepairCost() {
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

	public virtual void setRent(int newRent) {
		rentOffset = newRent - baseRent;
		updateRent ();
	}

	/// <summary>
	/// City reclaims the building and pays the player the base cost
	/// </summary>
	public override void repo() {
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

	//BEGIN EARTHQUAKE STUFF

	/// <summary>
	/// Begins earthquake damage cycle
	/// </summary>
	/// <param name="prob">probability from Month Manager</param>
	/// <param name="explosion">Explosion prefab</param>
	/// <param name="delay">Delay to wait before explosion</param>
	public void earthQuakeDamage (float prob, GameObject explosion, float delay) {
		damageObject (30);
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
		damageObject (15);
		setFire ();
	}
}