using UnityEngine;

namespace CometaChampions.Runtime.Cards
{
    /// <summary>
    /// Automatically scales the 750x1050 reference card layout to fit any parent RectTransform.
    /// </summary>
    [ExecuteAlways]
    public class DynamicCardAutoScaler : MonoBehaviour
    {
        [SerializeField] private RectTransform referenceRoot;

        public void Initialize(RectTransform refRoot)
        {
            referenceRoot = refRoot;
            UpdateScale();
        }

        private void OnEnable()
        {
            UpdateScale();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateScale();
        }

        private void LateUpdate()
        {
            UpdateScale();
        }

        public void UpdateScale()
        {
            if (referenceRoot == null) return;
            var parentRect = transform as RectTransform;
            if (parentRect == null) return;

            Rect r = parentRect.rect;
            float w = r.width;
            float h = r.height;

            if (w <= 1f || h <= 1f)
            {
                w = parentRect.sizeDelta.x;
                h = parentRect.sizeDelta.y;
            }

            if (w <= 1f || h <= 1f) return;

            float sx = w / DynamicCardAssembler.ReferenceWidth;
            float sy = h / DynamicCardAssembler.ReferenceHeight;
            Vector3 targetScale = new Vector3(sx, sy, 1f);
            if ((referenceRoot.localScale - targetScale).sqrMagnitude > 0.000001f)
            {
                referenceRoot.localScale = targetScale;
            }
        }
    }
}
