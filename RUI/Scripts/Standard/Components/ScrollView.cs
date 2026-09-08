using System.Linq;
using UnityEngine;

namespace REDIZIT.RUI
{
    // public class ScrollView : CanvasComponent, IPointerScrollHandler, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler, ILayoutSolver
    public class ScrollView : CanvasComponent, IPointerScrollHandler, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler
    {
        public float speed = 48f;
        public float scrollPosition = 0f;

        [WireIgnore] private CanvasElement contentElement;

        private readonly DragGestureRecognizer dragRecognizer = new();

        public override void OnAttached()
        {
            base.OnAttached();
            EnsureComponent<Mask>();

            dragRecognizer.onDragUpdate = (delta) =>
            {
                AddScroll(delta.y);
            };
        }

        public void OnPointerScroll(PointerScrollEvent e)
        {
            AddScroll(-e.scrollDelta.y * speed);
        }

        public void OnPointerDown(PointerDownEvent e, GestureArena arena)
        {
            if (e.button == 0) dragRecognizer.OnPointerDown(e, arena);
        }

        public void OnPointerMove(PointerMoveEvent e, GestureArena arena)
        {
            dragRecognizer.OnPointerMove(e, arena);
        }

        public void OnPointerUp(PointerUpEvent e, GestureArena arena)
        {
            if (e.button == 0) dragRecognizer.OnPointerUp(e, arena);
        }

        private void AddScroll(float delta)
        {
            if (contentElement == null && Element.Children.Count > 0)
                contentElement = Element.Children.First();
            
            if (contentElement == null) return;
            
            
            float viewportHeight = Transform.size.y;
            float contentHeight = contentElement.transform.size.y;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            
            scrollPosition = Mathf.Clamp(scrollPosition + delta, 0f, maxScroll);
            ApplyPosition();
            
            MarkDirty();
        }

        public void ApplyPosition()
        {
            if (contentElement == null && Element.Children.Count > 0)
                contentElement = Element.Children.First();
            
            if (contentElement == null) return;
            
            float viewportHeight = Transform.size.y;
            float contentHeight = contentElement.transform.size.y;
            float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);
            
            float targetY = (viewportHeight - contentHeight) + scrollPosition;
            if (Mathf.Abs(contentElement.transform.pos.y - targetY) > 0.001f)
            {
                contentElement.transform.pos.y = targetY;
            }
        }

        // public void Solve(SizeConstraints constraints)
        // {
	       //  // Debug.Log($"Scroll constraints: {constraints}");
        //
	       //  SizeConstraints contentConstraints = new()
	       //  {
		      //   maxX = Transform.size.x,
		      //   maxY = null
	       //  };
	       //  // Debug.Log($"Scroll content constraints: {contentConstraints}");
	       //  
	       //  Element.SolveChildren(contentConstraints);
	       //  ApplyPosition();
        // }
    }
}