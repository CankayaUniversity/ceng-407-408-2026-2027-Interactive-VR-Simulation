using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Simulation.UI
{
    public class MedicalDeviceUIController : MonoBehaviour
    {
        public static MedicalDeviceUIController Instance { get; private set; }

        [Header("UI Panel Elements")]
        [SerializeField] private GameObject deviceInfoPanel;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI modeBadgeText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI instructionFooterText;

        public bool IsOpen => deviceInfoPanel != null && deviceInfoPanel.activeSelf;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (deviceInfoPanel != null)
            {
                deviceInfoPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Close panel when Q is pressed
            if (IsOpen && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                HideInfo();
            }
        }

        public void ShowInfo(string header, string modeBadge, string description, string footer = null)
        {
            if (deviceInfoPanel != null)
            {
                deviceInfoPanel.SetActive(true);
            }

            if (headerText != null)
            {
                headerText.text = header;
            }

            if (modeBadgeText != null)
            {
                modeBadgeText.text = modeBadge;
            }

            if (descriptionText != null)
            {
                descriptionText.text = description;
            }

            if (instructionFooterText != null)
            {
                instructionFooterText.text = string.IsNullOrEmpty(footer) 
                    ? "[Q] Close Panel" 
                    : footer;
            }
        }

        public void HideInfo()
        {
            if (deviceInfoPanel != null)
            {
                deviceInfoPanel.SetActive(false);
            }
        }
    }
}
