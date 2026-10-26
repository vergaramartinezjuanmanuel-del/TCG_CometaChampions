using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CometaChampions.Runtime.Cards;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Orbit controller for 3D card presentation with free 360° rotation,
    /// touch/mouse drag inertia, 180° flip animation, turntable auto-spin,
    /// and subtle cursor parallax tilt restricted strictly to the card's bounds.
    /// </summary>
    public class Card3DViewer : MonoBehaviour
    {
        [Header("Target Card")]
        [SerializeField] private Transform targetCardTransform;

        [Header("Card Interaction Bounds")]
        [SerializeField] private float cardHitWidth = Card3D.CardWidth;
        [SerializeField] private float cardHitHeight = Card3D.CardHeight;

        [Header("Orbit Drag Settings")]
        [SerializeField] private float dragSensitivity = 0.35f;
        [SerializeField] private float inertiaDamping = 0.92f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Parallax Tilt (Idle)")]
        [SerializeField] private bool enableParallaxTilt = true;
        [SerializeField] private float maxTiltAngle = 8f;

        [Header("Turntable Auto-Spin")]
        [SerializeField] private bool isAutoSpinning = false;
        [SerializeField] private float autoSpinSpeed = 28f; // degrees per second

        [Header("Card Camera")]
        [SerializeField] private Camera showcaseCamera;

        // Current rotation angles
        private float currentYaw = 0f;
        private float currentPitch = 0f;
        private Vector2 angularVelocity;

        private bool isDragging = false;
        private Vector2 lastMousePosition;
        private Coroutine activeAnimationRoutine;
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>();

        public bool IsAutoSpinning => isAutoSpinning;

        public void SetParallaxTiltEnabled(bool enabled)
        {
            enableParallaxTilt = enabled;
        }
        public float CurrentYaw => currentYaw;

        private void Awake()
        {
            if (targetCardTransform == null)
            {
                targetCardTransform = transform;
            }
            if (showcaseCamera == null)
            {
                showcaseCamera = Camera.main;
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateCardRotation();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pointerPos = GetPointerScreenPosition();

                // Only start interaction if pointer is strictly inside the 3D card space and not over blocking UI
                if (IsPointerOverBlockingUI(pointerPos) || !IsPointerOverCard(pointerPos, out _))
                {
                    return;
                }

                isDragging = true;
                lastMousePosition = pointerPos;
                angularVelocity = Vector2.zero;

                if (activeAnimationRoutine != null)
                {
                    StopCoroutine(activeAnimationRoutine);
                    activeAnimationRoutine = null;
                }
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 currentPos = GetPointerScreenPosition();
                Vector2 delta = currentPos - lastMousePosition;
                lastMousePosition = currentPos;

                float yawDelta = -delta.x * dragSensitivity;
                float pitchDelta = delta.y * dragSensitivity;

                currentYaw += yawDelta;
                currentPitch = Mathf.Clamp(currentPitch + pitchDelta, minPitch, maxPitch);

                angularVelocity = new Vector2(yawDelta, pitchDelta) / Mathf.Max(Time.deltaTime, 0.001f);
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                isDragging = false;
            }
        }

        private void UpdateCardRotation()
        {
            if (isAutoSpinning && !isDragging && activeAnimationRoutine == null)
            {
                currentYaw += autoSpinSpeed * Time.deltaTime;
                currentPitch = Mathf.Lerp(currentPitch, 0f, Time.deltaTime * 3f);
            }
            else if (!isDragging && activeAnimationRoutine == null)
            {
                // Apply inertia damping
                if (angularVelocity.sqrMagnitude > 0.01f)
                {
                    currentYaw += angularVelocity.x * Time.deltaTime;
                    currentPitch = Mathf.Clamp(currentPitch + angularVelocity.y * Time.deltaTime, minPitch, maxPitch);
                    angularVelocity *= Mathf.Pow(inertiaDamping, Time.deltaTime * 60f);
                }
                else
                {
                    angularVelocity = Vector2.zero;
                }
            }

            // Normal base orientation from pitch & yaw
            Quaternion baseRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Subtle idle parallax tilt ONLY when pointer is actively over the card area
            Quaternion finalRotation = baseRotation;
            if (enableParallaxTilt && !isDragging && !isAutoSpinning && activeAnimationRoutine == null && HasActiveHoverPointer(out Vector2 hoverPos))
            {
                if (!IsPointerOverBlockingUI(hoverPos) && IsPointerOverCard(hoverPos, out Vector2 cardNorm))
                {
                    float tiltX = -cardNorm.y * maxTiltAngle;
                    float tiltY = cardNorm.x * maxTiltAngle;
                    Quaternion tiltRotation = Quaternion.Euler(tiltX, tiltY, 0f);

                    finalRotation = baseRotation * tiltRotation;
                }
            }

            if (targetCardTransform != null)
            {
                targetCardTransform.localRotation = Quaternion.Slerp(targetCardTransform.localRotation, finalRotation, Time.deltaTime * 24f);
            }
        }

        private static Vector2 GetPointerScreenPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            return Input.mousePosition;
        }

        private static bool HasActiveHoverPointer(out Vector2 screenPos)
        {
            if (Input.touchCount > 0)
            {
                screenPos = Input.GetTouch(0).position;
                return true;
            }

            if (Application.isMobilePlatform)
            {
                screenPos = Vector2.zero;
                return false;
            }

            screenPos = Input.mousePosition;
            return screenPos.x >= 0f && screenPos.x <= Screen.width &&
                   screenPos.y >= 0f && screenPos.y <= Screen.height;
        }

        private bool IsPointerOverBlockingUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            UiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, UiRaycastResults);

            for (int i = 0; i < UiRaycastResults.Count; i++)
            {
                var hitGo = UiRaycastResults[i].gameObject;
                if (hitGo == null) continue;

                // Ignore world-space canvas elements attached directly to the 3D card itself
                if (targetCardTransform != null && hitGo.transform.IsChildOf(targetCardTransform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given screen position falls strictly within the 3D space occupied by the card
        /// in Center_Card3D_Viewport.
        /// </summary>
        public bool IsPointerOverCard(Vector2 screenPos, out Vector2 normalizedCardOffset)
        {
            normalizedCardOffset = Vector2.zero;

            var cam = showcaseCamera != null ? showcaseCamera : Camera.main;
            var cardTrans = targetCardTransform != null ? targetCardTransform : transform;
            if (cam == null || cardTrans == null) return false;

            if (screenPos.x < 0f || screenPos.x > Screen.width || screenPos.y < 0f || screenPos.y > Screen.height)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(screenPos);
            Vector3 cardCenter = cardTrans.position;

            float halfWidth = Mathf.Max(cardHitWidth * 0.5f, 0.01f);
            float halfHeight = Mathf.Max(cardHitHeight * 0.5f, 0.01f);

            // 1. Check against the oriented card plane (when not nearly edge-on to the camera ray)
            Vector3 cardForward = cardTrans.forward;
            if (Mathf.Abs(Vector3.Dot(ray.direction, cardForward)) > 0.15f)
            {
                Plane orientedPlane = new Plane(cardForward, cardCenter);
                if (orientedPlane.Raycast(ray, out float enterOriented) && enterOriented > 0f)
                {
                    Vector3 worldHit = ray.GetPoint(enterOriented);
                    Vector3 localHit = cardTrans.InverseTransformPoint(worldHit);

                    if (Mathf.Abs(localHit.x) <= halfWidth && Mathf.Abs(localHit.y) <= halfHeight)
                    {
                        normalizedCardOffset = new Vector2(
                            Mathf.Clamp(localHit.x / halfWidth, -1f, 1f),
                            Mathf.Clamp(localHit.y / halfHeight, -1f, 1f)
                        );
                        return true;
                    }
                }
            }

            // 2. Check against the camera-facing viewport footprint centered on the card
            // (ensures the card remains grabbable within its 2.5x3.5 viewport slot even when rotated near 90° edge-on)
            Plane viewportPlane = new Plane(-cam.transform.forward, cardCenter);
            if (viewportPlane.Raycast(ray, out float enterViewport) && enterViewport > 0f)
            {
                Vector3 worldHit = ray.GetPoint(enterViewport);
                Vector3 delta = worldHit - cardCenter;

                float scaleX = Mathf.Max(Mathf.Abs(cardTrans.lossyScale.x), 0.0001f);
                float scaleY = Mathf.Max(Mathf.Abs(cardTrans.lossyScale.y), 0.0001f);

                float localX = Vector3.Dot(delta, cam.transform.right) / scaleX;
                float localY = Vector3.Dot(delta, cam.transform.up) / scaleY;

                if (Mathf.Abs(localX) <= halfWidth && Mathf.Abs(localY) <= halfHeight)
                {
                    normalizedCardOffset = new Vector2(
                        Mathf.Clamp(localX / halfWidth, -1f, 1f),
                        Mathf.Clamp(localY / halfHeight, -1f, 1f)
                    );
                    return true;
                }
            }

            return false;
        }

        public void ToggleAutoSpin()
        {
            SetAutoSpin(!isAutoSpinning);
        }

        public void SetAutoSpin(bool enable)
        {
            isAutoSpinning = enable;
            if (isAutoSpinning && activeAnimationRoutine != null)
            {
                StopCoroutine(activeAnimationRoutine);
                activeAnimationRoutine = null;
            }
        }

        public void FlipCard180()
        {
            isAutoSpinning = false;
            if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
            activeAnimationRoutine = StartCoroutine(AnimateFlipRoutine());
        }

        private IEnumerator AnimateFlipRoutine()
        {
            float startYaw = currentYaw;
            float targetYaw = currentYaw + 180f;
            float startPitch = currentPitch;
            float targetPitch = 0f;
            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                currentYaw = Mathf.Lerp(startYaw, targetYaw, t);
                currentPitch = Mathf.Lerp(startPitch, targetPitch, t);
                yield return null;
            }

            currentYaw = targetYaw;
            currentPitch = targetPitch;
            angularVelocity = Vector2.zero;
            activeAnimationRoutine = null;
        }

        public void ResetToFront()
        {
            isAutoSpinning = false;
            if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
            activeAnimationRoutine = StartCoroutine(AnimateResetRoutine());
        }

        private IEnumerator AnimateResetRoutine()
        {
            // Normalize yaw to nearest multiple of 360 facing front
            float nearestYaw = Mathf.Round(currentYaw / 360f) * 360f;
            float startYaw = currentYaw;
            float startPitch = currentPitch;
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                currentYaw = Mathf.Lerp(startYaw, nearestYaw, t);
                currentPitch = Mathf.Lerp(startPitch, 0f, t);
                yield return null;
            }

            currentYaw = 0f;
            currentPitch = 0f;
            angularVelocity = Vector2.zero;
            activeAnimationRoutine = null;
        }
    }
}
