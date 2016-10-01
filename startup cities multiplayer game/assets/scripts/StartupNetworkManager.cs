using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking.NetworkSystem;

public class StartupNetworkManager : NetworkManager {


	public override void OnServerConnect(NetworkConnection conn) {
		Object[] prefabs = Resources.LoadAll ("ConstructableBuildings");
		foreach (Object g in prefabs) {
			ClientScene.RegisterPrefab ((GameObject)g);
		}
	}

	// called when connected to a server
	public override void OnClientConnect(NetworkConnection conn)
	{

		Object[] prefabs = Resources.LoadAll ("ConstructableBuildings");
		foreach (Object g in prefabs) {
			ClientScene.RegisterPrefab ((GameObject)g);
		}
		//Debug.LogError ("Client Connected");
		ClientScene.Ready(conn);
		ClientScene.AddPlayer(0);
	}

	// called when client disconnects
	public override void OnClientDisconnect(NetworkConnection conn)
	{
		//Debug.LogError ("Client Disconnected");
		StopClient();
		UnityEngine.SceneManagement.SceneManager.LoadScene ("MainMenu");
	}

	public override void OnClientError(NetworkConnection conn, int errorCode) {
		//Debug.LogError ("Error");
		base.OnClientError(conn, errorCode);
	}

	public override void OnClientNotReady(NetworkConnection conn) {
		//Debug.LogError ("Client Not Ready");
		base.OnClientNotReady(conn);
	}

	// called when a client is ready
	public override void OnServerReady(NetworkConnection conn)
	{
		//Debug.LogError ("Server Ready");
		NetworkServer.SetClientReady(conn);
	}

}