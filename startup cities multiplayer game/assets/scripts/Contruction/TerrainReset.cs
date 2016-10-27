using UnityEngine;
using System.Collections;

public class TerrainReset : MonoBehaviour {

	public float[,] originalHeights;
	Terrain terrain;
	// Use this for initialization
	void Start () {
		terrain = GetComponent<Terrain> ();
		originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
	}

	/// <summary>
	/// When the terrain is destroyed, reset the values
	/// </summary>
	void OnDestroy()
	{
		terrain.terrainData.SetHeights(0, 0, originalHeights);
	}
}
