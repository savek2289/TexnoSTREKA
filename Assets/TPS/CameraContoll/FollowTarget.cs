using UnityEngine;

public class FollowTargetTemplate : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0;
    [SerializeField] private Vector3 offset;

    [Header("Shake")]
    [SerializeField] private float walkShake = 0.05f;
    [SerializeField] private float runShake = 0.1f;
    [SerializeField] private float shakeSpeed = 8f;
    [SerializeField] private float returnSpeed = 5f;

    private Vector3 velocity;
    private float shakeTimer;
    private Vector3 currentShakeOffset;

    private TPSControllerTemplate controller;
    private Vector3 lastTargetPos;

    private void Start()
    {
        if (target != null)
            controller = target.GetComponent<TPSControllerTemplate>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // FOLLOW
        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // SHAKE
        bool isMoving = IsTargetMoving();

        if (isMoving)
        {
            float shakeAmount = (controller != null && controller.isShift) ? runShake : walkShake;

            shakeTimer += Time.deltaTime * shakeSpeed;

            float shakeY = Mathf.Sin(shakeTimer) * shakeAmount;
            float shakeX = Mathf.Cos(shakeTimer * 0.5f) * shakeAmount * 0.5f;

            currentShakeOffset = new Vector3(shakeX, shakeY, 0f);
        }
        else
        {
            shakeTimer = 0f;
            currentShakeOffset = Vector3.Lerp(
                currentShakeOffset,
                Vector3.zero,
                Time.deltaTime * returnSpeed
            );
        }

        transform.position += currentShakeOffset;
    }

    private bool IsTargetMoving()
    {
        bool moving = (target.position - lastTargetPos).sqrMagnitude > 0.0001f;
        lastTargetPos = target.position;
        return moving;
    }
}