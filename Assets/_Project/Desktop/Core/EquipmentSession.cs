using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkplaceStudio.Core
{
    [Serializable] public sealed class EquipmentDefinition
    {
        public string id, title, kind, nodeName, screenNode, motionNode, auxiliaryNode;
    }
    public enum EquipmentAction { TogglePower, ToggleRun, ToggleDoor, RaiseBackrest, LowerBackrest, ToggleRail, Call, Acknowledge, Reset }
    public sealed class EquipmentState
    {
        public bool Powered { get; internal set; }
        public bool Running { get; internal set; }
        public bool DoorOpen { get; internal set; }
        public bool RailLowered { get; internal set; }
        public bool CallActive { get; internal set; }
        public int BackrestDegrees { get; internal set; }
        public EquipmentState Snapshot() => (EquipmentState)MemberwiseClone();
    }
    public sealed class EquipmentEvent
    {
        public string EquipmentId { get; }
        public EquipmentAction Action { get; }
        public EquipmentState Before { get; }
        public EquipmentState After { get; }
        public long Sequence { get; }
        internal EquipmentEvent(string id, EquipmentAction action, EquipmentState before, EquipmentState after, long sequence)
        { EquipmentId = id; Action = action; Before = before; After = after; Sequence = sequence; }
    }
    // No GameObjects, input devices or UI dependencies. Future lessons subscribe to
    // Performed and evaluate stable equipment IDs/actions rather than button labels.
    public sealed class EquipmentSession
    {
        readonly Dictionary<string, EquipmentDefinition> definitions;
        readonly Dictionary<string, EquipmentState> states;
        long sequence;
        public event Action<EquipmentEvent> Performed;
        public EquipmentSession(IEnumerable<EquipmentDefinition> equipment)
        {
            definitions = equipment.ToDictionary(d => d.id, StringComparer.Ordinal);
            states = definitions.ToDictionary(p => p.Key, p => Initial(p.Value), StringComparer.Ordinal);
        }
        static EquipmentState Initial(EquipmentDefinition d) => new EquipmentState { Powered = d.kind == "monitor", BackrestDegrees = d.kind == "bed" ? 15 : 0 };
        public EquipmentState Read(string id) => states.TryGetValue(id, out var state) ? state.Snapshot() : null;
        public bool Execute(string id, EquipmentAction action, out string feedback)
        {
            feedback = "This control is not available.";
            if (!definitions.TryGetValue(id, out var definition)) return false;
            var state = states[id]; var before = state.Snapshot();
            switch (action)
            {
                case EquipmentAction.TogglePower:
                    if (definition.kind != "monitor" && definition.kind != "pump") return false;
                    state.Powered = !state.Powered; if (!state.Powered) state.Running = false; break;
                case EquipmentAction.ToggleRun:
                    if (definition.kind != "pump") return false;
                    if (!state.Powered) { feedback = "Switch on the pump first."; return false; }
                    if (state.DoorOpen) { feedback = "Close the pump door before starting the demo."; return false; }
                    state.Running = !state.Running; break;
                case EquipmentAction.ToggleDoor:
                    if (definition.kind != "pump") return false;
                    if (state.Running) { feedback = "Pause the demo before opening the door."; return false; }
                    state.DoorOpen = !state.DoorOpen; break;
                case EquipmentAction.RaiseBackrest:
                case EquipmentAction.LowerBackrest:
                    if (definition.kind != "bed") return false;
                    int angle = Math.Max(0, Math.Min(60, state.BackrestDegrees + (action == EquipmentAction.RaiseBackrest ? 15 : -15)));
                    if (angle == state.BackrestDegrees) { feedback = "The backrest has reached its practice limit."; return false; }
                    state.BackrestDegrees = angle; break;
                case EquipmentAction.ToggleRail:
                    if (definition.kind != "bed") return false;
                    state.RailLowered = !state.RailLowered; break;
                case EquipmentAction.Call:
                case EquipmentAction.Acknowledge:
                    if (definition.kind != "call") return false;
                    bool active = action == EquipmentAction.Call;
                    if (state.CallActive == active) { feedback = active ? "The call is already active." : "There is no active call."; return false; }
                    state.CallActive = active; break;
                case EquipmentAction.Reset: states[id] = Initial(definition); state = states[id]; break;
                default: return false;
            }
            feedback = definition.title + " updated.";
            Performed?.Invoke(new EquipmentEvent(id, action, before, state.Snapshot(), ++sequence));
            return true;
        }
    }
}
