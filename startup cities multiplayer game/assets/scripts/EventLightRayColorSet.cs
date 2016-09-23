using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class EventLightRayColorSet : NetworkBehaviour {

	private GameObject eventParticle;
	private MeshRenderer[] mrs;
	[SyncVar]
	public Color particleColor;

	// Use this for initialization
	void Start () {
		mrs = GetComponentsInChildren<MeshRenderer> ();
		if (isServer) {
			foreach (MeshRenderer m in mrs) {
				m.material.SetColor ("_TintColor", particleColor);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		foreach (MeshRenderer m in mrs) {
			m.material.SetColor ("_TintColor", particleColor);
		}
	}
}
