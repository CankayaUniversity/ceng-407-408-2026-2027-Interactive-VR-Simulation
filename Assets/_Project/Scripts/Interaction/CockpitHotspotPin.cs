using UnityEngine;

namespace Simulation.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class CockpitHotspotPin : MonoBehaviour
    {
        [Header("Pin Configuration")]
        public int hotspotId;
        public string hotspotName;

        [Header("Visual Feedback")]
        public Transform visualTransform;
        public float normalScale = 1.0f;
        public float hoverScale = 1.3f;
        public float scaleSpeed = 10f;

        private float targetScale = 1.0f;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            if (visualTransform == null)
            {
                visualTransform = transform;
            }
            targetScale = normalScale;
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null && visualTransform != null)
            {
                // Billboard: Face the camera directly
                visualTransform.rotation = Quaternion.LookRotation(visualTransform.position - mainCamera.transform.position);
            }

            // Smooth scale interpolation
            if (visualTransform != null)
            {
                float currentScale = visualTransform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
                visualTransform.localScale = Vector3.one * newScale;
            }
        }

        private void OnMouseEnter()
        {
            targetScale = hoverScale;
        }

        private void OnMouseExit()
        {
            targetScale = normalScale;
        }

        private void OnMouseDown()
        {
            TriggerSelection();
        }

        public void TriggerSelection()
        {
            if (CockpitInspectionManager.Instance != null)
            {
                CockpitInspectionManager.Instance.SelectHotspot(hotspotId);
            }
            else
            {
                Debug.Log($"[HotspotPin] Selected #{hotspotId} - {hotspotName}");
            }
        }
    }
}
