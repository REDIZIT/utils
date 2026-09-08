using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Anchor_Composer : CanvasComponent, IMeasurable, IComposer
	{
		public DesiredSize Measure(SizeConstraints constraints)
		{
			float maxAvailableW = 0f;
			float maxAvailableH = 0f;
			bool hasMaxW = constraints.x.TryGetMax(out maxAvailableW);
			bool hasMaxH = constraints.y.TryGetMax(out maxAvailableH);

			float maxChildW = 0f;
			float maxChildH = 0f;

			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;

				Anchor anchor = child.TryGetComponent<Anchor>();
				SizeConstraints childConstraints = constraints;

				// Если у ребенка есть Anchor, вычисляем жесткие ограничения под него
				if (anchor != null)
				{
					// По оси X:
					if (anchor.width.HasValue)
					{
						childConstraints.x = AxisConstraints.Equal(anchor.width.Value);
					}
					else if (anchor.left.HasValue && anchor.right.HasValue && hasMaxW)
					{
						float w = math.max(0f, maxAvailableW - anchor.left.Value - anchor.right.Value);
						childConstraints.x = AxisConstraints.Equal(w);
					}
					else if (anchor.left.HasValue || anchor.right.HasValue)
					{
						float padX = (anchor.left ?? 0f) + (anchor.right ?? 0f);
						childConstraints.x.Shrink(padX);
					}

					// По оси Y:
					if (anchor.height.HasValue)
					{
						childConstraints.y = AxisConstraints.Equal(anchor.height.Value);
					}
					else if (anchor.top.HasValue && anchor.bottom.HasValue && hasMaxH)
					{
						float h = math.max(0f, maxAvailableH - anchor.top.Value - anchor.bottom.Value);
						childConstraints.y = AxisConstraints.Equal(h);
					}
					else if (anchor.top.HasValue || anchor.bottom.HasValue)
					{
						float padY = (anchor.top ?? 0f) + (anchor.bottom ?? 0f);
						childConstraints.y.Shrink(padY);
					}
				}

				DesiredSize childSize = child.Measure(childConstraints);
				maxChildW = math.max(maxChildW, childSize.x);
				maxChildH = math.max(maxChildH, childSize.y);
			}

			// Как оверлейный контейнер, стремимся занять максимум доступного места
			float desiredW = hasMaxW ? maxAvailableW : maxChildW;
			float desiredH = hasMaxH ? maxAvailableH : maxChildH;

			return constraints.Clamp(new DesiredSize(desiredW, desiredH));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;

				Anchor a = child.TryGetComponent<Anchor>();

				// По умолчанию (если нет Anchor) — элемент занимает весь экран (как ContextMenus)
				float x = 0f;
				float w = finalRect.size.x;
				float y = 0f;
				float h = finalRect.size.y;

				if (a != null)
				{
					// 1. Горизонтальная ось (X и Width)
					if (a.left.HasValue && a.right.HasValue)
					{
						x = a.left.Value;
						w = math.max(0f, finalRect.size.x - a.left.Value - a.right.Value);
					}
					else if (a.left.HasValue && a.width.HasValue)
					{
						x = a.left.Value;
						w = a.width.Value;
					}
					else if (a.right.HasValue && a.width.HasValue)
					{
						w = a.width.Value;
						x = finalRect.size.x - a.right.Value - w;
					}
					else if (a.left.HasValue)
					{
						x = a.left.Value;
						w = a.width ?? child.DesiredSize.x;
					}
					else if (a.right.HasValue)
					{
						w = a.width ?? child.DesiredSize.x;
						x = finalRect.size.x - a.right.Value - w;
					}
					else if (a.width.HasValue)
					{
						x = 0f;
						w = a.width.Value;
					}

					// 2. Вертикальная ось (Y и Height)
					if (a.top.HasValue && a.bottom.HasValue)
					{
						y = a.top.Value;
						h = math.max(0f, finalRect.size.y - a.top.Value - a.bottom.Value);
					}
					else if (a.top.HasValue && a.height.HasValue)
					{
						y = a.top.Value;
						h = a.height.Value;
					}
					else if (a.bottom.HasValue && a.height.HasValue)
					{
						h = a.height.Value;
						y = finalRect.size.y - a.bottom.Value - h;
					}
					else if (a.top.HasValue)
					{
						y = a.top.Value;
						h = a.height ?? child.DesiredSize.y;
					}
					else if (a.bottom.HasValue)
					{
						h = a.height ?? child.DesiredSize.y;
						y = finalRect.size.y - a.bottom.Value - h;
					}
					else if (a.height.HasValue)
					{
						y = 0f;
						h = a.height.Value;
					}
				}

				child.Arrange(new ArrangeRect(new float2(x, y), new float2(w, h)));
			}
		}
	}
}