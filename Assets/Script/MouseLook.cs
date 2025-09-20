using UnityEngine;
using UnityEngine.InputSystem; // IMPORTANT

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;
    public float mouseSensitivity = 10f;

    float xRotation = 0f;
    private PlayerControls controls;
    private Vector2 lookInput;

    void Awake()
    {
        controls = new PlayerControls();

        // Look = Vector2
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
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
        // Verrouille le curseur au centre de l'écran
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Déplacements de la souris
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        // Rotation verticale (limité pour éviter de tourner la tête à 360°)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotation horizontale du joueur (le corps suit la souris gauche/droite)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
