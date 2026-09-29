using UnityEngine;
using UnityEngine.Events;

namespace Rubber.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class VillaSlidingDoor : MonoBehaviour
    {
        public enum OpeningStyle { Sliding, Hinged }
        public OpeningStyle openingStyle;
        [Range(-150, 150)] public float openingAngle = -90;
        [Min(.01f)] public float width = 1;
        [Range(0, 2)] public float travelRatio = 2f / 3f;
        [Min(.1f)] public float duration = 2;
        public Transform companion;
        public UnityEvent onOpening = new UnityEvent(), onOpened = new UnityEvent();
        public UnityEvent onClosing = new UnityEvent(), onClosed = new UnityEvent();
        public bool TargetOpen { get; private set; }
        public float Progress { get; private set; }
        public bool IsMoving => enabled;
        public Vector3 ClosedCenter => transform.parent ? transform.parent.TransformPoint(closedCenter) : closedCenter;
        public Vector3 closedCenter;
        Rigidbody body;
        Vector3 closedPosition, companionPosition, localSlide;
        Quaternion closedRotation;
        bool initialized;
        void Awake() { Initialize(); enabled = false; }
        void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody>();
            closedPosition = transform.localPosition;
            closedRotation = transform.localRotation;
            localSlide = transform.localRotation * Vector3.left * (width * travelRatio);
            if (companion) companionPosition = companion.localPosition;
            initialized = true;
        }
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
        public void Toggle() => SetOpen(!TargetOpen);
        public void SetOpen(bool open)
        {
            if (!Application.isPlaying) return;
            Initialize();
            if (TargetOpen == open) return;
            TargetOpen = open; enabled = true;
            if (open) onOpening.Invoke(); else onClosing.Invoke();
        }
        void FixedUpdate()
        {
            Progress = Mathf.MoveTowards(Progress, TargetOpen ? 1 : 0, Time.fixedDeltaTime / Mathf.Max(.1f, duration));
            float eased = Mathf.SmoothStep(0, 1, Progress);
            var offset = openingStyle == OpeningStyle.Sliding ? localSlide * eased : Vector3.zero;
            var position = closedPosition + offset;
            body.MovePosition(transform.parent ? transform.parent.TransformPoint(position) : position);
            if (openingStyle == OpeningStyle.Hinged)
            {
                var rotation = closedRotation * Quaternion.Euler(0, openingAngle * eased, 0);
                body.MoveRotation(transform.parent ? transform.parent.rotation * rotation : rotation);
            }
            if (companion && openingStyle == OpeningStyle.Sliding) companion.localPosition = companionPosition + offset;
            if (Progress != (TargetOpen ? 1 : 0)) return;
            enabled = false;
            if (TargetOpen) onOpened.Invoke(); else onClosed.Invoke();
        }
    }
}
