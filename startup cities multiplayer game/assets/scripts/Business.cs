using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

public class Business : Building
{
	public struct NetId
	{
		public NetworkInstanceId id;
	}

	public class SyncListNetId : SyncListStruct<NetId>
	{
		public void addId (NetworkInstanceId id)
		{
			NetId w;
			w.id = id;
			this.Add (w);
		}

		public void removeId (NetworkInstanceId id)
		{
			this.Remove (this.Where (w => (w.id == id)).ToList () [0]);
		}
	}

	protected const int CHANNEL = 1;
	[SyncVar]
	public int neededWorkers;
	// the number of workers the business needs to operate normally
	public SyncListNetId workers = new SyncListNetId ();
	const int COST_PER_VISIT = 100;
	//the value each time a resident 'visits' the business. Subtracted from their budget.
	public int skillLevel;
	// the skill level the business requires of its workers
	private static string[] rSmallFirst = { "Generic" };
	private static string[] rSmallLast = { "Business" };
	protected int earnings;
	//accumulated money from resident's passing by the business

	void Start ()
	{
		lowestSkill = 0;
		c = GetComponent<Collider> ();
		modManager = GetComponent<BuildingModifier> ();
		tenant = GetComponent<Tenant> ();
		color = c.gameObject.GetComponent<MeshRenderer> ().materials.ElementAt (0).color;
		if (isServer) {
			skillLevel = 0;
			baseRent = 100;
			baseCondition = 100;
			baseSafety = 100;
			rent = baseRent;
			condition = baseCondition;
			safety = baseSafety;
			cost = 4500;
			baseCost = cost;
			upkeep = rent / UPKEEP_PORTION;
			officeName = "None";
			buildingName = nameGen ();
			id = objectNum;
			fire = false;
			ruin = false;
			occupied = false;
			onAuction = false;
			paying = false;
			objectNum++;
			neededWorkers = 1;
			GameObject tmp = getLocalInstance (lot);
			if (tmp != null) {
				localLot = tmp.GetComponent<Lot> ();
			} else if (localLot != null) {
				lot = localLot.netId; // the lot was set in the inspector, assign the netid
				localLot.addObject(this.netId);
			}
			GameObject tmpRegion = getLocalInstance (region);
			if (tmpRegion != null) {
				localRegion = tmpRegion.GetComponent<Region> ();
			} else if (localRegion != null) {
				region = localRegion.netId;
				localRegion.AddItem (this.netId);
			}
			typeName = buildingTypes [type];
			updateRent ();
			//updateNeighborhoodValue ();
		}
	}

	/// <summary>
	/// Gets the resident component on the object referred to by the passed networkinstanceid
	/// </summary>
	/// <returns>The resident.</returns>
	/// <param name="id">Net id.</param>
	protected Resident getResident (NetworkInstanceId id)
	{
		Resident r = getLocalInstance (id).GetComponent<Resident> ();
		return r;
	}

	public void addWorker (NetworkInstanceId id)
	{
		NetId w;
		w.id = id;
		workers.Add (w);
	}

	public void removeWorker (NetworkInstanceId id)
	{
		workers.removeId (id);
	}

	/// <summary>
	/// Gets the worker text. For base class, there are no workers, so returns empty.
	/// </summary>
	/// <returns>The worker text.</returns>
	public override string getWorkerText ()
	{
		string s = "";
		s += "Workers: " + workers.Count + "/" + neededWorkers;
		return s;
	}

	//	protected override void makeRuin() {
	//		if (isServer) {
	//			tenant.leaveJob ();
	//			List<NetId> tmp = new List<NetId> ();
	//			foreach (NetId n in workers) {
	//				tmp.Add (n);
	//			}
	//			foreach (NetId n in tmp) {
	//				GameObject obj = getLocalInstance (n.id);
	//				if (obj != null) {
	//					Resident r = obj.GetComponent<Resident> ();
	//					r.leaveJob ();
	//					Debug.Log ("Removing");
	//				}
	//			}
	//
	//			RpcMakeRuin ();
	//		}
	//	}


	public virtual void visitBusiness (Resident res)
	{
		if (res.spendingMoney >= GetCostOfVisit ()) {
			if (Random.Range (0, 100) <= condition) {
				int spent = GetCostOfVisit ();
				res.spendingMoney -= spent;
				addMoney (spent);
			}
		}
	}

	private string nameGen ()
	{
		string name;

		name = rSmallFirst [(int)Random.Range (0, rSmallFirst.Length)] + " " + rSmallLast [(int)Random.Range (0, rSmallLast.Length)];
		return name;
	}

	public virtual int GetCostOfVisit ()
	{
		return COST_PER_VISIT;
	}

	public void addMoney (int i)
	{
		earnings += i;
	}

}