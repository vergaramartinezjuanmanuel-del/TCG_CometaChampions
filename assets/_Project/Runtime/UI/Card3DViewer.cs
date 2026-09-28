using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CometaChampions.Runtime.UI
{
    /// <summary>
    /// Orbit controller for 3D card presentation with free 360° rotation,
    /// touch/mouse drag inertia, 180° flip animation, turntable auto-spin,
    /// and subtle cursor parallax tilt.
    /// </summary>
    public class Card3DViewer : MonoBehaviour
    {
        [Header("Target Card")]
        [SerializeField] private Transform targetCardTransform;

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

        public bool IsAutoSpinning => isAutoSpinning;
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
            // Do not start drag if user clicks on UI elements (like buttons or tabs)
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                isDragging = true;
                lastMousePosition = Input.mousePosition;
                angularVelocity = Vector2.zero;

                if (activeAnimationRoutine != null)
                {
                    StopCoroutine(activeAnimationRoutine);
                    activeAnimationRoutine = null;
                }
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                Vector2 currentPos = Input.mousePosition;
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

            // Optional subtle idle parallax tilt following mouse cursor
            Quaternion finalRotation = baseRotation;
            if (enableParallaxTilt && !isDragging && !isAutoSpinning && activeAnimationRoutine == null)
            {
                Vector2 screenNorm = new Vector2(
                    (Input.mousePosition.x / Screen.width - 0.5f) * 2f,
                    (Input.mousePosition.y / Screen.height - 0.5f) * 2f
                );
                screenNorm = Vector2.ClampMagnitude(screenNorm, 1f);

                float tiltX = -screenNorm.y * maxTiltAngle;
                float tiltY = screenNorm.x * maxTiltAngle;
                Quaternion tiltRotation = Quaternion.Euler(tiltX, tiltY, 0f);

                finalRotation = baseRotation * tiltRotation;
            }

            if (targetCardTransform != null)
            {
                targetCardTransform.localRotation = Quaternion.Slerp(targetCardTransform.localRotation, finalRotation, Time.deltaTime * 24f);
            }
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
