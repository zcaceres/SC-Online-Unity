using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Politicians are residents which can only work at city hall as the "tenant". They stay in the city during election season and leave when it ends.
/// </summary>
public class Politician : Resident {
	const int SPENDING_MONEY = 2000;

	/// <summary>
	/// Traits for politicians--focused more on stuff that effects regions and the region owner than the building they live in
	/// </summary>
	public class PoliticalTrait {
		static int traitNum = 0;

		public int id;
		public string name;
		public string description;
		public int party;             // trait associated with which party. 0 none, 1 teal, 2 grey, 3 yellow, 
		public float noLoyaltyChance; // % chance that the mayor will have no loyalty after the election
		public float embezzleChance;  // % chance that the mayor will lose tax funds for a year	
		public float copCostModifier; // multiplier to cost of police
		public float winChance;       // multiplier to election winning odds
		public float roadCostModifier;// multiplier to cost of infrastructure 
		public Color color;           // color the trait is displayed as on the election screen

		/// <summary>
		/// Initializes a new instance of the <see cref="Politician+PoliticalTrait"/> class.
		/// </summary>
		/// <param name="n">Trait Name.</param>
		/// <param name="d">Trait Description.</param>
		/// <param name="nol">No-loyalty chance.</param>
		/// <param name="emb">Embezzler chance.</param>
		/// <param name="cop">Cop cost modifier.</param>
		/// <param name="win">election win chance multiplier.</param>
		/// <param name="road">Road cost modifier.</param>
		/// <param name="c">Color.</param>
		public PoliticalTrait(string n, string d, float nol, float emb, float cop, float win, float road, Color c, int p) {
			id = traitNum;
			traitNum++;
			name = n;
			description = d;
			noLoyaltyChance = nol;
			embezzleChance = emb;
			copCostModifier = cop;
			roadCostModifier = road;
			winChance = win;
			party = p;
		}
	}

	protected static List<PoliticalTrait> politicalTraits = new List<PoliticalTrait>();
	[SyncVar]
	protected float electability;
	[SyncVar]
	protected int partyTrait;
	[SyncVar]
	protected int genericTraitOne;
	[SyncVar]
	protected int genericTraitTwo;
	[SyncVar]
	public int party;
	[SyncVar(hook = "SetFunds")]
	public int funds;
	public Player loyalty;
	public Region runningFor;
	static MonthManager mm;

	// dictionary of the donations the candidate has gotten from players
	protected Dictionary<int, int> playerFunds = new Dictionary<int, int> ();
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
		if (politicalTraits.Count == 0) {
			InitializePoliticalTraits ();
		}

		if (rm == null) {
			rm = FindObjectOfType<ResidentManager> ();
		}
		if (mm == null) {
			mm = FindObjectOfType<MonthManager> ();
		}
		jobBuilding = getBuilding (job);
		residenceBuilding = getBuilding (residence);
		monthsHomeless = 0;
		monthsJobless = 0;
		skill = -1;
		if (isServer) {
			generatePolitician();
			spendingMoney = SPENDING_MONEY;
			runningFor.candidates.addId (this.netId);
			advanceMonth ();
		}
	}

	public override void advanceMonth() {
		if (isServer) {
			if ((mm != null) && !mm.isElectionSeason && residenceBuilding == null) {
				leaveCity (); // the election is over and I didn't win
			}
		}
	}

	public override string personToString() {
		string s = "";
		s += residentName;
		if (residenceBuilding != null) {
			s += "\nMayor of " + residenceBuilding.GetComponent<Region> ().regionName;
		} else {
			s += "\nPolitician";
		}
		s +="\n" + politicalTraits [partyTrait].name + "\n" + politicalTraits [genericTraitOne].name + "\n" + politicalTraits [genericTraitTwo].name;
		return s;
	}

	public string ElectionFormatString() {
		string s = "";
		s += "Name: " + residentName;
		s += TraitToString (politicalTraits [partyTrait]);
		s += TraitToString (politicalTraits [genericTraitOne]);
		s += TraitToString (politicalTraits [genericTraitTwo]);
		return s;
	}

	/// <summary>
	/// Returns name and description of trait
	/// </summary>
	/// <returns>The string.</returns>
	/// <param name="p">trait.</param>
	public string TraitToString(PoliticalTrait p) {
		string s = "";
		s += "\n\n" + p.name + "\nDescription: " + p.description;
		return s;
	}

	/// <summary>
	/// Rate this candidate's chance at being elected. Considers their election funds, electability from traits, and
	/// also factors in a random multiplier
	/// </summary>
	public int Rate() {
		return (int)(funds * electability * Random.Range (.8f, 1.2f));
	}

	/// <summary>
	/// Adds a donation from a player to the politicians funds.
	/// </summary>
	/// <param name="amount">Amount to give.</param>
	/// <param name="giver">Player providing the donation.</param>
	public void AddFunds(int amount, Player giver) {
		funds += amount;
		if (playerFunds.ContainsKey (giver.id)) {
			playerFunds [giver.id] += amount;
		} else {
			playerFunds.Add (giver.id, amount);
		}
		giver.budget -= amount;
	}

	/// <summary>
	/// Chooses the mayor's loyalty. Finds the player who donated the most to the politician's campaign 
	/// or ignores the player if the politician has a trait which causes disloyalty
	/// </summary>
	public void ChooseLoyalty() {
		if (Random.value > GetDisloyalChance ()) {
			KeyValuePair<int, int> p = new KeyValuePair<int, int> ();
			foreach (KeyValuePair<int, int> pair in playerFunds) {
				if (pair.Value > p.Value && pair.Value > 0) {
					p = pair;
				}
			}
			Player[] players = FindObjectsOfType<Player> ();
			if (p.Value > 0) {
				foreach (Player pl in players) {
					if (pl.id == p.Key) {
						loyalty = pl;
						break;
					}
				}
			} else {
				loyalty = null;
			}
		}

	}

	/// <summary>
	/// Returns the multiplier to build cost caused by the candidate's traits
	/// </summary>
	/// <returns>The build multiplier.</returns>
	public float GetBuildMultiplier() {
		float multi = 1;
		multi *= politicalTraits[genericTraitOne].roadCostModifier * politicalTraits[genericTraitTwo].roadCostModifier * politicalTraits[partyTrait].roadCostModifier;
		return multi;
	}

	/// <summary>
	/// Gets the police multiplier.
	/// </summary>
	/// <returns>The police multiplier.</returns>
	public float GetPoliceMultiplier() {
		float multi = 1;
		multi *= politicalTraits[genericTraitOne].copCostModifier * politicalTraits[genericTraitTwo].copCostModifier * politicalTraits[partyTrait].copCostModifier;
		return multi;
	}

	public float GetEmbezzleChance() {
		float chance = 0;
		chance += politicalTraits [genericTraitOne].embezzleChance + politicalTraits [genericTraitTwo].embezzleChance + politicalTraits [partyTrait].embezzleChance;
		return chance;
	}

	/// <summary>
	/// Gets the disloyal chance (the chance that the politician will have no loyalty to their top donor.
	/// </summary>
	/// <returns>The disloyal chance.</returns>
	protected float GetDisloyalChance() {
		return (politicalTraits[partyTrait].noLoyaltyChance + politicalTraits[genericTraitOne].noLoyaltyChance + politicalTraits[genericTraitTwo].noLoyaltyChance);
	}

	/// <summary>
	/// hook that sets the funds and recalculates the candidate's current loyalty.
	/// </summary>
	/// <param name="f">F.</param>
	protected void SetFunds(int f) {
		funds = f;
		//ChooseLoyalty ();
	}

	protected void InitializePoliticalTraits() {
		PoliticalTrait p;

		// Non-Partisan traits 0-8
		p = new PoliticalTrait ("Honest", "This politician's honesty inspires voter trust. \n\t+10% electability", 0f, 0f, 1f, 1.1f, 1f, Color.white, 0);                              // 0
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Charismatic Speaker", "This politician's charisma wins over voters.\n\t+20% electability", 0f, 0f, 1f, 1.2f, 1f, Color.white, 0);                    // 1
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Independent", "This politician does not let special interests govern their decisions.\n\t+50% chance of no loyalty\n\t+30% electability", .5f, 0f, 1f, 1.3f, 1f, Color.white, 0); // 2
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Inept", "This politician's poor judgement leads to waste.\n\t+20% police cost\n\t+20% build cost", 0f, 0f, 1.2f, 1f, 1.2f, Color.white, 0);                            // 3
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Corrupt", "This politician knows it doesn't hurt to wet your beak a bit.\n\t+50% chance to embezzle taxes", 0f, .5f, 1f, 1f, 1f, Color.white, 0); // 4
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Wise Budget", "This politician is always trying to shrink the budget.\n\t-20% police cost\n\t-20% build cost", 0f, 0f, .8f, 1f, .8f, Color.white, 0);                // 5
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Stingy", "This politician is so agressive about cutting the budget that it turns off voters.\n\t-30% police cost\n\t-20% electability\n\t-30% build cost", 0f, 0f, .7f, .8f, .7f, Color.white, 0); // 6 
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Ugly", "This politician does not have the face of a leader.\n\t-30% electability", 0f, 0f, 1f, .7f, 1f, Color.white, 0);                             // 7
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Panderer", "This politician knows what to say to appeal to certain groups.\n\t+30% electability", 0f, 0f, 1f, 1.3f, 1f, Color.white, 0);             // 8
		politicalTraits.Add (p);

		// Teal Party Traits--Teal party wastes money, but they're easy to elect 9-12
		p = new PoliticalTrait ("Dynasty", "The Teal party employs candidates from well-liked political families... regardless of their leadership skills.\n\t+30% police cost\n\t+30% electability\n\t+30% build cost", 0f, 0f, 1.3f, 1.3f, 1.3f, Color.cyan, 1); // 9
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Celebrity Endorsements", "The Teal party pays public figures to appear at their events, which can cut into the budget.\n\t+20% police cost\n\t+20% electability\n\t+20% build cost", 0f, 0f, 1.2f, 1.2f, 1.2f, Color.cyan, 1); // 10
		politicalTraits.Add (p);
		p = new PoliticalTrait ("City Beautification Programs", "The Teal party likes to spend money decorating the city, a popular yet costly endeavor.\n\t+10% build cost\n\t+10% electability\n\t+10% build cost", 0f, 0f, 1.1f, 1.1f, 1.1f, Color.cyan, 1); // 11
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Social Welfare Programs", "The Teal party often invests in costly projects aimed at improving the lives of the less-fortunate.\n\t+50% police cost\n\t+50% electability\n\t+50% build cost", 0f, 0f, 1.5f, 1.5f, 1.5f, Color.cyan, 1); // 12
		politicalTraits.Add (p);

		// Grey party traits--pricey infrastructure, cheap police 13-16
		p = new PoliticalTrait ("Law and Order", "The Grey party invests in a police force at the expense of infrastructure.\n\t-30% police cost\n\t+30% build cost", 0f, 0f, .7f, 1f, 1.3f, Color.grey, 2); // 13
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Militarized Police", "The Grey party believes a well-armed police force is an effective police force.\n\t-10% police cost\n\t+10% build cost", 0f, 0f, .9f, 1f, 1.1f, Color.grey, 2); // 14
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Prison Investor", "The Grey party knows that investing in prisons is just as good as investing in schools.\n\t-20% police cost\n\t+20% build cost", 0f, 0f, .8f, 1f, 1.2f, Color.grey, 2); // 15
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Military Background", "The Grey party fields candidates with experience in the armed forces.\n\t-10% police cost\n\t+10% build cost", 0f, 0f, .9f, 1f, 1.1f, Color.grey, 2); // 16
		politicalTraits.Add (p);

		// Yellow party traits--cheap costs, low electability 17-20
		p = new PoliticalTrait ("Neo-Monarchist", "The Yellow party believes the city can lower costs by instituting an absolute monarch.\n\t-50% police cost\n\t-50% electability\n\t-40% build cost", 0f, 0f, .5f, .6f, .6f, Color.yellow, 3); // 17
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Technocrat", "The Yellow party knows that embracing technology will bring the city forward.\n\t-30% police cost\n\t-30% electability\n\t-30% build cost", 0f, 0f, .7f, .7f, .7f, Color.yellow, 3); // 18
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Wizard", "The Yellow party believes bearded sorcerers are capable of assisting in municipal governance.\n\t-20% police cost\n\t-20% electability\n\t-20% build cost", 0f, .0f, .8f, .8f, .8f, Color.yellow, 3); // 19
		politicalTraits.Add (p);
		p = new PoliticalTrait ("Nega-Mayor", "The Yellow party knows that the best way to do no harm is to do nothing.\n\t-10% police cost\n\t-10% electability\n\t-10% build cost", 0f, 0f, .9f, .9f, .9f, Color.yellow, 3); // 20
		politicalTraits.Add (p);
	}

	protected void generatePolitician() {
		// traits used by residents in homes or other jobs... not important for politicians
//		traits.Add (0);
//		traits.Add (1);
//		traits.Add (2);
//		bossTraits.Add (0);
//		bossTraits.Add (1);
//		bossTraits.Add (2);
//		factoryBossTraits.Add (0);
//		factoryBossTraits.Add (1);
//		factoryBossTraits.Add (2);

		int[] array = {0,1,2,3,4,5,6,7,8};

		// one trait is always a party trait
		if (party == 1) { //teal
			int[] tmp = {9,10,11,12};
			scrambleArray (tmp);
			partyTrait = tmp [0];
		} else if (party == 2) { // grey
			int[] tmp = {13,14,15,16};
			scrambleArray (tmp);
			partyTrait = tmp [0];
		} else { // yellow
			int[] tmp = {17,18,19,20};
			scrambleArray (tmp);
			partyTrait = tmp [0];
		}
		scrambleArray (array);

		// assign politician traits from the scrambled array
		genericTraitOne = array [0];
		genericTraitTwo = array [1];

		string[] names = System.IO.File.ReadAllLines (@"Assets\names\residentialSmallFirst.txt");
		residentName = names [rng.Next (names.Length - 1)]; // choose random name from file for the resident


		int[] p = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
		portrait = p[(int)Random.Range (0, p.Length)];

		// set the politician's electability modifier based on their traits
		electability = 1 * politicalTraits[partyTrait].winChance * politicalTraits[genericTraitOne].winChance * politicalTraits[genericTraitTwo].winChance;
	}
}
