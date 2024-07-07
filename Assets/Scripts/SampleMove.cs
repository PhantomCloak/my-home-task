using System.Linq;
using UnityEngine;

public class SampleMove : MonoBehaviour
{
    public float Speed = 2.0f;

    [SerializeField]
    private AnimationCurve m_MovevementCurve;
    private WoodResource m_CurrentTargetResource;

    private Vector3 m_TargetPosition = Vector3.positiveInfinity;

    [SerializeField]
    private string m_WoodColliderTag;

    [SerializeField]
    private float m_AdditionalStopDistance = 2.0f;
    private float m_TargetDistance;

    void Start() { }

    void Navigate(Vector3 self, Vector3 target)
    {
        m_TargetPosition = target;
        m_TargetDistance = Vector3.Distance(self, target);
    }

    void Update()
    {
        var currentPositionMinusY = new Vector3(transform.position.x, 0, transform.position.z);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePresent ? Input.mousePosition : Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out var hit))
            {

                var isResource = hit.collider.CompareTag("Wood");
                var destination = isResource
                    ? hit.point - (m_AdditionalStopDistance * Vector3.Normalize(hit.point - currentPositionMinusY))
                    : hit.point;
                destination.y = 0;

                Navigate(currentPositionMinusY, destination);

                if (isResource)
                {
                    m_CurrentTargetResource = hit.collider.gameObject.GetComponent<WoodResource>();
                    m_CurrentTargetResource.Select();
                }
                else if (m_CurrentTargetResource)
                {
                    m_CurrentTargetResource.DeSelect();
                    m_CurrentTargetResource = null;
                }
            }
        }

        if (m_TargetPosition.Equals(Vector3.positiveInfinity))
        {
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            m_TargetPosition,
            m_MovevementCurve.Evaluate(Speed * Time.deltaTime) / m_TargetDistance
        );

        const float epsilonSqr = 0.001f;
        if ((currentPositionMinusY - m_TargetPosition).sqrMagnitude < epsilonSqr)
        {
            transform.position = m_TargetPosition;
            m_TargetPosition = Vector3.positiveInfinity;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Wood"))
        {
            return;
        }

        if (collider.gameObject != m_CurrentTargetResource?.gameObject)
        {
            return;
        }

		Destroy(collider.gameObject);

		// Altough there is batching going on internally, this call can be optimised further on game-play level
        using (var snapHandle = new SnapshotHandle(AcquireType.ReadWrite))
        {
			var woodResource = snapHandle.Value.Items.FirstOrDefault();
			woodResource.Count++;
        }
    }
}
