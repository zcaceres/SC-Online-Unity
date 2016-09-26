using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Resident : NetworkBehaviour {
	const int LEASE_MONTHS = 12;
	const int VISIBLE_TRAITS = 3;
	const int JOBLESS_LIMIT = 3;  // 3
	const int HOMELESS_LIMIT = 6; // set to 6 most of the time
	const int LOW_SKILL = 0;
	const int MED_SKILL = 1;
	const int HIGH_SKILL = 2;
	readonly int[] spendingBudget = { 100, 500, 1000 }; // amount of $ for spending each day by skill level

	public class Trait {
		static int traitNum = 0;

		public int id;
		public int condition;
		public string name;
		public string description;
		public float payChance;
		public float damageChance;	
		public float crimeChance;

		public Trait(string n, string desc, int cond, float pay, float dam, float crime) {
			id = traitNum;
			traitNum++;
			name = n;
			description = desc;
			condition = cond;
			payChance = pay;
			damageChance = dam;
			crimeChance = crime;
		}

		public void monthlyEffect() {
		}

	}

	[SyncVar]
	public string residentName;
	[SyncVar]
	public bool criminal;
	[SyncVar]
	public bool lowlife;

	[SyncVar]
	public int portrait;
	[SyncVar]
	public int skill;                   // skill level: 0 low, 1 mid, 2 high
	[SyncVar(hook = "setResidence")]
	public NetworkInstanceId residence; // netid of the place they live
	[SyncVar(hook = "setJob")]
	public NetworkInstanceId job;       // netid of the place they work

	// references to the actual buildings the netIds identify, set in the syncvar hooks
	public Building jobBuilding;
	public Building residenceBuilding;

	public SyncListInt traits = new SyncListInt();
	public SyncListInt bossTraits = new SyncListInt();
	public SyncListInt factoryBossTraits = new SyncListInt();

	protected static ResidentManager rm;
	protected static System.Random rng = new System.Random ();
	protected static List<Trait> managerTraits = new List<Trait>();
	protected static List<Trait> residentTraits = new List<Trait>();
	protected static List<Trait> factoryTraits = new List<Trait> ();

	protected static List<Building> lowHomes = new List<Building>();  // list of homes rated for low skill people
	protected static List<Building> medHomes = new List<Building>();  // list of homes rated for med skill people
	protected static List<Building> highHomes = new List<Building>(); // list of homes rated for high skill people

	protected static int[] rentLimits = { 200, 600, 1800 };
	protected Tenant applyingAt;
	protected int months;
	protected int monthsHomeless;
	protected int monthsJobless;
	protected int oldRent;
	[SyncVar]
	public int spendingMoney;

	// Use this for initialization
	void Start () {
		if (managerTraits.Count == 0) {
			initializeManagerTraits ();
		}
		if (residentTraits.Count == 0) {
			initializeTraits ();
		}
		if (factoryTraits.Count == 0) {
			initializeFactoryTraits ();
		}

		if (rm == null) {
			rm = FindObjectOfType<ResidentManager> ();
		}
		jobBuilding = getBuilding (job);
		residenceBuilding = getBuilding (residence);
		monthsHomeless = 0;
		monthsJobless = 0;
		if (isServer) {
			if (lowlife) { // lowLife is set by the ResidentManager when it spawns new residents
				generateLowlife ();
			} else {
				generatePerson ();
			}
			spendingMoney = spendingBudget[skill];
		}
		advanceMonth ();
	}

	/// <summary>
	/// Returns the data associated with a person as a string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="p">The person.</param>
	public virtual string personToString() {
		string s;
 
		if (residenceBuilding == null) {
			if (applyingAt == null) {
				s = residentToString ();
			} else {
				Building b = applyingAt.gameObject.GetComponent<Building> ();
				if (b is Factory) {
					s = factoryBossToString ();
				} else if (b is Restaurant || b is CheapRestaurant) {
					s = rBossToString ();
				} else {
					s = residentToString ();
				}
			}
		} else if (residenceBuilding is Restaurant || residenceBuilding is CheapRestaurant) {
			s = rBossToString ();
		} else if (residenceBuilding is Factory) {
			s = factoryBossToString ();
		} else {
			s = residentToString ();
		}
		return s;
	}

	public virtual string residentToString() {
		string s;
		//s = "\n" + p.name + "\n" + traits.ElementAt (p.traits [0]).name + "\n" + traits.ElementAt (p.traits [1]).name + "\n" + traits.ElementAt (p.traits [2]).name;
		s = residentName + "\nSkill Level: " + skill + "\n";

		for (int i = 0; i < traits.Count; i++) {
			if (i < VISIBLE_TRAITS) {
				s += residentTraits.ElementAt (traits [i]).name + "\n";
			} else {
				s += "???\n";
			}
		}

		if (jobBuilding == null) {
			s += "\nUnemployed";
		} else {
			s += "\nWorks at " + jobBuilding.buildingName;
		}
		if (residenceBuilding == null) {
			s += "\nHomeless";
		} else {
			s += "\nLives at " + residenceBuilding.buildingName;
		}
		s += "\nSpending Money: $" + spendingMoney;
		return s;
	}
	/// <summary>
	/// Returns the data associated with a person as a string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="p">The person.</param>
	public virtual string rBossToString() {
		string s;
		//s = "\n" + p.name + "\n" + traits.ElementAt (p.traits [0]).name + "\n" + traits.ElementAt (p.traits [1]).name + "\n" + traits.ElementAt (p.traits [2]).name;
		s = residentName + "\nSkill Level: " + skill + "\n";

		for (int i = 0; i < bossTraits.Count; i++) {
			if (i < VISIBLE_TRAITS) {
				s += managerTraits.ElementAt (bossTraits [i]).name + "\n";
			} else {
				s += "???\n";
			}
		}

		if (jobBuilding == null) {
			s += "\nUnemployed";
		} else {
			s += "\nWorks at " + jobBuilding.buildingName;
		}
		if (residenceBuilding == null) {
			s += "\nHomeless";
		} else {
			s += "\nLives at " + residenceBuilding.buildingName;
		}
		return s;
	}

	/// <summary>
	/// Returns the data associated with a person as a string.
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="p">The person.</param>
	public virtual string factoryBossToString() {
		string s;
		s = residentName + "\nSkill Level: " + skill + "\n";

		for (int i = 0; i < factoryBossTraits.Count; i++) {
			if (i < VISIBLE_TRAITS) {
				s += factoryTraits.ElementAt (factoryBossTraits [i]).name + "\n";
			} else {
				s += "???\n";
			}
		}

		if (jobBuilding == null) {
			s += "\nUnemployed";
		} else {
			s += "\nWorks at " + jobBuilding.buildingName;
		}
		if (residenceBuilding == null) {
			s += "\nHomeless";
		} else {
			s += "\nLives at " + residenceBuilding.buildingName;
		}
		return s;
	}

	public virtual void advanceMonth() {
		if (isServer) {
			if (!isValidNetId (job)) {
				monthsJobless++;
				if (monthsJobless > JOBLESS_LIMIT && !lowlife) { // 3 months of unemployment will make them leave the city, but not if they're a lowlife
					leaveCity ();
					rm.flagLeavingJobless ();
				} else {
					findJob ();
				}
			} else {
				spendingMoney = spendingBudget[skill];
				monthsJobless = 0;
			}
			if (!isValidNetId (residence)) {
				monthsHomeless++;
				months = 0;
				if (monthsHomeless > HOMELESS_LIMIT) {
					leaveCity ();
					rm.flagLeavingHomeless ();
				} else {
					findResidence ();
				}
			} else {
				monthsHomeless = 0;
			}

		}
	}

	protected virtual void findJob() {
		Business[] businesses = FindObjectsOfType<Business> ();
		List <Business> jobs = new List<Business> ();
		jobs = businesses.Where(b => (b.occupied && !b.ruin && (b.workers.Count < b.neededWorkers) && (b.skillLevel == skill))).ToList<Business>();
		if ((jobs != null) && (jobs.Count > 0)) {
			Business tmpJob = jobs [(int)Random.Range (0, jobs.Count)];
			job = (tmpJob.netId);
			tmpJob.addWorker (this.netId);
		}
	}

	protected virtual void findResidence() { 
		if ((skill == LOW_SKILL) && (lowHomes.Count > 0)) {
			foreach (Building b in lowHomes) {
				if (b.tenant.availableTenants.Count < 3 && ((Random.value < b.safety) || lowlife)) {
					b.tenant.availableTenants.addId (this.netId);
					applyingAt = b.tenant;
					break;
				}
			}
		} else if ((skill == MED_SKILL) && (medHomes.Count > 0)) {
			foreach (Building b in medHomes) {
				if (b.tenant.availableTenants.Count < 3 && ((Random.value < b.safety) || lowlife)) {
					b.tenant.availableTenants.addId (this.netId);
					applyingAt = b.tenant;
					break;
				}
			}
		} else if ((skill == HIGH_SKILL) && (highHomes.Count > 0)) {
			foreach (Building b in highHomes) {
				if (b.tenant.availableTenants.Count < 3 && ((Random.value < b.safety) || lowlife)) {
					b.tenant.availableTenants.addId (this.netId);
					applyingAt = b.tenant;
					break;
				}
			}
		}
	}

	public virtual void applyEffects() {
		if (residenceBuilding is Restaurant) {
			applyRestaurantEffects ();
		} else if (residenceBuilding is Factory) {
			applyFactoryEffects();
		} else {
			applyResidentialEffects ();
		}
	}
	/// <summary>
	/// Applies effects the tenant triggers on the building
	/// (bad modifiers, fires, etc)
	/// </summary>
	public virtual void applyResidentialEffects() {
		float damChance = 0;
		//float criminalChance = 0;
		bool fireRisk = false;
		if (residenceBuilding == null) {
			return;
		}
		BuildingModifier mod = residenceBuilding.modManager;
		months++;
		if (!leaveResidence ()) { // do the following only if the tenant is not going to leave the building this turn
			foreach (int t in traits) {
				if (t == 12 || t == 15) { //smoker
					fireRisk = true;
				}
				damChance += residentTraits.ElementAt (t).damageChance;
				//criminalChance += residentTraits.ElementAt (t).crimeChance;
			}

			//if (criminal) {
			//	lowerSafety (2);
			//}
			//if ((Random.value < criminalChance) && !criminal) {
			//	criminal = true;
			//	lowerSafety (30);
			//	residenceBuilding.RpcMessageOwner ("A crime has been commited in the vicinity of " + residenceBuilding.buildingName + "!");
			//}
			if (Random.value < damChance) {
				if (fireRisk) {
					residenceBuilding.setFire ();
					residenceBuilding.RpcMessageOwner (residentName + " has accidentally set " + residenceBuilding.buildingName + " on fire!");
				} else {
					int malus = (int)Random.Range (0, 6);

					switch (malus) {
					case 0:
						mod.addMalusUnique ("Clogged toilet", .9f, 1, 1, 200, 8);
						residenceBuilding.RpcMessageOwner (residentName + " has clogged the toilet of " + residenceBuilding.buildingName + "!");
						break;
					case 1:
						mod.addMalusUnique ("Hole in wall", .9f, 1, .8f, 300, 4);
						residenceBuilding.RpcMessageOwner (residentName + " has punched a hole in the wall of " + residenceBuilding.buildingName + "!");
						break;
					case 2:
						mod.addMalusUnique ("Broken window", .8f, 1, .8f, 400, 42);
						residenceBuilding.RpcMessageOwner (residentName + " has accidentally broken a window at " + residenceBuilding.buildingName + "!");
						break;
					case 3:
						mod.addMalusUnique ("Cockroach infestation", 1, .8f, 1, 200, 1);
						residenceBuilding.RpcMessageOwner ("Roaches have infested " + residenceBuilding.buildingName + "!");
						break;
					case 4:
						mod.addMalusUnique ("Bedbugs", 1, .8f, 1, 200, 1);
						residenceBuilding.RpcMessageOwner ("Bedbugs have infested " + residenceBuilding.buildingName + "!");
						break;
					case 5:
						mod.addMalusUnique ("Rats", 1, .8f, 1, 300, 1);
						residenceBuilding.RpcMessageOwner ("Rats have infested " + residenceBuilding.buildingName + "!");
						break;
					default:
						mod.addMalusUnique ("Clogged toilet", .9f, 1, 1, 100, 8);
						residenceBuilding.RpcMessageOwner (residentName + " has clogged the toilet of " + residenceBuilding.buildingName + "!");
						break;
					}
				}
			}

			if ((jobBuilding != null) && !jobBuilding.occupied) {
				residenceBuilding.RpcMessageOwner (residentName + " has lost their job at " + jobBuilding.buildingName + ".");
				leaveJob ();
			}
		} else {
			residenceBuilding.tenant.evict ();
		}
	}

	public virtual void applyRestaurantEffects() {
		float damChance = 0;
		//float criminalChance = 0;
		bool fireRisk = false;

		if (!leaveResidence ()) { // do the following only if the tenant is not going to leave the building this turn
			foreach (int t in bossTraits) {
				if (t == 9) { //reckless, fire risk
					fireRisk = true;
				} else if (t == 5) { // entrepreneur, random bonus
					if (Random.value < .10f) {
						int bonus = (int)Random.Range (0, residenceBuilding.rent);
						residenceBuilding.messageOwner (residentName + "'s entrepreneurial ways have paid off! They made an extra $" + bonus + " for you.");
						residenceBuilding.giveOwnerMoney (bonus);
					}
				}
				damChance += managerTraits.ElementAt (t).damageChance;
				//criminalChance += managerTraits.ElementAt (t).crimeChance;
			}
		
			if (Random.value < damChance) {
				if (fireRisk) {
					residenceBuilding.setFire ();
					residenceBuilding.messageOwner (residentName + " has accidentally set " + residenceBuilding.buildingName + " on fire!");
				} else {
					int malus = (int)Random.Range (0, 4);

					switch (malus) {
					case 0:
						residenceBuilding.modManager.addMalusUnique ("Broken freezer", .9f, 1, 1, 800, 5);
						residenceBuilding.messageOwner ("The freezer has broken down at " + residenceBuilding.buildingName + "!");
						break;
					case 1:
						residenceBuilding.modManager.addMalusUnique ("Broken oven", .9f, 1, 1, 150, 3);
						residenceBuilding.messageOwner ("The oven has broken down at " + residenceBuilding.buildingName + "!");
						break;
					case 2:
						residenceBuilding.modManager.addMalusUnique ("Plumbing issues", .8f, 1, 1, 200, 8);
						residenceBuilding.messageOwner (residenceBuilding.buildingName + " is experiencing plumbing issues.");
						break;
					case 3:
						residenceBuilding.modManager.addMalusUnique ("Cockroach infestation", 1, .8f, 1, 200, 1);
						residenceBuilding.messageOwner ("Roaches have infested " + residenceBuilding.buildingName + "!");
						break;
					default:
						residenceBuilding.modManager.addMalusUnique ("Broken freezer", .9f, 1, 1, 800, 5);
						residenceBuilding.messageOwner ("The freezer has broken down at " + residenceBuilding.buildingName + "!");
						break;
					}
				}
			}
		}
	}

	public virtual void applyFactoryEffects() {
		float damChance = 0;
		//float criminalChance = 0;
		bool fireRisk = false;

		if (!leaveResidence ()) { // do the following only if the tenant is not going to leave the building this turn
			foreach (int t in bossTraits) {
				if (t == 2) { // experimenter, random bonus
					if (Random.value < .10f) {
						int bonus = (int)Random.Range (0, residenceBuilding.rent);
						residenceBuilding.messageOwner (residentName + "'s experimental management has paid off! They made an extra $" + bonus + " for you.");
						residenceBuilding.giveOwnerMoney (bonus);
					}
				} else if (t == 5) { // unionizer
					if (Random.value < .05f) {
						residenceBuilding.modManager.addMalusUnique ("Strike!", 0, 1, 1, 25000, 9);
						residenceBuilding.messageOwner ("The workers are striking at " + residenceBuilding.buildingName + "!");
					}
				} else if (t == 8) { // luddite
					if (Random.value < .01f) {
						residenceBuilding.modManager.addMalusUnique ("Outdated Machinery", .7f, 1, 1, 9000, 10);
					}
				}
				damChance += managerTraits.ElementAt (t).damageChance;
			}

			if (Random.value < damChance) {
				if (fireRisk) {
					residenceBuilding.setFire ();
					residenceBuilding.messageOwner (residentName + " has accidentally set " + residenceBuilding.buildingName + " on fire!");
				} else {
					int malus = (int)Random.Range (0, 4);

					switch (malus) {
					case 0:
						residenceBuilding.modManager.addMalusUnique ("Damaged Machinery", .7f, 1, 1, 8000, 5);
						residenceBuilding.messageOwner ("Factory machinery has been damaged at " + residenceBuilding.buildingName + "!");
						break;
					case 1:
						residenceBuilding.modManager.addMalusUnique ("Industrial waste", .8f, 1, 1, 6000, 3);
						residenceBuilding.messageOwner ("Industrial waste is building up at " + residenceBuilding.buildingName + "!");
						break;
					case 2:
						residenceBuilding.modManager.addMalusUnique ("Radioactive waste", .5f, 1, 1, 12000, 1);
						residenceBuilding.messageOwner ("Dangerous radioactive waste is building up at " + residenceBuilding.buildingName);
						break;
					case 3:
						residenceBuilding.modManager.addMalusUnique ("Broken Machinery", .5f, 1, 1, 16000, 5);
						residenceBuilding.messageOwner ("Factory machinery has been broken at " + residenceBuilding.buildingName + "!");
						break;
					default:
						residenceBuilding.modManager.addMalusUnique ("Broken freezer", .9f, 1, 1, 800, 5);
						residenceBuilding.messageOwner ("The freezer has broken down at " + residenceBuilding.buildingName + "!");
						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Returns the damage caused by the tenant each month to the building.
	/// Negative if the tenant repairs the building, ie if they're a handyman
	/// </summary>
	public int condition() {
		int damage = 0;

		foreach (int i in traits) {
			damage -= residentTraits.ElementAt (i).condition;
		}

		return damage;
	}

	/// <summary>
	/// Returns true if the tenant will pay this month,
	/// false otherwise.
	/// </summary>
	public bool willPay() {
		float payChance = 1;
		bool paying = true;
		foreach (int i in traits) {
			payChance += residentTraits.ElementAt (i).payChance;
		}
		if (residenceBuilding != null) {
			float priceChange = ((float)residenceBuilding.playerSetRent / oldRent) - 1f;
			payChance += priceChange;
		}
		if (Random.value > payChance) {
			paying = false;
		}

		return paying;
	}

	public void leaveJob() {
		if (isServer) {
			if (isValidNetId (job)) {
				jobBuilding.GetComponent<Business> ().removeWorker (this.netId);
				job = NetworkInstanceId.Invalid;
				jobBuilding = null;
			} else {
				Debug.Log ("invalid job");
			}
		} else {
			Debug.LogError ("Error: client tried to call leaveJob");
		}
	}

	public bool employed() {
		bool b = false;
		if (jobBuilding != null) {
			b = true;
		} 
		return b;
	}

	public virtual void sortBuildings() {
		lowHomes.Clear ();
		medHomes.Clear ();
		highHomes.Clear ();

		int[] nonResidential = { 15, 17, 18, 21, 25 };
		List<Building> buildings = FindObjectsOfType<Building> ().ToList();
		buildings = buildings.Where (b => (!b.ruin && !b.fire && !b.occupied && (b.validOwner() || b.validCompany())
			&& !nonResidential.Contains(b.type))).ToList<Building>();
		lowHomes = buildings.Where (b => ( (b.rent <= rentLimits [LOW_SKILL]) || (b is Business)) ).ToList ();
		medHomes = buildings.Where (b => (( (b.rent <= rentLimits [MED_SKILL]) || (b is Business) ) && (b.lowestSkill >= MED_SKILL))).ToList ();
		highHomes = buildings.Where (b => (( (b.rent <= rentLimits [HIGH_SKILL]) || (b is Business) ) && (b.lowestSkill >= HIGH_SKILL))).ToList ();

		lowHomes = lowHomes.OrderByDescending (b => (rate (b, 0))).ToList ();
		medHomes = medHomes.OrderByDescending (b => (rate (b, 1))).ToList ();
		highHomes = highHomes.OrderByDescending (b => (rate (b, 2))).ToList ();

	}

	protected virtual int rateBuilding (Building b) {
		int score = 0;
		score += rentLimits [skill] - b.rent;
		score += b.getAttractiveness ();
		return score;
	}

	protected virtual int rate(Building b, int skill) {
		int score = 0;
		int[] badHomes = { 1, 9, 19 };   // small houses, tenements, trailers
		int[] goodHomes = { 2, 10, 11 }; // nice apartment buildings
		int[] greatHomes = { 12 };       // best apartment buildings

		// Apply a score bonus for nicer building types.
		if (badHomes.Contains (b.type)) {
			score -= 50;
		} else if (goodHomes.Contains (b.type)) {
			score += 200;
		} else if (greatHomes.Contains (b.type)) {
			score += 400;
		} else if (b is Business) {
			score += 99999999; // Manager jobs get priority
		}

		// Score the rent based on how far it is from the skill level's rent cap
		score += rentLimits [skill] - b.rent;
		score += b.getAttractiveness (); // score the building based on its attractiveness
		return score;
	}

	/// <summary>
	/// Generates a person with a normal distribution of traits.
	/// </summary>
	protected virtual void generatePerson() {
		int[] array = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }; // indices of all traits
		int[] managerArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};   // indices of all restaurant manager traits
		int[] factoryArray = {0,1,2,3,4,5,6,7,8,9};                 // indices of all factory manager traits

		array = scrambleArray (array);
		managerArray = scrambleArray (managerArray);
		factoryArray = scrambleArray (factoryArray);

		string[] names = System.IO.File.ReadAllLines (@"Assets\names\residentialSmallFirst.txt");
		residentName = names [rng.Next (names.Length - 1)]; // choose random name from file for the resident

		traits.Add(array[0]); // assign traits from the scrambled array of traits
		traits.Add(array [1]);
		traits.Add(array [2]);

		bossTraits.Add (managerArray [0]); // assign manager traits
		bossTraits.Add (managerArray [1]);
		bossTraits.Add (managerArray [2]);

		factoryBossTraits.Add (factoryArray [0]);
		factoryBossTraits.Add (factoryArray [1]);
		factoryBossTraits.Add (factoryArray [2]);

		if (traits.Contains(2)) { // 2 is the elderly trait. If they have it, use the old-looking portraits
			int[] oldguys = { 4, 8, 13 };
			portrait = oldguys [(int)Random.Range (0, oldguys.Length)];
		} else { // use a young-looking portrait
			int[] youngGuys = { 0, 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19 };
			portrait = youngGuys[(int)Random.Range (0, youngGuys.Length)];
		}
	}

	/// <summary>
	/// Generates a person with exclusively negative traits.
	/// </summary>
	protected virtual void generateLowlife() {
		int[] array = {6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16}; // array of all negative traits (lowlives only use negative traits
		int[] managerArray = {6, 7, 8, 9, 10};  // array of all negative manager traits
		int[] factoryArray = {5,6,7,8,9}; // indices of all negative factory manager traits

		array = scrambleArray (array);
		managerArray = scrambleArray (managerArray);
		factoryArray = scrambleArray (factoryArray);

		string[] names = System.IO.File.ReadAllLines (@"Assets\names\residentialSmallFirst.txt");
		residentName = names [rng.Next (names.Length - 1)]; // choose random name from file for the resident

		traits.Add(array[0]); // assign traits from the scrambled array of traits
		traits.Add(array [1]);
		traits.Add(array [2]);

		bossTraits.Add (managerArray [0]); // assign manager traits
		bossTraits.Add (managerArray [1]);
		bossTraits.Add (managerArray [2]);

		factoryBossTraits.Add (factoryArray [0]);
		factoryBossTraits.Add (factoryArray [1]);
		factoryBossTraits.Add (factoryArray [2]);

		// elderly counts as a positive trait, so give a lowlife a young-looking portrait
		int[] youngGuys = { 0, 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19 };
		portrait = youngGuys[(int)Random.Range (0, youngGuys.Length)];

	}
		
	/// <summary>
	/// Lowers the safety.
	/// </summary>
	/// <param name="amount">Amount.</param>
	protected void lowerSafety(int amount) {
		Collider[] colliding = Physics.OverlapSphere(residenceBuilding.c.transform.position, 35);
		foreach (Collider hit in colliding) {
			Building b = hit.GetComponent<Building> ();

			if (b != null) {
				b.damageBuildingSafety (amount);
			}
		}
	}

	protected virtual void initializeTraits() {
		// Good traits
		Trait t;
		t = new Trait ("Handyman", "This person maintains their home diligently.", 1, 0, -.01f, 0);                                 // 0
		residentTraits.Add (t);
		t = new Trait ("Gardener", "This person enjoys keeping their lawn and garden healthy.", 1, 0, 0, 0);                        // 1
		residentTraits.Add (t);
		t = new Trait ("Elderly", "This person is older than the average tenant.", 0, .1f, 0, -1);                                  // 2
		residentTraits.Add (t);
		t = new Trait ("Family Guy", "This person heads a household with children.", 0, 0, 0, -.5f);                                // 3
		residentTraits.Add (t);
		t = new Trait ("Home Improver", "This person puts significant effort into keeping their house in great shape.", 2, 0, 0, 0);// 4 
		residentTraits.Add (t);
		t = new Trait ("Ample Savings", "This person has a lot of money set aside and it unlikely to miss their bills.", 0, .2f, 0, 0); // 5
		residentTraits.Add (t);

		// Bad traits
		t = new Trait ("Recently incarcerated", "This person recently completed a stint at a local correctional institution.", 0, 0, 0, .05f);  // 6
		residentTraits.Add (t);
		t = new Trait ("Irregular Employment", "This person seems to have a hard time holding down a job.", 0, -.2f, 0, .001f);     // 7 
		residentTraits.Add (t);
		t = new Trait ("Bad Credit", "This person has a history of missing their bill payments.", 0, -.1f, 0, .001f);               // 8 
		residentTraits.Add (t);
		t = new Trait ("Drunk", "This person enjoys consuming adult beverages more than most.", 0, 0, .01f, .005f);                 // 9 
		residentTraits.Add (t);
		t = new Trait ("Cat Lady", "This person's feline companions may cause damage to the rental property.", -1, 0, 0, 0);        // 10
		residentTraits.Add (t);
		t = new Trait ("Slob", "This person does not take care of themself or their property.", -1, 0, .05f, 0);                    // 11
		residentTraits.Add (t);
		t = new Trait ("Smoker", "This person likes to smoke cigarettes.", 0, 0, .0001f, 0);                                        // 12
		residentTraits.Add (t);

		// Lowlife-only super bad traits
		t = new Trait ("Hoarder", "This person refuses to throw anything away, even garbage.", -4, 0, 0, 0);                        // 13
		residentTraits.Add (t);
		t = new Trait ("Squatter", "This person does not intend to pay rent regularly.", 0, -.5f, 0, 0);                            // 14
		residentTraits.Add (t);
		t = new Trait ("Pyromaniac", "This person likes fire.", 0, 0, .1f, 0);                                                      // 15
		residentTraits.Add (t);
		t = new Trait ("Deadbeat Diety", "This person is just bad.", -5, -.5f, .2f, 0);                                             // 16
		residentTraits.Add (t);
	}

	protected virtual void initializeManagerTraits() {
		// Good traits
		Trait t;
		t = new Trait ("Delivery King", "This manager brings in extra money through delivery jobs.", 0, 0, 0, 0);    // 0 
		managerTraits.Add (t);
		t = new Trait ("Gourmet", "This manager brings in extra rent money with their great food.", 0, 0, 0, 0);     // 1
		managerTraits.Add (t);
		t = new Trait ("Health Inspector", "This manager keeps the kitchen tidy at all times.", 1, 0, 0, 0);         // 2
		managerTraits.Add (t);
		t = new Trait ("Natural Leader", "This manager does a great job managing their staff", 0, .3f, 0, 0);        // 3
		managerTraits.Add (t);
		t = new Trait ("Maintenance Pro", "This manager knows how to repair restaurant equipment.", 2, 0, 0, 0);     // 4
		managerTraits.Add (t);
		t = new Trait ("Entrepreneurial", "This manager takes risks to bring in more business.", 0, 0, 0, 0);        // 5
		managerTraits.Add(t);

		// Bad Traits
		t = new Trait ("Inept Accountant", "This manager does not manage their money well.", 0, -.2f, 0, 0);         // 6
		managerTraits.Add (t);
		t = new Trait ("Lazy", "This manager doesn't like to work hard.", 0, 0, .01f, 0);                            // 7
		managerTraits.Add (t);
		t = new Trait ("Incompetent", "This manager often makes stupid mistakes.", 0, 0, .05f, 0);                   // 8
		managerTraits.Add (t);
		t = new Trait ("Reckless", "This manager ignores basic safety procedures.", 0, 0, .001f, 0);                 // 9
		managerTraits.Add (t);
		t = new Trait ("Shady Operator", "This manager employs unconventional business techniques.", 0, 0, 0, .001f);// 10
		managerTraits.Add (t);


	}

	protected virtual void initializeFactoryTraits() {
		// Good traits
		Trait t;
		t = new Trait ("Efficient", "This manager is unusually productive.", 0, .3f, 0, 0);                     // 0 
		factoryTraits.Add(t);
		t = new Trait ("Engineer", "This manager keeps the factory's machinery in great condition.", 2,0,0,0);  // 1 
		factoryTraits.Add(t);
		t = new Trait ("Experimenter", "This manager is always trying new things.", 0, 0, 0, 0);                // 2 (used like Entrepreneurial)
		factoryTraits.Add(t);
		t = new Trait ("OSHA Compliant", "This manager values safety on the job.", 1, 0, 0, 0);                 // 3
		factoryTraits.Add(t);
		t = new Trait ("Organized", "This manager is well-organized.", 1, 0, 0, 0);                             // 4
		factoryTraits.Add(t);

		// Bad
		t = new Trait ("Union Organizer", "This manager is involved with organized labor movements.", 0, 0, 0, 0); // 5 causes special modifier 
		factoryTraits.Add(t);
		t = new Trait ("Slave Driver", "This manager pushes their employees too hard.", -1, 0, .01f, 0);           // 6 
		factoryTraits.Add(t);
		t = new Trait ("Reckless", "This manager take unecessary risks.", 0, 0, .05f, 0);                          // 7
		factoryTraits.Add (t);
		t = new Trait ("Luddite", "This manager is slow to adapt to new technology.", 0, 0, .01f, 0);              // 8
		factoryTraits.Add(t);
		t = new Trait ("Stingy", "This manager avoids spending money on important repair work.", -2, 0, .01f, 0);   // 9
		factoryTraits.Add(t);

	}

	protected virtual bool leaveResidence() {
		bool b = false;
		if (!(residenceBuilding is Business) && (months >= LEASE_MONTHS)) {
			float priceChange = ((float)residenceBuilding.playerSetRent / oldRent) - 1f;
			if ((priceChange > 0.01f) && (Random.value < priceChange)) {
				b = true;
				residenceBuilding.RpcMessageOwner(residentName + " is moving out of " + residenceBuilding.buildingName +" due to a rent increase.");
			} else if ((Random.value > (residenceBuilding.safety / 100f)) && !lowlife) { // neighborhood is unsafe and they're not a lowlife
				if (Random.value < .20f) { // 20% chance of leaving
					rm.spawnResident (0, true);
					residenceBuilding.RpcMessageOwner(residentName + " is moving out of " + residenceBuilding.buildingName +" due to crime!");
					b = true;
				}
			}
		}
		return b;
	}

	private int[] scrambleArray(int[] array) {
		int n;
		n = array.Length;
		while (n > 1) { // scrambles the manager trait array
			int k = rng.Next(n--);
			int temp = array[n];
			array[n] = array[k];
			array[k] = temp;
		}
		return array;
	}

	private GameObject getLocalInstance(NetworkInstanceId id) {
		GameObject g;
		if (isClient) {
			g = ClientScene.FindLocalObject (id);
		} else {
			g = NetworkServer.FindLocalObject (id); 
		}
		return g;
	}

	/// <summary>
	/// utility function to get the building referenced by the netid
	/// </summary>
	/// <returns>The building.</returns>
	/// <param name="id">net id.</param>
	private Building getBuilding(NetworkInstanceId id) {
		if (isValidNetId (id)) {
			GameObject tmp = getLocalInstance (id);
			Building b = tmp.GetComponent<Building> ();
			return b;
		} else {
			return null;
		}
	}

	/// <summary>
	/// hook used when the job is set
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	private void setJob(NetworkInstanceId netId) {
		job = netId;
		if (netId.IsEmpty () || netId == NetworkInstanceId.Invalid) {
			jobBuilding = null;
		} else {
			jobBuilding = getBuilding (netId);
		}
	}

	/// <summary>
	/// hook used when the residence is set
	/// </summary>
	/// <param name="netId">Net identifier.</param>
	public void setResidence(NetworkInstanceId netId) {
		residence = netId;
		if (netId.IsEmpty () || netId == NetworkInstanceId.Invalid) {
			residenceBuilding = null;
		} else {
			residenceBuilding = getBuilding (netId);
			oldRent = residenceBuilding.playerSetRent;
		}
	}

	public void leaveHome() {
		residence = NetworkInstanceId.Invalid;
		residenceBuilding = null;
	}

	private bool isValidNetId(NetworkInstanceId netId) {
		bool b = false;
		if (!netId.IsEmpty () && netId != NetworkInstanceId.Invalid) {
			b = true;
		}
		return b;
	}

	private void leaveCity() {
		if (isValidNetId (job)) {
			getLocalInstance (job).GetComponent<Business> ().removeWorker (this.netId);
		}
		if (isValidNetId (residence)) {
			getLocalInstance (residence).GetComponent<Building> ().tenant.evict ();
		}
		Destroy (this.gameObject);
	}
}