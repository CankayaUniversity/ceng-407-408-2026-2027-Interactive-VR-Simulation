using UnityEngine;
using Simulation.Interaction;
using Simulation.UI;
using Simulation.WebBridge;

namespace Simulation.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class DeviceHowItWorksOrb : MonoBehaviour, IInteractable
    {
        [Header("Device Reference")]
        public int deviceNumber = 1;
        public string deviceNameEnglish = "Operating Table";
        [TextArea(3, 6)] public string howItWorksEnglish;

        [Header("Floating Animation")]
        [SerializeField] private float floatSpeed = 3.0f;
        [SerializeField] private float floatAmplitude = 0.035f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private Color activeCyanColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private Color completedGreenColor = new Color(0.1f, 1.0f, 0.45f, 1.0f);

        private Vector3 initialLocalPos;
        private bool isCompleted = false;
        public bool IsCompleted => isCompleted;
        private Material orbMaterial;

        public string InteractionID => $"device_{deviceNumber}_how_it_works";
        public string InteractionPrompt => $"[E / Click] How Does Device #{deviceNumber} Work?";

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            if (orbRenderer == null)
            {
                orbRenderer = GetComponent<Renderer>();
            }

            if (orbRenderer != null)
            {
                orbMaterial = orbRenderer.material;
                SetOrbColor(activeCyanColor);
            }
        }

        private void OnEnable()
        {
            // Reset floating base pos when activated
            initialLocalPos = transform.localPosition;
        }

        private void Update()
        {
            // Gentle hovering floating motion
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0f, yOffset, 0f);
        }

        public bool CanInteract(InteractorContext context)
        {
            return true;
        }

        public void OnHoverEnter(InteractorContext context)
        {
            if (orbRenderer != null)
            {
                transform.localScale = Vector3.one * 0.26f;
            }
        }

        public void OnHoverExit(InteractorContext context)
        {
            if (orbRenderer != null)
            {
                transform.localScale = Vector3.one * 0.22f;
            }
        }

        public void Interact(InteractorContext context)
        {
            isCompleted = true;
            SetOrbColor(completedGreenColor);

            if (MedicalDeviceUIController.Instance != null)
            {
                MedicalDeviceUIController.Instance.ShowInfo(
                    $"DEVICE #{deviceNumber}: {deviceNameEnglish.ToUpper()}",
                    "STAGE 2: HOW IT WORKS (MECHANISM)",
                    howItWorksEnglish,
                    "[Q] Close Panel"
                );
            }

            WebBridgeService.ReportStepCompletedStatic($"device_{deviceNumber}_how_it_works", true, Time.timeSinceLevelLoad);
            Debug.Log($"[DeviceHowItWorksOrb] Device #{deviceNumber} Mechanism inspected.");
        }

        private void SetOrbColor(Color color)
        {
            if (orbMaterial != null)
            {
                if (orbMaterial.HasProperty("_BaseColor"))
                {
                    orbMaterial.SetColor("_BaseColor", color);
                }
                else if (orbMaterial.HasProperty("_Color"))
                {
                    orbMaterial.color = color;
                }

                if (orbMaterial.HasProperty("_EmissionColor"))
                {
                    orbMaterial.EnableKeyword("_EMISSION");
                    orbMaterial.SetColor("_EmissionColor", color * 1.5f);
                }
            }
        }

        private void OnDestroy()
        {
            if (orbMaterial != null)
            {
                Destroy(orbMaterial);
            }
        }
    }
}
