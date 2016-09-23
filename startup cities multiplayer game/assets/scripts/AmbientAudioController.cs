using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AmbientAudioController : MonoBehaviour {
	AudioSource audioS;
	List<AudioClip> clips = new List<AudioClip>();
	// Use this for initialization
	void Start () {
		audioS = GetComponent<AudioSource>();
		SetAmbientAudio ();
	}


	/// <summary>
	/// Sets the ambient audio for any GameObject. For buildings, audioclips should be put into a folder
	/// with the name of the BUILDING TYPE. This is so that similar buildings can load from the same folder.
	/// For objects that are NOT buildings, folder should be named using the gameObject.name variable.
	/// </summary>
	/// <param name="value">Value.</param>
	public void SetAmbientAudio () {
		string objectName = gameObject.name;
		if (gameObject.GetComponent<Building> () == null) {
			//For non-Buildings, uses the gameObject name to find relevant audioclips in a folder with the gameObjects name
			objectName = objectName.Substring (0, objectName.Length - 7); //Removes (Clone) from objectname;
		} else { //This is a building -- use the building-type instead
			objectName = gameObject.GetComponent<Building> ().typeName;
		}
		Object[] clipFiles = Resources.LoadAll ("Sounds/" + objectName); //Uses objectname string set above to load audioclips
		if (clipFiles.Length != 0) {
			foreach (Object c in clipFiles) {
				clips.Add (c as AudioClip);
			}
			int clipIndex = Random.Range (0, clips.Count - 1);
			if (Random.Range (0, 10) <= 5) { //40% to not play sound at all
				audioS.clip = clips [clipIndex];
				audioS.Play ();
			}
		} else {
			//Debug.Log (objectName + " still needs an ambient audio file to play here!");
		}
	}

}
