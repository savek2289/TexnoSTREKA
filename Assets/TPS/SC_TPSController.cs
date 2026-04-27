using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TPSControllerTemplate : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float airControlPercent = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Camera")]
    [SerializeField] private Transform cameraParent;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 60f;

    [HideInInspector]
    public bool isShift = false;

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalVelocity;

    private float currentYRotation;
    private float rotationVelocity;
    private float cameraXRotation;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCamera();
        HandleMovement();
    }

    private void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentYRotation += mouseX;

        cameraXRotation -= mouseY;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -verticalLookLimit, verticalLookLimit);

        cameraParent.localRotation = Quaternion.Euler(cameraXRotation, currentYRotation, 0f);
    }

    private void HandleMovement()
    {
        float dt = Time.unscaledDeltaTime;

        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        isShift = Input.GetKey(KeyCode.LeftShift);

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        float targetSpeed = isShift ? runSpeed : walkSpeed;

        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + currentYRotation;

            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                rotationSmoothTime
            );
            Debug.Log("s");
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            float control = isGrounded ? 1f : airControlPercent;

            velocity.x = moveDir.x * targetSpeed * control;
            velocity.z = moveDir.z * targetSpeed * control;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
            verticalVelocity = jumpForce;

        verticalVelocity -= gravity * dt;
        velocity.y = verticalVelocity;

        controller.Move(velocity * dt);
    }
}