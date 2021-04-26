using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
    // visible props
    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;

    private int _steer = 0;
    private int _accelerate = 0;


    public Transform DrivingPosition;
    public Transform FishinRodPosition;


    // Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;


    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
    }

    private void Update()
    {
        // _steer = 0;
        // if (Input.GetKey(KeyCode.A))
        //     _steer += 1;
        // if (Input.GetKey(KeyCode.D))
        //     _steer += -1;

        // _accelerate = 0;
        // if (Input.GetKey(KeyCode.W))
        //     _accelerate += 1;
        // if (Input.GetKey(KeyCode.S))
        //     _accelerate += -1;
    }

    void FixedUpdate()
    {
        // var forceDirection = transform.forward;

        // Steering (rotating) force
        Rigidbody.AddForceAtPosition(_steer * transform.right * SteerPower / 100f, Motor.position);

        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);
        if (_accelerate != 0)
        {
            // Rigidbody.AddForceAtPosition(_accelerate * forward * Power * Time.fixedDeltaTime, Motor.position);
            ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * _accelerate, Power);
        }

        //moving forward
        var movingForward = Vector3.Cross(transform.forward, Rigidbody.velocity).y < 0;
        //move in direction
        Rigidbody.velocity = Quaternion.AngleAxis(Vector3.SignedAngle(Rigidbody.velocity, (movingForward ? 1f : 0f) * transform.forward, Vector3.up) * Drag, Vector3.up) * Rigidbody.velocity;

    }

    public void Steer(int dir)
    {
        _steer = Mathf.Clamp(dir, -1, 1);
    }

    public void Accelerate(int dir)
    {
        _accelerate = Mathf.Clamp(dir, -1, 1);
    }

    public void RemoveFishingRod() => FishinRodPosition.gameObject.SetActive(false);
    public void PlaceFishingRod() => FishinRodPosition.gameObject.SetActive(true);

    public static void ApplyForceToReachVelocity(Rigidbody rigidbody, Vector3 velocity, float force = 1, ForceMode mode = ForceMode.Force)
    {
        if (force == 0 || velocity.magnitude == 0)
            return;

        velocity = velocity + velocity.normalized * 0.2f * rigidbody.drag;

        //force = 1 => need 1 s to reach velocity (if mass is 1) => force can be max 1 / Time.fixedDeltaTime
        force = Mathf.Clamp(force, -rigidbody.mass / Time.fixedDeltaTime, rigidbody.mass / Time.fixedDeltaTime);

        //dot product is a projection from rhs to lhs with a length of result / lhs.magnitude https://www.youtube.com/watch?v=h0NJK4mEIJU
        if (rigidbody.velocity.magnitude == 0)
        {
            rigidbody.AddForce(velocity * force, mode);
        }
        else
        {
            var velocityProjectedToTarget = (velocity.normalized * Vector3.Dot(velocity, rigidbody.velocity) / velocity.magnitude);
            rigidbody.AddForce((velocity - velocityProjectedToTarget) * force, mode);
        }
    }
}
