using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorkplaceStudio.Core
{
    public static class GlbFiles
    {
        public const long MaximumBytes = 512L * 1024 * 1024;
        public static void Validate(string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".glb", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Only GLB 2.0 files are supported.");
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                if (stream.Length < 20 || stream.Length > MaximumBytes) throw new InvalidDataException("Invalid GLB size. The current limit is 512 MB.");
                if (reader.ReadUInt32() != 0x46546C67 || reader.ReadUInt32() != 2 || reader.ReadUInt32() != stream.Length)
                    throw new InvalidDataException("Invalid GLB 2.0 header or length. Check that Git LFS assets have been downloaded.");
                uint length = reader.ReadUInt32(), type = reader.ReadUInt32();
                if (type != 0x4E4F534A || length > 16 * 1024 * 1024 || length < 2 || length % 4 != 0 || 20L + length > stream.Length)
                    throw new InvalidDataException("The GLB JSON chunk is invalid.");
                var json = Encoding.UTF8.GetString(reader.ReadBytes((int)length)).TrimEnd(' ', '\0');
                JObject root;
                using (var jsonReader = new JsonTextReader(new StringReader(json)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                    root = JObject.Load(jsonReader);
                if ((string)root["asset"]?["version"] != "2.0") throw new InvalidDataException("Unsupported glTF version.");
                foreach (var collection in new[] { "buffers", "images" })
                    if (root[collection] is JArray items)
                        foreach (var item in items)
                            if (item["uri"] != null) throw new InvalidDataException("The model must be self-contained. External file, data and network URIs are not supported.");
                if (root["nodes"] is JArray nodes && nodes.Count > 20000) throw new InvalidDataException("The model contains too many nodes.");
                // Check every remaining chunk before the importer sees the file.
                bool binary = false;
                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < 8) throw new InvalidDataException("The GLB contains an incomplete chunk.");
                    uint size = reader.ReadUInt32(), chunkType = reader.ReadUInt32();
                    if (size % 4 != 0 || stream.Position + size > stream.Length || chunkType != 0x004E4942 || binary)
                        throw new InvalidDataException("The GLB binary chunk is invalid.");
                    binary = true; stream.Seek(size, SeekOrigin.Current);
                }
                if (!binary) throw new InvalidDataException("The GLB binary data is missing.");
            }
        }
        public static string Sha256(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
        }
        public static void Verify(string path, string expectedHash)
        {
            Validate(path);
            if (!string.Equals(Sha256(path), expectedHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The model file does not match its integrity hash.");
        }
    }
}
