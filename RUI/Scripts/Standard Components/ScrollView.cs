using UnityEngine;

namespace InGame.UI
{
    public class ScrollView : CanvasComponent
    {
        public float scrollSpeed = 25f;
        public float scrollPosition = 0f;

        public CanvasElement contentElement;

        public override void OnAttached()
        {
            base.OnAttached();
            EnsureComponent<Mask>();
        }

        public override void Update()
        {
            if (Element == null) return;

            // Находим контент (первый дочерний элемент)
            if (contentElement == null && Element.children.Count > 0)
            {
                contentElement = Element.children[0];
            }

            if (contentElement == null) return;

            HandleInput();
            ApplyPosition();
        }

        private void HandleInput()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector4 bounds = Element.GetScreenBounds();

            bool isHovered = mousePos.x >= bounds.x && mousePos.x <= bounds.z &&
                             mousePos.y >= bounds.y && mousePos.y <= bounds.w;

            if (!isHovered) return;

            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) < 0.001f) return;

            float viewportHeight = Transform.size.y;
            float contentHeight = contentElement.transform.size.y;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

            scrollPosition += -scrollDelta * scrollSpeed;
            scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);
        }

        private void ApplyPosition()
        {
            float viewportHeight = Transform.size.y;
            float contentHeight = contentElement.transform.size.y;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

            // Клампим на случай, если количество элементов уменьшилось динамически
            scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);

            // Универсальная формула верхней границы для любого pivot.y:
            // Если pivot.y == 1, baseY = viewportHeight
            // Если pivot.y == 0, baseY = viewportHeight - contentHeight
            float baseY = viewportHeight - contentHeight * (1f - contentElement.transform.pivot.y);
            float targetY = baseY + scrollPosition;

            // Применяем позицию сразу же на первом кадре, даже если колесико мыши не двигали!
            if (Mathf.Abs(contentElement.transform.localPos.y - targetY) > 0.001f)
            {
                contentElement.transform.localPos.y = targetY;
                MarkDirty();
            }
        }
    }
}