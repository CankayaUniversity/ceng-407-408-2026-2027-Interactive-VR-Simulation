using UnityEngine;
using TMPro;
using Simulation.Interaction;
using Simulation.UI;
using Simulation.WebBridge;

namespace Simulation.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class MedicalDeviceHotspot : MonoBehaviour, IInteractable
    {
        [Header("Device Configuration")]
        public int deviceNumber = 1;
        public string deviceNameEnglish = "Operating Table";
        [TextArea(2, 4)] public string whatItDoesEnglish;
        [TextArea(3, 6)] public string howItWorksEnglish;

        [Header("Linked How-It-Works Orb")]
        public DeviceHowItWorksOrb howItWorksOrb;

        [Header("3D Billboard Label")]
        [SerializeField] private Transform billboardTransform;
        [SerializeField] private TextMeshPro labelText;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer markerRenderer;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1.0f, 0.45f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.9f, 1.0f, 0.85f);
        [SerializeField] private Color inspectedColor = new Color(0.2f, 1.0f, 0.5f, 0.75f);

        private Material markerMaterial;
        private bool hasBeenInspected = false;
        private Camera mainCamera;

        public string InteractionID => $"device_{deviceNumber}_what_it_does";
        public string InteractionPrompt => $"[E / Click] Inspect Device #{deviceNumber}: {deviceNameEnglish}";

        private void Awake()
        {
            if (markerRenderer == null)
            {
                markerRenderer = GetComponent<Renderer>();
            }

            if (markerRenderer != null)
            {
                markerMaterial = markerRenderer.material;
                SetMarkerColor(normalColor);
            }

            // Sync data to linked orb if assigned
            if (howItWorksOrb != null)
            {
                howItWorksOrb.deviceNumber = deviceNumber;
                howItWorksOrb.deviceNameEnglish = deviceNameEnglish;
                howItWorksOrb.howItWorksEnglish = howItWorksEnglish;
                howItWorksOrb.gameObject.SetActive(false);
            }

            if (labelText != null)
            {
                labelText.text = $"[{deviceNumber}]";
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (billboardTransform != null)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    billboardTransform.rotation = Quaternion.LookRotation(billboardTransform.position - mainCamera.transform.position);
                }
            }
        }

        public bool CanInteract(InteractorContext context)
        {
            return true;
        }

        public void OnHoverEnter(InteractorContext context)
        {
            if (!hasBeenInspected)
            {
                SetMarkerColor(hoverColor);
            }
        }

        public void OnHoverExit(InteractorContext context)
        {
            if (!hasBeenInspected)
            {
                SetMarkerColor(normalColor);
            }
        }

        public void Interact(InteractorContext context)
        {
            hasBeenInspected = true;
            SetMarkerColor(inspectedColor);

            // 1. Show Stage 1 What It Does in UI
            if (MedicalDeviceUIController.Instance != null)
            {
                MedicalDeviceUIController.Instance.ShowInfo(
                    $"DEVICE #{deviceNumber}: {deviceNameEnglish.ToUpper()}",
                    "STAGE 1: WHAT IT DOES (FUNCTION)",
                    whatItDoesEnglish,
                    "Look at the glowing blue sphere next to the device and press [E] / Left Click to learn HOW IT WORKS. | [Q] Close Panel"
                );
            }

            // 2. Activate Stage 2 Orb
            if (howItWorksOrb != null)
            {
                howItWorksOrb.gameObject.SetActive(true);
            }

            // 3. Report WebGL progress
            WebBridgeService.ReportStepCompletedStatic($"device_{deviceNumber}_what_it_does", true, Time.timeSinceLevelLoad);
            Debug.Log($"[MedicalDeviceHotspot] Device #{deviceNumber} ({deviceNameEnglish}) inspected.");
        }

        private void SetMarkerColor(Color col)
        {
            if (markerMaterial != null)
            {
                if (markerMaterial.HasProperty("_BaseColor"))
                {
                    markerMaterial.SetColor("_BaseColor", col);
                }
                else if (markerMaterial.HasProperty("_Color"))
                {
                    markerMaterial.color = col;
                }

                if (markerMaterial.HasProperty("_EmissionColor"))
                {
                    markerMaterial.EnableKeyword("_EMISSION");
                    markerMaterial.SetColor("_EmissionColor", col * 0.8f);
                }
            }
        }

        private void OnDestroy()
        {
            if (markerMaterial != null)
            {
                Destroy(markerMaterial);
            }
        }
    }
}
