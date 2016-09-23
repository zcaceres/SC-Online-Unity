using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ErrorMessage : MonoBehaviour {
	static float y;
	static int numMessages = 0;

	const int HEIGHT = 66;
	const int WIDTH = 194/2;
	private int oldNum;
	private float time;
	private float myY;
	private Button ok;
	// Use this for initialization
	void Start () {
		time = 0;
		oldNum = numMessages;
		y = transform.position.y;
		myY = y;
		numMessages++;
		ok = transform.Find ("ErrorMessage").transform.Find ("Button").GetComponent<Button> ();

		ok.onClick.AddListener (delegate {
			selfDestruct ();
		});
			
		transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
	}
	
	// Update is called once per frame
	void Update () {
		time += Time.deltaTime;

		if (oldNum < numMessages) {
			float tmp = y - (HEIGHT * (numMessages - oldNum));
			tmp += 66;
			if (tmp < myY) {
				myY = tmp;
				transform.position = new Vector3 (transform.position.x, myY, transform.position.z);
			}
		}

		if (time > 6) {
			selfDestruct ();
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			selfDestruct ();
		}
	}

	void selfDestruct() {
		numMessages--;
		Destroy (gameObject);
	}
}
