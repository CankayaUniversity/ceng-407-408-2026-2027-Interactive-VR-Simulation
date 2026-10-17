using UnityEngine;
using Simulation.Interaction;
using Simulation.WebBridge;

namespace Simulation.Interactables
{
    public class TestEmergencyButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private string buttonId = "emergency_btn_01";
        [SerializeField] private string promptMessage = "[E] Acil Durum Butonuna Bas";
        [SerializeField] private WebBridgeService webBridge;

        private MeshRenderer meshRenderer;
        private Color originalColor;
        private bool isPressed = false;

        public string InteractionID => buttonId;
        public string InteractionPrompt => isPressed ? "Buton Aktif Edildi" : promptMessage;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        public bool CanInteract(InteractorContext context)
        {
            return !isPressed;
        }

        public void OnHoverEnter(InteractorContext context)
        {
            if (!isPressed && meshRenderer != null) meshRenderer.material.color = Color.yellow;
        }

        public void OnHoverExit(InteractorContext context)
        {
            if (!isPressed && meshRenderer != null) meshRenderer.material.color = originalColor;
        }

        public void Interact(InteractorContext context)
        {
            isPressed = true;
            if (meshRenderer != null) meshRenderer.material.color = Color.green;
            Debug.Log($"Butona basıldı: {buttonId}");

            if (webBridge != null)
            {
                webBridge.ReportStepCompleted(buttonId, true, Time.timeSinceLevelLoad);
            }
        }
    }
}
