using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public sealed class EquipmentTarget : MonoBehaviour { public string EquipmentId { get; internal set; } }
    public sealed class BedHandsetKey : MonoBehaviour { public EquipmentAction Action { get; internal set; } }
    public sealed class EquipmentRig : MonoBehaviour
    {
        sealed class Binding
        {
            public EquipmentDefinition Definition;
            public Transform Motion, Auxiliary;
            public Quaternion Rotation;
            public Vector3 AuxiliaryPosition;
            public EquipmentDisplay Display;
            public Renderer Indicator;
            public Material IndicatorMaterial;
        }
        readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
        public EquipmentSession Session { get; private set; }
        public IReadOnlyList<EquipmentDefinition> Definitions { get; private set; }
        public static EquipmentRig Create(GameObject root, List<EquipmentDefinition> definitions, IReadOnlyDictionary<string, Transform> nodes)
        {
            var rig = root.AddComponent<EquipmentRig>();
            rig.Definitions = definitions.AsReadOnly();
            Transform Node(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                if (!nodes.TryGetValue(name, out var node)) throw new InvalidDataException("Missing or ambiguous equipment part: " + name);
                return node;
            }
            foreach (var d in definitions)
            {
                var target = Node(d.nodeName).gameObject.AddComponent<EquipmentTarget>(); target.EquipmentId = d.id;
                if (d.kind == "bed")
                {
                    // Older and imported beds can still use the inspector without a handset.
                    BindHandsetKey(target.transform.Find("NR_Bed_Controls/NR_Bed_Raise"), EquipmentAction.RaiseBackrest);
                    BindHandsetKey(target.transform.Find("NR_Bed_Controls/NR_Bed_Lower"), EquipmentAction.LowerBackrest);
                }
                var motion = Node(d.motionNode); var auxiliary = Node(d.auxiliaryNode);
                var b = new Binding { Definition = d, Motion = motion, Auxiliary = auxiliary,
                    Rotation = motion != null ? motion.localRotation : Quaternion.identity,
                    AuxiliaryPosition = auxiliary != null ? auxiliary.localPosition : Vector3.zero };
                if (!string.IsNullOrEmpty(d.screenNode)) b.Display = EquipmentDisplay.Create(Node(d.screenNode).gameObject, d.kind);
                if (d.kind == "call" && auxiliary != null)
                {
                    b.Indicator = auxiliary.GetComponent<Renderer>();
                    b.IndicatorMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Nurse-call indicator" };
                    b.Indicator.sharedMaterial = b.IndicatorMaterial;
                }
                rig.bindings.Add(d.id, b);
            }
            rig.Session = new EquipmentSession(definitions);
            rig.StepVisuals(1);
            return rig;
        }
        static void BindHandsetKey(Transform key, EquipmentAction action)
        {
            if (key != null) key.gameObject.AddComponent<BedHandsetKey>().Action = action;
        }
        public string FindEquipment(Transform node)
        {
            var target = node != null ? node.GetComponentInParent<EquipmentTarget>() : null;
            return target != null ? target.EquipmentId : null;
        }
        public void StepVisuals(float dt)
        {
            if (Session == null) return;
            foreach (var b in bindings.Values)
            {
                var s = Session.Read(b.Definition.id);
                if (b.Motion != null)
                {
                    var offset = b.Definition.kind == "bed" ? Quaternion.Euler(s.BackrestDegrees, 0, 0) : Quaternion.Euler(0, s.DoorOpen ? -95 : 0, 0);
                    b.Motion.localRotation = Quaternion.RotateTowards(b.Motion.localRotation, b.Rotation * offset, 110 * Mathf.Max(0, dt));
                }
                if (b.Definition.kind == "bed" && b.Auxiliary != null)
                    b.Auxiliary.localPosition = Vector3.MoveTowards(b.Auxiliary.localPosition, b.AuxiliaryPosition + Vector3.down * (s.RailLowered ? .31f : 0), Mathf.Max(0, dt));
                if (b.Display != null) b.Display.SetState(s);
                if (b.Indicator != null)
                {
                    var color = s.CallActive ? new Color(1, .44f, .05f) : new Color(.04f, .3f, .4f);
                    b.IndicatorMaterial.SetColor("_BaseColor", color);
                }
            }
        }
        void Update() => StepVisuals(Time.unscaledDeltaTime);
        void OnDestroy() { foreach (var b in bindings.Values) if (b.IndicatorMaterial != null) Destroy(b.IndicatorMaterial); }
    }
}
