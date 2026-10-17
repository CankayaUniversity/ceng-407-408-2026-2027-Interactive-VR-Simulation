using UnityEngine;
using UnityEngine.UI;

namespace Simulation.Interaction
{
    public class CockpitUIOverlay : MonoBehaviour
    {
        [Header("Bottom Navigation Bar")]
        public GameObject bottomBarPanel;
        public Text bottomTitleText;
        public Button prevButton;
        public Button nextButton;
        public Button overviewButton;

        [Header("Hotspot Detail Card (Tooltip)")]
        public GameObject detailCardPanel;
        public Text detailTitleText;
        public Text detailSubtitleText;
        public Text detailDescriptionText;

        [Header("Status & Keybind Hint")]
        public Text keybindHintText;

        private void Start()
        {
            if (CockpitInspectionManager.Instance != null)
            {
                CockpitInspectionManager.Instance.OnHotspotChanged += HandleHotspotChanged;
                CockpitInspectionManager.Instance.OnOverviewReset += HandleOverviewReset;
            }

            if (prevButton != null)
            {
                prevButton.onClick.AddListener(() =>
                {
                    if (CockpitInspectionManager.Instance != null)
                    {
                        CockpitInspectionManager.Instance.PreviousHotspot();
                    }
                });
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() =>
                {
                    if (CockpitInspectionManager.Instance != null)
                    {
                        CockpitInspectionManager.Instance.NextHotspot();
                    }
                });
            }

            if (overviewButton != null)
            {
                overviewButton.onClick.AddListener(() =>
                {
                    if (CockpitInspectionManager.Instance != null)
                    {
                        CockpitInspectionManager.Instance.ResetToOverview();
                    }
                });
            }

            if (keybindHintText != null)
            {
                keybindHintText.text = "[A / D or Arrow Keys]: Navigate | [Space / ESC]: Overview | [Click Pins]: Zoom-In";
            }

            HandleOverviewReset();
        }

        private void OnDestroy()
        {
            if (CockpitInspectionManager.Instance != null)
            {
                CockpitInspectionManager.Instance.OnHotspotChanged -= HandleHotspotChanged;
                CockpitInspectionManager.Instance.OnOverviewReset -= HandleOverviewReset;
            }
        }

        private void HandleHotspotChanged(CockpitHotspotData data)
        {
            if (data == null) return;

            // Update bottom bar
            if (bottomTitleText != null)
            {
                bottomTitleText.text = $"{data.id:D2}. {data.title}";
            }

            // Show and update detail card
            if (detailCardPanel != null)
            {
                detailCardPanel.SetActive(true);
            }

            if (detailTitleText != null)
            {
                detailTitleText.text = data.title;
            }

            if (detailSubtitleText != null)
            {
                detailSubtitleText.text = data.subtitle;
            }

            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = data.description;
            }
        }

        private void HandleOverviewReset()
        {
            if (bottomTitleText != null)
            {
                bottomTitleText.text = "Airbus H145 Cockpit (Overview)";
            }

            // Hide or keep minimal detail card in overview
            if (detailCardPanel != null)
            {
                detailCardPanel.SetActive(false);
            }
        }
    }
}
