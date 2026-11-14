using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WorkplaceStudio.Core
{
    public static class AreaHierarchy
    {
        public static List<Area> Children(EditorDocument data, string parent = null) => data.zones
            .Where(a => a.parentZoneId == parent).OrderBy(a => a.order ?? data.zones.IndexOf(a)).ToList();
        public static List<Area> Path(EditorDocument data, string id)
        {
            var path = new List<Area>(); var seen = new HashSet<string>();
            while (id != null && seen.Add(id))
            {
                var area = data.zones.Find(a => a.id == id); if (area == null) break;
                path.Insert(0, area); id = area.parentZoneId;
            }
            return path;
        }
        public static void Move(EditorDocument data, string id, string parent)
        {
            var area = data.zones.Find(a => a.id == id) ?? throw new InvalidDataException("Area not found.");
            if (parent != null && (!data.zones.Any(a => a.id == parent) || Path(data, parent).Any(a => a.id == id)))
                throw new InvalidDataException("An area cannot be moved under itself or a descendant.");
            if (area.parentZoneId == parent) return;
            var siblings = Children(data, parent).Where(a => a.id != id).ToList();
            area.parentZoneId = parent;
            area.order = siblings.Count == 0 ? 0 : siblings.Max(a => a.order ?? data.zones.IndexOf(a)) + 1;
            data.hierarchyVersion = 1;
        }
        public static void Reorder(EditorDocument data, string id, int delta)
        {
            var area = data.zones.Find(a => a.id == id) ?? throw new InvalidDataException("Area not found.");
            var siblings = Children(data, area.parentZoneId); int from = siblings.IndexOf(area), to = from + Math.Sign(delta);
            if (to < 0 || to >= siblings.Count) return;
            siblings[from] = siblings[to]; siblings[to] = area;
            for (int i = 0; i < siblings.Count; i++) siblings[i].order = i;
            data.hierarchyVersion = 1;
        }
        public static void Remove(EditorDocument data, string id)
        {
            var removed = data.zones.Find(a => a.id == id) ?? throw new InvalidDataException("Area not found.");
            foreach (var child in Children(data, id)) Move(data, child.id, removed.parentZoneId);
            foreach (var spot in data.hotspots.Where(s => s.zoneId == id)) spot.zoneId = removed.parentZoneId;
            data.zones.Remove(removed); data.hierarchyVersion = 1;
        }
    }
}
