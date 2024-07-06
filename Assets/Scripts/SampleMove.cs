using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleMove : MonoBehaviour
{
	public Vector3 DebugStartPosition;
	public Vector3 DebugEndPosition;
    //private Vector3 m_TargetPosition;
    private AnimationCurve m_MovevementCurve;

    public float duration = 2.0f;
    private float timeElapsed;

    // Start is called before the first frame update
    void Start() { }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            float curveValue = m_MovevementCurve.Evaluate(t);
            Vector3 interpolatedVector = Vector3.Lerp(DebugStartPosition, DebugEndPosition, curveValue);
            // Do something with the interpolatedVector, e.g., update the position of an object
            transform.position = interpolatedVector;
        }
        //else
        //{
        //    // Ensure the end vector is set when the interpolation finishes
        //    transform.position = endVector;
        //}
    }
}
