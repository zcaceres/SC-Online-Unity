using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Creates and manages a loop that takes a screenshot at a set interval (delay) and then stores it
/// in a directory. NEEDS: a way to collect the .png files and turn them into an animated gif or video!
/// </summary>
public class TimeLapseCamera : MonoBehaviour {
	float delay;
	int frameIterator;

	void Start () {
		delay = 5; //Delay between frames
		frameIterator = 0;
		//Debug.LogError (Application.persistentDataPath);
		if (System.IO.File.Exists (DirectoryName())) { //Checks if directory exists in game folder
			Debug.Log ("Folder " + DirectoryName() + " exists, don't create!"); //Does nothing if it exists!
		} else {
			System.IO.Directory.CreateDirectory (DirectoryName()); //If it doesn't exist, creates a directory
			Debug.Log ("Folder " + DirectoryName() + " did NOT exist. Created folder for screenshots!");
		}
		StartCoroutine(TimeLapseDelay (delay)); //Initializes the screenshot-capture routine
	}
		


	/// <summary>
	/// Delays the timelapse and runs the screenshot loop
	/// </summary>
	/// <returns>The lapse delay.</returns>
	/// <param name="delay">Delay.</param>
	IEnumerator<WaitForSeconds> TimeLapseDelay(float delay) {
		do {
			yield return new WaitForSeconds (delay); //Yield a pause (set delay above)
			Application.CaptureScreenshot(ScreenShotName(frameIterator)); //Take a screenshot, save as PNG
			//Debug.LogError("I took a photo!");
			frameIterator += 1; //Iterate another screenshot
		} while (frameIterator <= 100); //What number is proper for a real game? Base on time??

	}


	/// <summary>
	/// Sets the file path for screenshots
	/// </summary>
	/// <returns>The shot name.</returns>
	/// <param name="frameIt">Frame it.</param>
	private string ScreenShotName(int frameIt) {
		return string.Format("{0}/test-screenshots/GameFrame-{1}.png", //Sets file format to ping and stores in a screenshot folder.
			Application.dataPath,
			frameIt);
	}

	/// <summary>
	/// Sets the directory name for storing the screenshots
	/// </summary>
	/// <returns>The name.</returns>
	private string DirectoryName () {
		return string.Format ("{0}/test-screenshots", Application.dataPath);
	}

}
