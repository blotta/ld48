using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFloat2 : MonoBehaviour
{
    public Rigidbody rigidbody;
    public float depthBeforeSubmerged = 1f;
    public float displacementAmount = 3f;
    public float floaterCount = 1;
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;

    private Waves _waves;

    private void Start()
    {
        _waves = FindObjectOfType<Waves>();
    }

    void FixedUpdate()
    {
        rigidbody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);

        float waveHeight = _waves.GetHeight(transform.position);
        if (transform.position.y < waveHeight)
        {
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;
            rigidbody.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), transform.position, ForceMode.Acceleration);
            rigidbody.AddForce(displacementMultiplier * -rigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
}
