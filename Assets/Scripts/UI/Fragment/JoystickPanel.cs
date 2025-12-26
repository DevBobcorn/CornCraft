using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace CraftSharp.UI
{
    /// <summary>
    /// UI control that turns mouse/touch drags into a 2D movement vector (dir + magnitude).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class JoystickPanel : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform handle;
        [SerializeField] private Camera uiCameraOverride;
        
        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_controlPathInternal;

        [Header("Behavior")]
        [SerializeField] [Min(1F)] private float maxRadius = 120F;
        [SerializeField] [Range(0F, 1F)] private float deadZone = 0.1F;

        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }
        
        protected override string controlPathInternal
        {
            get => m_controlPathInternal;
            set => m_controlPathInternal = value;
        }

        private RectTransform rectTransform;
        private Canvas parentCanvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            ResetStick();

            if (Keyboard.current != null)
            {
                gameObject.SetActive(false);
            }
        }

        private Camera ResolveCamera()
        {
            if (uiCameraOverride) return uiCameraOverride;
            if (!parentCanvas) return null;
            return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            if (TryGetLocalPoint(eventData, out var localPoint))
            {
                UpdateValue(localPoint);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld) return;
            if (TryGetLocalPoint(eventData, out var localPoint))
            {
                UpdateValue(localPoint);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            ResetStick();
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                           eventData.position,
                                                                           ResolveCamera(),
                                                                           out localPoint);
        }

        private void UpdateValue(Vector2 currentLocalPoint)
        {
            var rect = rectTransform.rect;
            // Keep pointer inside panel bounds
            var clampedPoint = new Vector2(
                Mathf.Clamp(currentLocalPoint.x, rect.xMin, rect.xMax),
                Mathf.Clamp(currentLocalPoint.y, rect.yMin, rect.yMax)
            );

            // Offset relative to center
            var offset = clampedPoint - rectTransform.rect.center;

            // Optional radial clamp to maxRadius to cap magnitude
            if (offset.sqrMagnitude > maxRadius * maxRadius)
            {
                offset = offset.normalized * maxRadius;
            }

            var newValue = offset; // Raw, not normalized

            // Dead zone expressed as fraction of maxRadius
            if (maxRadius > 0F && (newValue.magnitude / maxRadius) < deadZone)
            {
                newValue = Vector2.zero;
            }

            Value = newValue;
            
            SendValueToControl(Value);

            if (handle)
            {
                handle.anchoredPosition = offset;
            }
        }

        private void ResetStick()
        {
            Value = Vector2.zero;
            
            SendValueToControl(Value);

            if (handle)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
