﻿using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof (Rigidbody))]
[RequireComponent(typeof (CapsuleCollider))]
public class SpectatorController : NetworkBehaviour
{
	[Serializable]
	public class MovementSettings
	{
		public float ForwardSpeed = 1.0f;   // Speed when walking forward
		public float BackwardSpeed = 1.0f;  // Speed when walking backwards
		public float StrafeSpeed = 1.0f;    // Speed when walking sideways
		public float RunMultiplier = 1.0f;   // Speed when sprinting
		public KeyCode RunKey = KeyCode.CapsLock;
		public float JumpForce = 30f;
		public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
		[HideInInspector] public float CurrentTargetSpeed = 8f;
		public Animator anim;

		#if !MOBILE_INPUT
		private bool m_Running;
		#endif

		public void UpdateDesiredTargetSpeed(Vector2 input)
		{

			if (input == Vector2.zero) return;
			if (input.x > 0 || input.x < 0)
			{
				//strafe
				CurrentTargetSpeed = StrafeSpeed;
			}
			if (input.y < 0)
			{
				//backwards
				CurrentTargetSpeed = BackwardSpeed;
			}
			if (input.y > 0)
			{
				//forwards
				//handled last as if strafing and moving forward at the same time forwards speed should take precedence
				CurrentTargetSpeed = ForwardSpeed;

			}

			#if !MOBILE_INPUT
			//				if (Input.GetKeyDown(RunKey))
			//	            {
			//		            CurrentTargetSpeed *= RunMultiplier;
			//					m_Running = !m_Running;
			//					anim.SetBool("Walk", m_Running);
			//				}
			#endif
		}

		#if !MOBILE_INPUT
		public bool Running
		{
			get { return m_Running; }
		}
		#endif
	}


	[Serializable]
	public class AdvancedSettings
	{
		public float groundCheckDistance = 0.01f;        // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
		public float stickToGroundHelperDistance = 0.5f; // stops the character
		public float slowDownRate = 20f;                 // rate at which the controller comes to a stop when there is no input
		public bool airControl;                          // can the user control the direction that is being moved in the air
		[Tooltip("set it to 0.1 or more if you get stuck in wall")]
		public float shellOffset;                        //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
	}


	public Camera cam;
	public MovementSettings movementSettings = new MovementSettings();
	public StartupMouseLook mouseLook = new StartupMouseLook();
	public AdvancedSettings advancedSettings = new AdvancedSettings();
	public Animator anim;


	private Rigidbody m_RigidBody;
	private CapsuleCollider m_Capsule;
	private float m_YRotation;
	private Vector3 m_GroundContactNormal;
	private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;
	private bool footCollision;

	//Declarations for Chat Toggle with Return key
	private GameObject PlayerUICanvas;
	private Transform ChatUI;
	private Transform ChatBG;
	private Transform InputBox;
	private Transform ChatInput;
	private Transform PlayerNameInput;
	private Transform enterPlayerNameField;
	private InputField ChatInputField;
	private EventSystem es;



	public Vector3 Velocity
	{			
		get { return m_RigidBody.velocity; }
	}

	public bool Grounded
	{
		get { return m_IsGrounded; }
	}

	public bool Jumping
	{
		get { return m_Jumping; }
	}

	public bool Running
	{
		get
		{
			#if !MOBILE_INPUT
			return movementSettings.Running;
			#else
			return false;
			#endif
		}
	}

	bool lookEnabled;
	bool movementEnabled;
	public KeyCode RunKey = KeyCode.CapsLock;
	bool isWalking = true;
	int footCollisionLayerMask = 1 << 31;
	GameObject panel;

	private void Start()
	{
		anim = GetComponent<Animator> ();
		m_RigidBody = GetComponent<Rigidbody>();
		if (!isLocalPlayer)
			return;
		m_Capsule = GetComponent<CapsuleCollider>();
		mouseLook.Init (transform, cam.transform);

		//Setting variables for Chat Toggle with Return Key
		PlayerUICanvas = GameObject.Find ("Canvas");
		ChatInput = PlayerUICanvas.transform.Find("ChatUI/Bg/Inpunt/InputField");
		ChatInputField = ChatInput.GetComponent<InputField> ();
		enterPlayerNameField = PlayerUICanvas.transform.Find ("ChatUI/EnterPlayerName");
		PlayerNameInput = PlayerUICanvas.transform.Find ("ChatUI/EnterPlayerName/InputField");
		es = GameObject.Find ("EventSystem").GetComponent<EventSystem> ();
		setMouseLookEnabled (true);
		GameObject.Find("Canvas").transform.Find ("ChatUI").GetComponent<bl_ChatUI> ().SetPlayerName ("Spectator");
	}

	public const float WALK_SPEED = .25f;


	private void Update()
	{

		Vector3 fwd = cam.transform.TransformDirection (Vector3.forward);

		if (!isLocalPlayer || ChatInputField.isFocused) {
			return;
		}

		if (Input.GetKeyDown(KeyCode.Period)) {
			lookEnabled = !lookEnabled;
			mouseLook.SetCursorLock(lookEnabled);
			mouseLook.UpdateCursorLock();
		}

		//Chat toggle with return key
		if (Input.GetKeyDown (KeyCode.Return)) {
			if (enterPlayerNameField.gameObject.activeInHierarchy) {
				es.SetSelectedGameObject (PlayerNameInput.gameObject, null);
			} else {
				es.SetSelectedGameObject (ChatInput.gameObject, null); 
			}
		}
	}

	private void FixedUpdate()
	{
		if (!isLocalPlayer)
			return;

		if (lookEnabled) {
			RotateView ();
		}
		GroundCheck();
		Vector2 input = GetInput();

		if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon))
		{
			// always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = cam.transform.forward*input.y + cam.transform.right*input.x;
			//desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

			desiredMove.x = desiredMove.x*movementSettings.CurrentTargetSpeed;
			desiredMove.z = desiredMove.z*movementSettings.CurrentTargetSpeed;
			desiredMove.y = desiredMove.y*movementSettings.CurrentTargetSpeed;
			if (m_RigidBody.velocity.sqrMagnitude <
				(movementSettings.CurrentTargetSpeed*movementSettings.CurrentTargetSpeed))
			{
				m_RigidBody.AddForce(desiredMove*SlopeMultiplier(), ForceMode.Impulse);
			}
		}


		m_RigidBody.drag = 5f;

		if (m_Jump)
		{
			m_RigidBody.drag = 0f;
			m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
			m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
			m_Jumping = true;
		}

		if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && m_RigidBody.velocity.magnitude < 1f)
		{
			m_RigidBody.Sleep();
		}

		m_Jump = false;
	}


	private float SlopeMultiplier()
	{
		float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
		return movementSettings.SlopeCurveModifier.Evaluate(angle);
	}


	private void StickToGroundHelper()
	{
		RaycastHit hitInfo;
		if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
			((m_Capsule.height/2f) - m_Capsule.radius) +
			advancedSettings.stickToGroundHelperDistance, ~0, QueryTriggerInteraction.Ignore))
		{
			if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
			{
				m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
			}
		}
	}


	private Vector2 GetInput()
	{

		Vector2 input = new Vector2
		{
			x = CrossPlatformInputManager.GetAxis("MoveX"),
			y = CrossPlatformInputManager.GetAxis("MoveZ")
		};
		movementSettings.UpdateDesiredTargetSpeed(input);
		return input;
	}


	private void RotateView()
	{
		//avoids the mouse looking if the game is effectively paused
		if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

		// get the rotation before it's changed
		float oldYRotation = transform.eulerAngles.y;

		mouseLook.LookRotation (transform, cam.transform);

		if (advancedSettings.airControl)
		{
			// Rotate the rigidbody velocity to match the new direction that the character is looking
			Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
			m_RigidBody.velocity = velRotation*m_RigidBody.velocity;
		}
	}

	/// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
	private void GroundCheck()
	{
		m_PreviouslyGrounded = m_IsGrounded;
		RaycastHit hitInfo;
		if (Physics.SphereCast(m_Capsule.bounds.center, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo, 
			((m_Capsule.height/2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, ~0, QueryTriggerInteraction.Ignore))
		{
			m_IsGrounded = true;
			m_GroundContactNormal = hitInfo.normal;
		}
		else
		{
			m_IsGrounded = false;
			m_GroundContactNormal = Vector3.up;
		}
		if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
		{
			m_Jumping = false;
		}
	}

	public void setMovementEnabled(bool b) {
		movementEnabled = b;
	}

	public void setMouseLookEnabled(bool b) {
		lookEnabled = b;
		mouseLook.SetCursorLock(b);
		mouseLook.UpdateCursorLock();
	}
}