using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float horizontalSpeed = 8f;
    public float verticalSpeedMultiplier = 0.6f;
    [Tooltip("Horizontal acceleration (units/sec^2)")]
    public float acceleration = 30f;
    [Tooltip("Horizontal deceleration (units/sec^2)")]
    public float deceleration = 40f;


    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private InputActionMap playerActionMap;

    private Vector2 moveInput;            // Left stick (horizontal movement)
    private Vector2 verticalStickInput;   // Right stick (vertical movement)
    // Smoothed horizontal velocity in world space (xz plane)
    private Vector3 currentHorizontalVelocity = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset not assigned!");
            return;
        }

        playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("'Player' action map not found!");
            return;
        }

        playerActionMap.Enable();

        // Horizontal movement (left stick)
        var moveAction = playerActionMap.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled += ctx => moveInput = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("'Move' action not found!");
        }

        // Vertical movement (right stick Y)
        var verticalMoveAction = playerActionMap.FindAction("VerticalMove");
        if (verticalMoveAction != null)
        {
            verticalMoveAction.performed += ctx => verticalStickInput = ctx.ReadValue<Vector2>();
            verticalMoveAction.canceled += ctx => verticalStickInput = Vector2.zero;
        }
        else
        {
            Debug.LogWarning("'VerticalMove' action not found!");
        }

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("Camera not found! Assign Camera Transform manually.");
            }
        }
    }

    void Update()
    {
        if (cameraTransform == null || controller == null)
            return;

        // Head-relative forward and right (flattened)
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Vertical input with comfort scaling
        float verticalInput = Mathf.Clamp(verticalStickInput.y, -1f, 1f);
        float verticalSpeed = horizontalSpeed * verticalSpeedMultiplier;

        // Desired horizontal velocity in world space (xz plane)
        Vector3 desiredHorizontal = (camRight * moveInput.x + camForward * moveInput.y) * horizontalSpeed;

        // Choose accel vs decel based on whether we're increasing speed
        float currentSpeed = currentHorizontalVelocity.magnitude;
        float desiredSpeed = desiredHorizontal.magnitude;
        bool accelerating = desiredSpeed > currentSpeed + 0.001f;

        float maxDelta = (accelerating ? acceleration : deceleration) * Time.deltaTime;

        // Smoothly move current horizontal velocity toward desired
        currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredHorizontal, maxDelta);

        Vector3 move = currentHorizontalVelocity + Vector3.up * verticalInput * verticalSpeed;

        controller.Move(move * Time.deltaTime);
    }

    void OnDestroy()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }
}
