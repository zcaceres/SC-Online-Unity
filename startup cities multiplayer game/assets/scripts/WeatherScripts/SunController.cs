using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SunController : NetworkBehaviour {
	public Renderer[] cityLights;

	Material sky;
	Light sunLight;

	public Gradient nightDayColor;

	public float maxIntensity = 3f;
	public float minIntensity = .6f;
	public float minPoint = -0.2f;

	public float maxAmbient = 1f;
	public float minAmbient = .4f;
	public float minAmbientPoint = -0.2f;


	public Gradient nightDayFogColor;
	public AnimationCurve fogDensityCurve;
	public float fogScale = 1f;

	public float dayAtmosphereThickness = 0.4f;
	public float nightAtmosphereThickness = 0.87f;

	public Vector3 dayRotateSpeed;
	public Vector3 nightRotateSpeed;

	public Transform stars;

	//public float skySpeed;
	//private MonthManager mm;
	bool lightson;

	Light mainLight;
	Material skyMat;

	void Start ()
	{
		skyMat = RenderSettings.skybox;
		mainLight = GetComponent<Light> ();
		if (isServer) {
			ModulateStarsDown (true);
		}
	}

	void Update ()
	{
		stars.transform.rotation = transform.rotation;

		float tRange = 1 - minPoint;
		float dot = Mathf.Clamp01 ((Vector3.Dot (mainLight.transform.forward, Vector3.down) - minPoint) / tRange);
		float i = ((maxIntensity - minIntensity) * dot) + minIntensity;

		mainLight.intensity = i;

		tRange = 1 - minAmbientPoint;
		dot = Mathf.Clamp01 ((Vector3.Dot (mainLight.transform.forward, Vector3.down) - minAmbientPoint) / tRange);
		i = ((maxAmbient - minAmbient) * dot) + minAmbient;
		RenderSettings.ambientIntensity = i;

		mainLight.color = nightDayColor.Evaluate (dot);
		RenderSettings.ambientLight = mainLight.color;

		RenderSettings.fogColor = nightDayFogColor.Evaluate (dot);
		RenderSettings.fogDensity = fogDensityCurve.Evaluate (dot) * fogScale;

		i = ((dayAtmosphereThickness - nightAtmosphereThickness) * dot) + nightAtmosphereThickness;
		skyMat.SetFloat ("_AtmosphereThickness", i);

		if (dot > 0)
			transform.Rotate (dayRotateSpeed * Time.deltaTime);
		else
			transform.Rotate (nightRotateSpeed * Time.deltaTime);

	
		//This should be turned off before release -- for testing only
//		if (Input.GetKeyDown (KeyCode.P)) {
//			lightson = !lightson;
//		}
//
//		if (lightson) {
//			//Debug.Log ("pressed P");
//			cityLights = FindObjectsOfType<MeshRenderer> ();
//			Color final = Color.white * Mathf.LinearToGammaSpace (5);
//			foreach (Renderer r in cityLights) { 
//				if (r.gameObject.CompareTag ("citylight")) {
//					r.material.SetColor ("_EmissionColor", final);
//					DynamicGI.SetEmissive (r, final);
//					if (r.gameObject.GetComponent<Light> () != null) {
//						r.gameObject.GetComponent<Light> ().enabled = true;
//					}
//				}
//			}
//
//		} else {
//			cityLights = FindObjectsOfType<MeshRenderer> ();
//			Color final = Color.white * Mathf.LinearToGammaSpace (0);
//			foreach (Renderer r in cityLights) { 
//				if (r.gameObject.CompareTag ("citylight")) {
//					r.material.SetColor ("_EmissionColor", final);
//					DynamicGI.SetEmissive (r, final);
//					if (r.gameObject.GetComponent<Light> () != null) {
//						r.gameObject.GetComponent<Light> ().enabled = false;
//					}
//				}
//			}
		}
//
//		//	Vector3 tvec = Camera.main.transform.position;
//		//	worldProbe.transform.position = tvec;
//
//		//	water.material.mainTextureOffset = new Vector2 (Time.time / 100, 0);
//		//	water.material.SetTextureOffset ("_DetailAlbedoMap", new Vector2 (0, Time.time / 80));
//
	//}

	/// <summary>
	/// Modulates the stars so that they don't appear during the day....
	/// </summary>
	public void ModulateStarsDown (bool modulation)
	{
		if (modulation) {
			stars.GetComponent<ParticleSystem> ().maxParticles = 10;
		} else {
			stars.GetComponent<ParticleSystem> ().maxParticles = 500;
		}			
	}

	/// <summary>
	/// Turns on lights throughout the city when night falls.
	/// Also toggles particle systems on and off where relevant.
	/// </summary>
	/// <param name="enabled">If set to <c>true</c> enabled.</param>
	public void TurnOnLights (bool enabled)
	{
		Light[] lightSources = GameObject.FindObjectsOfType<Light> ();
		StartCoroutine(LightsOn(lightSources, enabled));
		ParticlesOn (enabled);
	}

	private IEnumerator LightsOn(Light[] lightSources, bool enabled) {
		if (enabled) {
			foreach (Light l in lightSources) {
				yield return new WaitForSeconds (.0001f);
				if (!l.gameObject.CompareTag ("Sun")) {
					l.enabled = true;
				}
			}
		} else {
			foreach (Light l in lightSources) {
				if (!l.gameObject.CompareTag ("Sun")) {
					//yield return new WaitForSeconds (.0001f);
					l.enabled = false;
				}
			}
		}




	}

	/// <summary>
	/// Gets all particles that trigger with time of day and calls the method to turn them on and off.
	/// This method was used because many particle systems have sub-systems best controlled by the ParticleToggler class.
	/// </summary>
	/// <param name="enabled">If set to <c>true</c> enabled.</param>
	void ParticlesOn (bool enabled)
	{
		ParticleToggler[] particles = FindObjectsOfType<ParticleToggler> ();
		if (enabled) {
			foreach (ParticleToggler pt in particles) {
				pt.TurnOnParticles (enabled);
			}
		} else {
			foreach (ParticleToggler pt in particles) {
				pt.TurnOnParticles (enabled);
			}
		}
	}
		
}