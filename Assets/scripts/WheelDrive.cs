using UnityEngine;
using System;

[Serializable]
public enum DriveType
{
	RearWheelDrive,
	FrontWheelDrive,
	AllWheelDrive
}

public class WheelDrive : MonoBehaviour
{
	[Header("Steering")]

	[Tooltip("Maximum steering angle at low speed")]
	public float maxAngle = 30f;

	[Tooltip("How quickly steering moves toward the target")]
	public float steeringSpeed = 90f;

	[Tooltip("Steering strength at maximum speed")]
	[Range(0.1f, 1f)]
	public float highSpeedSteering = 0.4f;


	[Header("Engine")]

	[Tooltip("Maximum torque applied to driving wheels")]
	public float maxTorque = 250f;

	[Tooltip("Maximum vehicle speed in km/h")]
	public float maxSpeedKph = 140f;


	[Header("Brakes")]

	public float brakeTorque = 30000f;


	[Header("Wheel Setup")]

	public GameObject wheelShape;

	public DriveType driveType;


	[Header("Physics Quality")]

	public float criticalSpeed = 5f;

	// Below speed threshold
	public int stepsBelow = 5;

	// Above speed threshold
	public int stepsAbove = 10;


	private WheelCollider[] m_Wheels;
	private Rigidbody rb;

	private float horizontalInput;
	private float verticalInput;

	private bool handBrake;

	private float currentSteerAngle;


	void Start()
	{
		rb = GetComponent<Rigidbody>();

		m_Wheels = GetComponentsInChildren<WheelCollider>();

		if (m_Wheels.Length > 0)
		{
			m_Wheels[0].ConfigureVehicleSubsteps(
				criticalSpeed,
				stepsBelow,
				stepsAbove
			);
		}

		for (int i = 0; i < m_Wheels.Length; i++)
		{
			WheelCollider wheel = m_Wheels[i];

			if (wheelShape != null)
			{
				GameObject ws = Instantiate(wheelShape);

				ws.transform.parent = wheel.transform;
			}
		}
	}


	void Update()
	{
		// Read player input here
		horizontalInput = Input.GetAxis("Horizontal");

		verticalInput = Input.GetAxis("Vertical");

		handBrake = Input.GetKey(KeyCode.X);
	}


	void FixedUpdate()
	{
		float speedKph =
			rb.linearVelocity.magnitude * 3.6f;


		// ---------------------------------
		// SPEED-SENSITIVE STEERING
		// ---------------------------------

		float speedPercent =
			Mathf.Clamp01(speedKph / maxSpeedKph);


		float steeringMultiplier =
			Mathf.Lerp(
				1f,
				highSpeedSteering,
				speedPercent
			);


		float targetSteerAngle =
			horizontalInput
			* maxAngle
			* steeringMultiplier;


		// Smooth steering movement
		currentSteerAngle =
			Mathf.MoveTowards(
				currentSteerAngle,
				targetSteerAngle,
				steeringSpeed * Time.fixedDeltaTime
			);


		// ---------------------------------
		// ENGINE
		// ---------------------------------

		float torque = maxTorque * verticalInput;


		// Stop accelerating forward
		// after reaching max speed
		if (speedKph >= maxSpeedKph &&
			verticalInput > 0f)
		{
			torque = 0f;
		}


		float brake =
			handBrake ? brakeTorque : 0f;


		// ---------------------------------
		// WHEELS
		// ---------------------------------

		foreach (WheelCollider wheel in m_Wheels)
		{
			bool frontWheel =
				wheel.transform.localPosition.z > 0;

			bool rearWheel =
				wheel.transform.localPosition.z < 0;


			// Front wheels steer
			if (frontWheel)
			{
				wheel.steerAngle =
					currentSteerAngle;
			}


			// Rear hand brake
			if (rearWheel)
			{
				wheel.brakeTorque = brake;
			}


			// Rear wheel drive
			if (rearWheel &&
				driveType != DriveType.FrontWheelDrive)
			{
				wheel.motorTorque = torque;
			}


			// Front wheel drive
			if (frontWheel &&
				driveType != DriveType.RearWheelDrive)
			{
				wheel.motorTorque = torque;
			}


			UpdateWheelVisual(wheel);
		}
	}


	void UpdateWheelVisual(WheelCollider wheel)
	{
		if (wheelShape == null)
			return;


		wheel.GetWorldPose(
			out Vector3 position,
			out Quaternion rotation
		);


		Transform shapeTransform =
			wheel.transform.GetChild(0);


		shapeTransform.position = position;


		if (
			wheel.name == "a0l" ||
			wheel.name == "a1l" ||
			wheel.name == "a2l"
		)
		{
			shapeTransform.rotation =
				rotation *
				Quaternion.Euler(0, 180, 0);
		}
		else
		{
			shapeTransform.rotation =
				rotation;
		}
	}
}