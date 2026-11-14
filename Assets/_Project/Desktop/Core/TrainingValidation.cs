using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WorkplaceStudio.Core
{
    public static class TrainingJson
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MaxDepth = 48,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };
        public static T Read<T>(string json) where T : class
        {
            if (json == null || json.Length > 2 * 1024 * 1024) throw new InvalidDataException("The project file is too large.");
            return JsonConvert.DeserializeObject<T>(json, Settings) ?? throw new InvalidDataException("The project file is empty.");
        }
        public static string Write(object value) => JsonConvert.SerializeObject(value, Formatting.Indented, Settings);
        public static TrainingProject Clone(TrainingProject project) => Read<TrainingProject>(Write(project));
    }

    public static class TrainingValidation
    {
        public static void Text(string value, int maximum, string field, bool required = true)
        {
            if ((required && string.IsNullOrWhiteSpace(value)) || (value != null && value.Length > maximum))
                throw new InvalidDataException(field + " is invalid.");
        }
        public static void Vector(float[] value, string field, bool nonzero = false)
        {
            if (value == null || value.Length != 3 || value.Any(n => float.IsNaN(n) || float.IsInfinity(n) || Math.Abs(n) > 100000) ||
                (nonzero && value.Any(n => Math.Abs(n) < .000001f))) throw new InvalidDataException(field + " is invalid.");
        }
        public static void Camera(Pose pose)
        {
            if (pose == null) throw new InvalidDataException("Camera pose is missing.");
            Vector(pose.position, "Camera position"); Vector(pose.target, "Camera target");
            if (float.IsNaN(pose.fovDegrees) || pose.fovDegrees < 10 || pose.fovDegrees > 120 ||
                !pose.position.Where((n, i) => Math.Abs(n - pose.target[i]) > .000001f).Any())
                throw new InvalidDataException("Camera pose is invalid.");
        }
        public static void Editor(EditorDocument data)
        {
            if (data == null || data.schemaVersion != 1 || (data.hierarchyVersion.HasValue && data.hierarchyVersion != 1))
                throw new InvalidDataException("Unsupported training data version.");
            Text(data.environmentId, 100, "Environment ID"); Text(data.modelUrl, 1000, "Model reference");
            if (data.modelTransform == null) throw new InvalidDataException("Model transform is missing.");
            Vector(data.modelTransform.position, "Model position"); Vector(data.modelTransform.rotation, "Model rotation"); Vector(data.modelTransform.scale, "Model scale", true);
            if (data.zones == null || data.hotspots == null || data.zones.Count > 32 || data.hotspots.Count > 100)
                throw new InvalidDataException("A project supports up to 32 areas and 100 hotspots.");
            var areas = new Dictionary<string, Area>(StringComparer.Ordinal);
            foreach (var area in data.zones)
            {
                if (area == null) throw new InvalidDataException("Area is missing.");
                Text(area.id, 100, "Area ID"); Text(area.title, 100, "Area title"); Camera(area.camera);
                if (area.environmentId != data.environmentId || area.calibration != "reviewed" || areas.ContainsKey(area.id) || area.order < 0)
                    throw new InvalidDataException("Area ID, order or environment is invalid.");
                areas.Add(area.id, area);
            }
            foreach (var area in data.zones)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal) { area.id };
                var parent = area.parentZoneId;
                while (parent != null)
                {
                    if (!areas.TryGetValue(parent, out var ancestor) || !visited.Add(parent)) throw new InvalidDataException("The area hierarchy contains a missing parent or a cycle.");
                    parent = ancestor.parentZoneId;
                }
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spot in data.hotspots)
            {
                if (spot == null) throw new InvalidDataException("Hotspot is missing.");
                Text(spot.id, 100, "Hotspot ID"); Text(spot.title, 100, "Hotspot title"); Text(spot.description, 2000, "Training information", false); Vector(spot.position, "Hotspot position");
                if (!ids.Add(spot.id)) throw new InvalidDataException("Duplicate hotspot ID.");
                if (spot.zoneId != null) Text(spot.zoneId, 100, "Move to area");
            }
        }
        public static void Project(TrainingProject project)
        {
            if (project == null || project.formatVersion != 1) throw new InvalidDataException("Unsupported project version.");
            Text(project.id, 100, "Project ID"); Text(project.title, 100, "Project name"); Camera(project.overview); Editor(project.editor);
            if (project.environmentId != project.editor.environmentId ||
                (project.navigationMode != "walking" && project.navigationMode != "seated" && project.navigationMode != "spectator"))
                throw new InvalidDataException("Project environment or navigation mode is invalid.");
            if (project.modelSha256 == null || project.modelSha256.Length != 64 || project.modelSha256.Any(c => !Uri.IsHexDigit(c)))
                throw new InvalidDataException("The model integrity hash is missing.");
            LocalPaths.ValidateRelative(project.modelFile);
        }
    }

    public static class LocalPaths
    {
        public static void ValidateRelative(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || relative.Length > 500 || Path.IsPathRooted(relative) || relative.Contains(":") ||
                relative.IndexOf('\0') >= 0 || relative.Split('/', '\\').Any(part => part == ".." || part == "." || part.Length == 0))
                throw new InvalidDataException("The model path must stay inside the project folder.");
        }
        public static string Resolve(string root, string relative)
        {
            ValidateRelative(relative);
            var canonicalRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(canonicalRoot, relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A path outside the project folder was rejected.");
            var current = new DirectoryInfo(Path.GetDirectoryName(full));
            while (current != null && current.FullName.Length >= canonicalRoot.TrimEnd(Path.DirectorySeparatorChar).Length)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked folders inside a project are not supported.");
                current = current.Parent;
            }
            if (File.Exists(full) && (File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked files are not supported.");
            return full;
        }
    }
}
