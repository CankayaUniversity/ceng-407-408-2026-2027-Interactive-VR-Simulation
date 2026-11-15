using System;
using System.Collections.Generic;

namespace WorkplaceStudio.Core
{
    // File coordinates stay right-handed/Y-up, as in the existing web editor.
    // Only Unity's presentation adapter mirrors X; saved data is engine-independent.
    [Serializable] public sealed class Pose
    {
        public float[] position = { 0, 1.65f, 4.5f };
        public float[] target = { 0, 1.65f, 0 };
        public float fovDegrees = 70;
    }
    [Serializable] public sealed class ModelTransform
    {
        public float[] position = { 0, 0, 0 };
        public float[] rotation = { 0, 0, 0 };
        public float[] scale = { 1, 1, 1 };
    }
    [Serializable] public sealed class Area
    {
        public string id, title, environmentId, parentZoneId;
        public int? order;
        public string calibration = "reviewed";
        public Pose camera = new Pose();
    }
    [Serializable] public sealed class Hotspot
    {
        public string id, title, description = "", zoneId, targetNode;
        public float[] position = { 0, 0, 0 };
        public float[] targetPosition;
    }
    [Serializable] public sealed class EditorDocument
    {
        public int schemaVersion = 1;
        public int? hierarchyVersion;
        public string environmentId, modelUrl;
        public ModelTransform modelTransform = new ModelTransform();
        public List<Area> zones = new List<Area>();
        public List<Hotspot> hotspots = new List<Hotspot>();
    }
    [Serializable] public sealed class Attribution
    {
        public string author, sourceUrl, license, licenseUrl;
    }
    [Serializable] public sealed class TrainingProject
    {
        public int formatVersion = 1;
        public string id, title, environmentId, modelFile, modelSha256;
        public string navigationMode = "walking";
        public Pose overview = new Pose();
        public EditorDocument editor = new EditorDocument();
        public Attribution attribution;
        public List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
    }
    [Serializable] public sealed class Catalogue
    {
        public int formatVersion = 1;
        public string sourceCommit;
        public List<TrainingProject> environments = new List<TrainingProject>();
    }

    // Device adapters call the same operations; a future XR adapter does not own training state.
    public interface ITrainingActions
    {
        void OpenArea(string id);
        void ActivateHotspot(string id);
        void ReturnToOverview();
    }
}
