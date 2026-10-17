using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Simulation.Interaction;

namespace Simulation.Player
{
    public class DesktopRayInteractor : MonoBehaviour
    {
        [Header("Raycast Ayarları")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactionDistance = 3.0f;
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("UI Referansı")]
        [SerializeField] private TextMeshProUGUI promptText;

        private IInteractable currentTarget;
        private InteractorContext context;

        private void Start()
        {
            context = new InteractorContext
            {
                InteractorObject = this.gameObject,
                AttachmentPoint = playerCamera != null ? playerCamera.transform : this.transform,
                IsVRMode = false
            };

            if (promptText != null) promptText.text = string.Empty;
        }

        private void Update()
        {
            if (playerCamera == null) return;

            PerformRaycast();

            if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (currentTarget.CanInteract(context))
                {
                    currentTarget.Interact(context);
                }
            }
        }

        private void PerformRaycast()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    if (currentTarget != interactable)
                    {
                        ClearTarget();
                        currentTarget = interactable;
                        currentTarget.OnHoverEnter(context);
                        if (promptText != null) promptText.text = currentTarget.InteractionPrompt;
                    }
                    return;
                }
            }

            ClearTarget();
        }

        private void ClearTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnHoverExit(context);
                currentTarget = null;
                if (promptText != null) promptText.text = string.Empty;
            }
        }
    }
}
