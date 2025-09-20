using UnityEngine;
using UnityEngine.InputSystem; // IMPORTANT

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public Transform cameraTransform;
    private Vector3 camerapos = new Vector3(0, 1.5f, -2);

    private CharacterController controller;
    private Vector3 velocity;

    private PlayerControls controls;
    private Vector2 moveInput;
    private bool jumpPressed;

    void Awake()
    {
        controls = new PlayerControls();

        // Move = Vector2
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Jump = Button
        controls.Player.Jump.performed += ctx => jumpPressed = true;
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        cameraTransform.position = transform.position + camerapos;
    }

    void Update()
    {
        // Déplacement relatif à la caméra
        Vector3 move = (cameraTransform.right * moveInput.x + cameraTransform.forward * moveInput.y);
        move.y = 0f;
        controller.Move(move * speed * Time.deltaTime);

        // Gravité
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Saut (via Input System)
        if (jumpPressed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false; // reset pour éviter le spam
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
