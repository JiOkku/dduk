using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [Tooltip("Assign Assets/Settings/InputSystem_Actions (Player/Move).")]
    [SerializeField] private InputActionAsset inputActions;

    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2 moveInput;

    private void Reset()
    {
        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        rigidbody2D.gravityScale = 0f;
        rigidbody2D.linearDamping = 0f;
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        InputAction source = inputActions != null
            ? inputActions.FindAction("Player/Move", false)
            : null;

        if (source == null || source.expectedControlType != "Vector2")
        {
            Debug.LogError("Assign an Input Action Asset containing Player/Move (Vector2).", this);
            enabled = false;
            return;
        }

        // Own the action lifecycle without disabling the project's shared actions.
        moveAction = source.Clone();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void Update()
    {
        moveInput = Application.isFocused
            ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f)
            : Vector2.zero;
    }

    private void FixedUpdate()
    {
        // Velocity is in units/second; the physics engine applies the time step.
        body.linearVelocity = moveInput * Mathf.Max(0f, moveSpeed);
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        moveInput = Vector2.zero;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }
}
