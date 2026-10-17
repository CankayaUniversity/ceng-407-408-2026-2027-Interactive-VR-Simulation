using UnityEngine;

namespace Simulation.Interaction
{
    public struct InteractorContext
    {
        public GameObject InteractorObject;
        public Transform AttachmentPoint;
        public bool IsVRMode;
    }

    public interface IInteractable
    {
        string InteractionID { get; }
        string InteractionPrompt { get; }
        bool CanInteract(InteractorContext context);
        
        void OnHoverEnter(InteractorContext context);
        void OnHoverExit(InteractorContext context);
        void Interact(InteractorContext context);
    }

    public interface IGrabbable : IInteractable
    {
        void OnGrab(Transform holdPoint);
        void OnRelease(Vector3 throwVelocity);
        void UseEquippedItem();
    }
}
