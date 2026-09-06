using UnityEngine;

namespace REDIZIT.RUI
{
    public class ScrollView : CanvasComponent
    {
        public float scrollSpeed = 25f;
        public float scrollPosition = 0f;

        [WireIgnore] private CanvasElement contentElement;

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

            float viewportHeight = Transform.calculatedSize.y;
            float contentHeight = contentElement.transform.calculatedSize.y;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

            scrollPosition += -scrollDelta * scrollSpeed;
            scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);
        }

        private void ApplyPosition()
        {
	        float viewportHeight = Transform.calculatedSize.y;
	        float contentHeight = contentElement.transform.calculatedSize.y;
	        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

	        scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);

	        // Когда скролл 0, низ контента находится в (viewportHeight - contentHeight)
	        // Когда мы крутим колесико, контент уезжает вверх (+scrollPosition)
	        float targetY = (viewportHeight - contentHeight) + scrollPosition;

	        if (Mathf.Abs(contentElement.transform.localPos.y - targetY) > 0.001f)
	        {
		        contentElement.transform.localPos.y = targetY;
		        MarkDirty();
	        }
        }
    }
}