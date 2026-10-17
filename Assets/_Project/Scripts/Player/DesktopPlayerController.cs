using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayerController : MonoBehaviour
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Kamera (Mouse) Ayarları")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float maxLookAngle = 80f;

        private CharacterController characterController;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private float verticalVelocity;
        private float cameraPitch = 0f;
        private bool isCursorLocked = false;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!isCursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor(true);
            }

            if (isCursorLocked && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
            }

            if (isCursorLocked)
            {
                ReadInputs();
                HandleLook();
                HandleMovement();
            }
        }

        private void ReadInputs()
        {
            if (Keyboard.current != null)
            {
                float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
                float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
                moveInput = new Vector2(x, y).normalized;
            }

            if (Mouse.current != null)
            {
                lookInput = Mouse.current.delta.ReadValue();
            }
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;

            cameraPitch -= lookInput.y * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
            cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

            transform.Rotate(Vector3.up * (lookInput.x * mouseSensitivity));
        }

        private void HandleMovement()
        {
            if (characterController.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }

            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalVelocity = (moveDirection * moveSpeed) + (Vector3.up * verticalVelocity);
            characterController.Move(finalVelocity * Time.deltaTime);
        }

        public void LockCursor(bool shouldLock)
        {
            isCursorLocked = shouldLock;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }
}
