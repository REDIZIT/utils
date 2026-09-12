using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasInputManager
    {
        public readonly GestureArenaManager arenaManager = new GestureArenaManager();
        private readonly List<CanvasElement> hoveredElements = new List<CanvasElement>();
        private readonly List<CanvasElement> currentHits = new List<CanvasElement>();
        
        public event System.Action<PointerDownEvent, List<CanvasElement>> onGlobalPointerDown;

        private Vector2 lastMousePos;

        public void ProcessInput(CanvasElement root)
        {
            if (root == null || !root.isEnabled) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 delta = mousePos - lastMousePos;
            lastMousePos = mousePos;

            // 1. Единый Hit-Test для текущего кадра
            currentHits.Clear();
            HitTest(root, mousePos, currentHits);

            // 2. Единая обработка Hover
            ProcessHover(currentHits);

            // 3. Обработка нажатий (Down) - объединяем ЛКМ и ПКМ
            for (int btn = 0; btn <= 1; btn++)
            {
                if (Input.GetMouseButtonDown(btn))
                {
                    var downEvent = new PointerDownEvent(mousePos, btn, 0);

                    // Уведомляем глобальных слушателей (например, закрытие контекстного меню)
                    onGlobalPointerDown?.Invoke(downEvent, currentHits);

                    // Открываем арену жестов
                    var arena = arenaManager.OpenArena(0);
                    bool buttonCaptured = false;
                    
                    foreach (CanvasElement hit in currentHits)
                    {
                        foreach (CanvasComponent comp in hit.Components)
                        {
                            // Ограничиваем: только самая верхняя кнопка вступает в борьбу за нажатие
                            if (comp is Button)
                            {
                                if (buttonCaptured) continue;
                                buttonCaptured = true;
                            }

                            if (comp is IPointerDownHandler handler)
                            {
                                handler.OnPointerDown(downEvent, arena);
                            }
                        }
                    }

                    // Закрываем арену для приема новых участников (теперь они должны доказывать победу)
                    arenaManager.CloseArena(0);
                }
            }

            // 4. Обработка движения (Move)
            if (delta.sqrMagnitude > 0.0001f)
            {
                var arena = arenaManager.GetArena(0);
                // Move рассылаем только если арена активна (зажата кнопка)
                if (arena != null)
                {
                    var moveEvent = new PointerMoveEvent(mousePos, delta, 0);
                    
                    foreach (CanvasElement hit in currentHits)
                    {
                        foreach (CanvasComponent comp in hit.Components)
                        {
                            if (comp is IPointerMoveHandler handler)
                            {
                                handler.OnPointerMove(moveEvent, arena);
                            }
                        }
                    }
                }
            }

            // 5. Обработка отпускания (Up)
            if (Input.GetMouseButtonUp(0))
            {
                var arena = arenaManager.GetArena(0);
                var upEvent = new PointerUpEvent(mousePos, 0, 0);
                
                foreach (CanvasElement hit in currentHits)
                {
                    foreach (CanvasComponent comp in hit.Components)
                    {
                        if (comp is IPointerUpHandler handler)
                        {
                            handler.OnPointerUp(upEvent, arena);
                        }
                    }
                }

                // Решаем исход арены (обычно здесь побеждает Tap, если не было скролла)
                arenaManager.Sweep(0);
            }

            // 6. Колесико мыши (Scroll)
            Vector2 scroll = Input.mouseScrollDelta;
            if (Mathf.Abs(scroll.y) > 0.001f || Mathf.Abs(scroll.x) > 0.001f)
            {
                var scrollEvent = new PointerScrollEvent(mousePos, scroll, 0);
                
                foreach (CanvasElement hit in currentHits)
                {
                    foreach (CanvasComponent comp in hit.Components)
                    {
                        if (comp is IPointerScrollHandler handler)
                        {
                            handler.OnPointerScroll(scrollEvent);
                            return; // Поглощаем скролл
                        }
                    }
                }
            }
        }

        private bool HitTest(CanvasElement element, Vector2 point, List<CanvasElement> results)
        {
            if (!element.isEnabled) return false;

            var mask = element.TryGetComponent<Mask>();
            if (mask != null && mask.enabled)
            {
                Rect clip = mask.GetWorldClipRect();
                if (clip.Contains(point) == false) return false;
            }

            // Идем с конца (последние отрисованные дети - сверху)
            for (int i = element.Children.Count - 1; i >= 0; i--)
            {
                HitTest(element.Children.ElementAt(i), point, results);
            }

            Rect bounds = element.GetScreenBounds();
            if (bounds.Contains(point))
            {
                results.Add(element);
                return true;
            }

            return false;
        }

        private void ProcessHover(List<CanvasElement> hits)
        {
            CanvasElement topInteractiveElement = null;
            foreach (CanvasElement hit in hits)
            {
                foreach (CanvasComponent comp in hit.Components)
                {
                    if (comp is IPointerEnterHandler)
                    {
                        topInteractiveElement = hit;
                        break;
                    }
                }
                if (topInteractiveElement != null) break;
            }

            // Exit
            for (int i = hoveredElements.Count - 1; i >= 0; i--)
            {
                var el = hoveredElements[i];
                if (el != topInteractiveElement)
                {
                    hoveredElements.RemoveAt(i);
                    foreach (CanvasComponent comp in el.Components)
                        if (comp is IPointerExitHandler handler) handler.OnPointerExit();
                }
            }

            // Enter
            if (topInteractiveElement != null && !hoveredElements.Contains(topInteractiveElement))
            {
                hoveredElements.Add(topInteractiveElement);
                foreach (CanvasComponent comp in topInteractiveElement.Components)
                    if (comp is IPointerEnterHandler handler) handler.OnPointerEnter();
            }
        }
    }
}