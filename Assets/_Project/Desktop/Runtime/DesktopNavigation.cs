using System;
using UnityEngine;
using UnityEngine.InputSystem;
using WorkplaceStudio.Core;
using Pose = WorkplaceStudio.Core.Pose;

namespace WorkplaceStudio.Desktop
{
    public enum DesktopMoveResult { Completed, Blocked, Cancelled }
    public sealed class DesktopNavigation : MonoBehaviour
    {
        public Camera View { get; private set; }
        public bool Captured => Cursor.lockState == CursorLockMode.Locked;
        public bool Moving { get; private set; }
        bool spectator, walkingNeedsGrounding = true;
        public bool Spectator
        {
            get => spectator;
            set { if (spectator != value) { spectator = value; walkingNeedsGrounding = true; vertical = 0; } }
        }
        public bool Ready { get; set; }
        public event Action Released;
        CharacterController body;
        float yaw, pitch, vertical;
        public float EyeHeight { get; private set; } = 1.65f;
        public float MovementSpeed { get; private set; } = .8f;
        Pose destination;
        Vector3 startPosition;
        Quaternion startRotation;
        float startFov, progress;
        Action<DesktopMoveResult> moveCompleted;

        public void Initialize()
        {
            gameObject.layer = 31;
            body = gameObject.AddComponent<CharacterController>();
            body.height = 1.7f; body.radius = .18f; body.center = new Vector3(0, .85f, 0); body.skinWidth = .02f; body.stepOffset = .2f;
            var cameraObject = new GameObject("Desktop camera"); cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = Vector3.up * EyeHeight;
            View = cameraObject.AddComponent<Camera>(); View.tag = "MainCamera"; View.nearClipPlane = .02f; View.farClipPlane = 200;
            View.backgroundColor = new Color(.05f, .06f, .08f); View.clearFlags = CameraClearFlags.SolidColor;
            View.cullingMask = ~(1 << 30);
        }
        public void ConfigureForEnvironment(string environmentId)
        {
            CancelMove();
            var eye = View.transform.position;
            bool hospital = environmentId == "charite-operating-room";
            EyeHeight = hospital ? 2.4f : 1.65f;
            MovementSpeed = hospital ? 2.4f : .8f;
            body.enabled = false;
            body.height = EyeHeight + .05f;
            body.center = Vector3.up * (body.height * .5f);
            View.transform.localPosition = Vector3.up * EyeHeight;
            transform.position = eye - Vector3.up * EyeHeight;
            vertical = 0; walkingNeedsGrounding = true; body.enabled = true;
        }
        public Pose Capture() => new Pose { position = Coordinates.ToSource(View.transform.position), target = Coordinates.ToSource(View.transform.position + View.transform.forward), fovDegrees = View.fieldOfView };
        public void Apply(Pose pose)
        {
            CancelMove(); body.enabled = false;
            transform.position = Coordinates.ToUnity(pose.position) - Vector3.up * EyeHeight;
            var direction = Coordinates.ToUnity(pose.target) - View.transform.position;
            var look = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles;
            yaw = look.y; pitch = Mathf.Clamp(Mathf.DeltaAngle(0, look.x), -85, 85);
            transform.rotation = Quaternion.Euler(0, yaw, 0); View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            View.fieldOfView = pose.fovDegrees; vertical = 0; walkingNeedsGrounding = true; body.enabled = true;
        }
        public void CapturePointer()
        {
            if (!Ready || Moving) return;
            if (!Spectator) PrepareWalking();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        public void ReleasePointer()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; vertical = 0; Released?.Invoke();
        }
        public void MoveTo(Pose pose, Action<DesktopMoveResult> completed)
        {
            CancelMove(); ReleasePointer();
            Vector3 to = Coordinates.ToUnity(pose.position), from = View.transform.position;
            Vector3 delta = to - from;
            // Preserve the existing editor's blocked-path behavior; no teleport through equipment.
            if (delta.magnitude > .03f)
                foreach (var offset in new[] { Vector3.zero, Vector3.right * .08f, Vector3.left * .08f, Vector3.up * .08f, Vector3.down * .08f })
                    if (Physics.Raycast(from + offset, delta.normalized, delta.magnitude, ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore)) { completed(DesktopMoveResult.Blocked); return; }
            startPosition = from; startRotation = View.transform.rotation; startFov = View.fieldOfView;
            destination = pose; progress = 0; Moving = true; body.enabled = false; moveCompleted = completed;
        }
        public void CancelMove()
        {
            if (!Moving) return;
            Moving = false; walkingNeedsGrounding = true; if (body != null) body.enabled = true;
            var callback = moveCompleted; moveCompleted = null; callback?.Invoke(DesktopMoveResult.Cancelled);
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { CancelMove(); ReleasePointer(); }
            if (Moving)
            {
                progress = Mathf.Min(1, progress + Time.unscaledDeltaTime / .8f);
                float t = progress * progress * (3 - 2 * progress);
                Vector3 eye = Vector3.Lerp(startPosition, Coordinates.ToUnity(destination.position), t);
                transform.position = eye - Vector3.up * EyeHeight;
                var targetRotation = Quaternion.LookRotation(Coordinates.ToUnity(destination.target) - Coordinates.ToUnity(destination.position), Vector3.up);
                var angles = Quaternion.Slerp(startRotation, targetRotation, t).eulerAngles;
                yaw = angles.y; pitch = Mathf.Clamp(Mathf.DeltaAngle(0, angles.x), -85, 85);
                transform.rotation = Quaternion.Euler(0, yaw, 0); View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                View.fieldOfView = Mathf.Lerp(startFov, destination.fovDegrees, t);
                if (progress >= 1) { Moving = false; walkingNeedsGrounding = true; body.enabled = true; var callback = moveCompleted; moveCompleted = null; callback?.Invoke(DesktopMoveResult.Completed); }
                return;
            }
            if (!Ready || !Captured || keyboard == null || !Application.isFocused) return;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var movement = mouse.delta.ReadValue(); yaw += movement.x * .12f; pitch = Mathf.Clamp(pitch - movement.y * .12f, -85, 85);
                transform.rotation = Quaternion.Euler(0, yaw, 0); View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            var direction = transform.right * x + (Spectator ? View.transform.forward : transform.forward) * z;
            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool control = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (Spectator) direction += Vector3.up * ((shift ? 1 : 0) - (control ? 1 : 0));
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (Spectator) { body.enabled = false; transform.position += Vector3.ClampMagnitude(direction, 1) * MovementSpeed * dt; body.enabled = true; }
            else MoveWalking(direction, shift, dt);
        }
        void PrepareWalking()
        {
            if (!walkingNeedsGrounding) return;
            walkingNeedsGrounding = false; vertical = 0;
            // Saved poses describe the eye, not the feet. A taller walking profile can
            // otherwise spawn the capsule below a single-sided scanned floor.
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(View.transform.position, Vector3.down, EyeHeight * 4,
                ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            RaycastHit floor = default;
            float minNormal = Mathf.Cos(body.slopeLimit * Mathf.Deg2Rad);
            foreach (var hit in hits)
                if (hit.normal.y >= minNormal && hit.distance < nearest) { nearest = hit.distance; floor = hit; }
            if (float.IsPositiveInfinity(nearest)) return;
            body.enabled = false;
            var feet = transform.position; feet.y = floor.point.y + body.skinWidth;
            transform.position = feet; body.enabled = true;
        }
        // Shared walking step for desktop input and future device adapters.
        public void MoveWalking(Vector3 direction, bool faster, float deltaTime)
        {
            if (!Ready || Moving || Spectator) return;
            PrepareWalking();
            float dt = Mathf.Clamp(deltaTime, 0, .05f);
            direction = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(direction, Vector3.up), 1);
            vertical = body.isGrounded ? -1 : Mathf.Max(-12, vertical - 9.81f * dt);
            var collisions = body.Move((direction * MovementSpeed * (faster ? 2 : 1) + Vector3.up * vertical) * dt);
            if ((collisions & CollisionFlags.Below) != 0) vertical = -1;
        }
        void OnApplicationFocus(bool focused) { if (!focused) { CancelMove(); ReleasePointer(); } }
        void OnDisable() { CancelMove(); ReleasePointer(); }
    }
}
