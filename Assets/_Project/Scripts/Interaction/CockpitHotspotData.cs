using System;
using UnityEngine;

namespace Simulation.Interaction
{
    [Serializable]
    public class CockpitHotspotData
    {
        public int id;
        public string objectName;
        public string title;
        public string subtitle;
        [TextArea(2, 5)]
        public string description;
        public Vector3 cameraPosition;
        public Vector3 cameraLookAt;
        public float fieldOfView = 35f;

        public CockpitHotspotData(int id, string objectName, string title, string subtitle, string description, Vector3 camPos, Vector3 lookAt, float fov = 35f)
        {
            this.id = id;
            this.objectName = objectName;
            this.title = title;
            this.subtitle = subtitle;
            this.description = description;
            this.cameraPosition = camPos;
            this.cameraLookAt = lookAt;
            this.fieldOfView = fov;
        }
    }
}
