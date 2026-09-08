using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class ScrollView : CanvasComponent, IPointerScrollHandler, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler, IMeasurable, IComposer
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
			// При скролле колесика вниз e.scrollDelta.y < 0, скролл должен увеличиваться
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

		private void EnsureContentElement()
		{
			if (contentElement == null && Element != null && Element.Children.Count > 0)
			{
				contentElement = Element.Children.First();
			}
		}

		public void AddScroll(float delta)
		{
			EnsureContentElement();
			if (contentElement == null) return;

			float viewportHeight = Transform.size.y;
			float contentHeight = contentElement.DesiredSize.y;
			float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

			if (maxScroll <= 0.001f) return;

			float newScroll = Mathf.Clamp(scrollPosition + delta, 0f, maxScroll);
			if (Mathf.Abs(newScroll - scrollPosition) > 0.001f)
			{
				scrollPosition = newScroll;
				MarkDirty(); // Запускает фазу Arrange с новой позицией скролла
			}
		}

		public DesiredSize Measure(SizeConstraints constraints)
		{
			EnsureContentElement();

			if (contentElement != null && contentElement.isEnabled)
			{
				// Главный секрет: по вертикали даем контенту полную свободу (Unlimited),
				// чтобы он честно сообщил свой полный размер со всеми 100 лотами!
				SizeConstraints contentConstraints = new()
				{
					x = constraints.x,
					y = AxisConstraints.Unlimited()
				};

				contentElement.Measure(contentConstraints);
			}

			// Сам ScrollView занимает размеры своего вьюпорта
			float w = 0f;
			float h = 0f;
			if (constraints.x.TryGetMax(out float maxW)) w = maxW;
			else if (contentElement != null) w = contentElement.DesiredSize.x;

			if (constraints.y.TryGetMax(out float maxH)) h = maxH;
			else if (contentElement != null) h = contentElement.DesiredSize.y;

			return constraints.Clamp(new DesiredSize(w, h));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			EnsureContentElement();
			if (contentElement == null || !contentElement.isEnabled) return;

			float viewportWidth = finalRect.size.x;
			float viewportHeight = finalRect.size.y;

			// Настоящая высота контента
			float contentHeight = contentElement.DesiredSize.y;

			// Ограничиваем скролл
			float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);
			scrollPosition = Mathf.Clamp(scrollPosition, 0f, maxScroll);

			// Расчет смещения в системе Y=0 снизу:
			// Верх контента при scrollPosition=0 должен быть на viewportHeight.
			// Значит низ контента = (viewportHeight - contentHeight).
			// При скролле контент едет вверх (+ scrollPosition).
			float targetY = (viewportHeight - contentHeight) + scrollPosition;

			ArrangeRect contentRect = new ArrangeRect(
				new float2(0f, targetY),
				new float2(viewportWidth, contentHeight)
			);

			// Выделяем контенту его НАСТОЯЩУЮ высоту (например, 2000px)
			contentElement.Arrange(contentRect);
		}
	}
}