//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.Networking;
//using System.Collections.Generic;
//using System.Linq;
//
//public struct NetId {
//	public NetworkInstanceId id;
//}
//	
//public class SyncListNetworkInstanceId : SyncListStruct<NetId> {
//	public void removeId(NetworkInstanceId id) {
//		this.Remove(this.Where(w => (w.id == id)).ToList()[0]);
//	}
//	public void addId(NetworkInstanceId id) {
//		NetId tmp;
//		tmp.id = id;
//		this.Add (tmp);
//	}
//	protected override void SerializeItem(NetworkWriter writer, NetworkInstanceId item)
//	{
//		writer.Write(item);
//	}
//
//	protected override NetworkInstanceId DeserializeItem(NetworkReader reader)
//	{
//		return reader.ReadNetworkId();
//	}
//
//	static public SyncListNetworkInstanceId ReadInstance(NetworkReader reader)
//	{
//		uint count = reader.ReadUInt16();
//		SyncListNetworkInstanceId result = new SyncListNetworkInstanceId();
//		for (ushort i = 0; i < count; i++)
//		{
//			result.Add(reader.ReadNetworkId());
//		}
//		return result;
//	}
//
//	static public void WriteInstance(NetworkWriter writer, SyncListNetworkInstanceId items)
//	{
//		writer.Write((ushort)items.Count);
//		foreach (var item in items)
//		{
//			writer.Write(item);
//		}
//	}
//}