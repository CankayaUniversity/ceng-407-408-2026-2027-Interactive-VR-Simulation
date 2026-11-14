using System;
using System.IO;
using System.Text;

namespace WorkplaceStudio.Core
{
    public static class ProjectStore
    {
        public static TrainingProject Open(string directory)
        {
            var path = LocalPaths.Resolve(directory, "project.json");
            if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidDataException("The project file is too large.");
            var project = TrainingJson.Read<TrainingProject>(File.ReadAllText(path, Encoding.UTF8));
            TrainingValidation.Project(project);
            GlbFiles.Verify(LocalPaths.Resolve(directory, project.modelFile), project.modelSha256);
            return project;
        }
        public static void Save(string directory, TrainingProject project, string sourceModel)
        {
            // Validate and copy first; publish the manifest only after all assets exist.
            TrainingValidation.Project(project); GlbFiles.Verify(sourceModel, project.modelSha256);
            var saved = TrainingJson.Clone(project);
            saved.modelFile = "assets/" + saved.modelSha256.ToLowerInvariant() + ".glb";
            Directory.CreateDirectory(directory);
            var destination = LocalPaths.Resolve(directory, saved.modelFile);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (File.Exists(destination)) GlbFiles.Verify(destination, saved.modelSha256);
            else
            {
                var modelTemp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp.glb";
                try
                {
                    File.Copy(sourceModel, modelTemp); GlbFiles.Verify(modelTemp, saved.modelSha256);
                    File.Move(modelTemp, destination);
                }
                finally { if (File.Exists(modelTemp)) File.Delete(modelTemp); }
            }
            var manifest = LocalPaths.Resolve(directory, "project.json");
            var temporary = LocalPaths.Resolve(directory, "project-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(TrainingJson.Write(saved)); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (File.Exists(manifest)) File.Replace(temporary, manifest, LocalPaths.Resolve(directory, "project.previous.json"));
                else File.Move(temporary, manifest);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
