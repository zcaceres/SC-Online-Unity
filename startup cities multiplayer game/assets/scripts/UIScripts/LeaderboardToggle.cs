using UnityEngine;
using System.Collections;

public class LeaderboardToggle : MonoBehaviour {

	public GameObject g;

	public void ToggleLeaderboard() {
		bool b = g.activeInHierarchy;
		g.SetActive (!b);
	}
}
