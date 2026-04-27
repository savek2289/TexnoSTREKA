using UnityEngine;

public class SC_CameraCollision : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private LayerMask collisionMask; // 🔥 выбираемые слои

    private Vector3 defaultLocalPos;
    private float defaultDistance;
    private Vector3 direction;

    private void Start()
    {
        defaultLocalPos = transform.localPosition;
        defaultDistance = defaultLocalPos.magnitude;
        direction = defaultLocalPos.normalized;
    }

    private void LateUpdate()
    {
        RaycastHit hit;
        Vector3 desiredPosition = defaultLocalPos;

        Vector3 worldDir = transform.parent.TransformPoint(defaultLocalPos) - target.position;

        if (Physics.SphereCast(
            target.position,
            collisionRadius,
            worldDir.normalized,
            out hit,
            defaultDistance,
            collisionMask // 🔥 используем маску
        ))
        {
            desiredPosition = direction * (hit.distance - collisionRadius);
        }

        transform.localPosition =
            Vector3.Lerp(transform.localPosition, desiredPosition, Time.deltaTime * smoothSpeed);
    }
}