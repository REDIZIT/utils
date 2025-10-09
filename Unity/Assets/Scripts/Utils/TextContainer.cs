using TMPro;
using UnityEngine;

namespace InGame
{
    [ExecuteAlways]
    public class TextContainer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        public Vector2 padding;

        [SerializeField] private bool fitWidth = true, fitHeight = true;

        [SerializeField] private bool useMinSize;
        [SerializeField] private Vector2 minSize;

        [SerializeField] private bool useMaxWidth;
        [SerializeField] private float maxWidth;

        private RectTransform rect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (text == null) return;

            Vector2 delta = rect.sizeDelta;

            if (fitWidth)
            {
                delta.x = text.preferredWidth + padding.x;
                if (useMinSize) delta.x = Mathf.Max(minSize.x, delta.x);
                if (useMaxWidth) delta.x = Mathf.Min(maxWidth, delta.x);
            }
            if (fitHeight)
            {
                delta.y = text.preferredHeight + padding.y;
                if (useMinSize) delta.y = Mathf.Max(minSize.y, delta.y);
            }

            rect.sizeDelta = delta;
        }
    }
}