using System.Collections.Generic;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasInputManager
    {
        public readonly GestureArenaManager arenaManager = new GestureArenaManager();
        private readonly List<CanvasElement> hoveredElements = new List<CanvasElement>();
        private readonly List<CanvasElement> currentHits = new List<CanvasElement>();

        private Vector2 lastMousePos;

        public void ProcessInput(CanvasElement root)
        {
            if (root == null || !root.isEnabled) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 delta = mousePos - lastMousePos;
            lastMousePos = mousePos;

            // 1. Hit-Test снизу-вверх по z-order (первый в списке = самый верхний визуально)
            currentHits.Clear();
            HitTest(root, mousePos, currentHits);

            // 2. Обработка Hover (только верхняя кнопка подсвечивается!)
            ProcessHover(currentHits);

            // 3. Обработка PointerDown
            if (Input.GetMouseButtonDown(0))
            {
                var arena = arenaManager.OpenArena(0);
                var downEvent = new PointerDownEvent(mousePos, 0, 0);

                bool buttonCaptured = false;

                for (int i = 0; i < currentHits.Count; i++)
                {
                    var compList = currentHits[i].components;
                    for (int c = 0; c < compList.Count; c++)
                    {
                        var comp = compList[c];

                        // Только самая верхняя кнопка получает PointerDown и входит в арену!
                        if (comp is Button)
                        {
                            if (buttonCaptured) continue;
                            buttonCaptured = true;
                        }

                        if (comp is IPointerDownHandler handler)
                            handler.OnPointerDown(downEvent, arena);
                    }
                }
                arenaManager.CloseArena(0);
            }

            // 4. Обработка PointerMove
            if (delta.sqrMagnitude > 0.0001f)
            {
                var arena = arenaManager.GetArena(0);
                if (arena != null)
                {
                    var moveEvent = new PointerMoveEvent(mousePos, delta, 0);
                    for (int i = 0; i < currentHits.Count; i++)
                    {
                        var compList = currentHits[i].components;
                        for (int c = 0; c < compList.Count; c++)
                        {
                            if (compList[c] is IPointerMoveHandler handler)
                                handler.OnPointerMove(moveEvent, arena);
                        }
                    }
                }
            }

            // 5. Обработка PointerUp
            if (Input.GetMouseButtonUp(0))
            {
                var arena = arenaManager.GetArena(0);
                var upEvent = new PointerUpEvent(mousePos, 0, 0);

                for (int i = 0; i < currentHits.Count; i++)
                {
                    var compList = currentHits[i].components;
                    for (int c = 0; c < compList.Count; c++)
                    {
                        if (compList[c] is IPointerUpHandler handler)
                            handler.OnPointerUp(upEvent, arena);
                    }
                }
                arenaManager.Sweep(0);
            }

            // 6. Колесико мыши (Scroll)
            Vector2 scroll = Input.mouseScrollDelta;
            if (Mathf.Abs(scroll.y) > 0.001f || Mathf.Abs(scroll.x) > 0.001f)
            {
                var scrollEvent = new PointerScrollEvent(mousePos, scroll, 0);
                for (int i = 0; i < currentHits.Count; i++)
                {
                    var compList = currentHits[i].components;
                    for (int c = 0; c < compList.Count; c++)
                    {
                        if (compList[c] is IPointerScrollHandler handler)
                        {
                            handler.OnPointerScroll(scrollEvent);
                            return;
                        }
                    }
                }
            }
        }

        private bool HitTest(CanvasElement element, Vector2 point, List<CanvasElement> results)
        {
            if (!element.isEnabled) return false;

            var mask = element.GetComponent<Mask>();
            if (mask != null && mask.enabled)
            {
                Vector4 clip = mask.GetWorldClipRect();
                if (point.x < clip.x || point.x > clip.z || point.y < clip.y || point.y > clip.w)
                    return false;
            }

            for (int i = element.children.Count - 1; i >= 0; i--)
            {
                HitTest(element.children[i], point, results);
            }

            Vector4 bounds = element.GetScreenBounds();
            if (point.x >= bounds.x && point.x <= bounds.z && point.y >= bounds.y && point.y <= bounds.w)
            {
                results.Add(element);
                return true;
            }

            return false;
        }

        private void ProcessHover(List<CanvasElement> hits)
        {
            // Находим самый верхний элемент, у которого есть обработчик Hover (например, Button)
            CanvasElement topInteractiveElement = null;
            for (int i = 0; i < hits.Count; i++)
            {
                var comps = hits[i].components;
                for (int c = 0; c < comps.Count; c++)
                {
                    if (comps[c] is IPointerEnterHandler)
                    {
                        topInteractiveElement = hits[i];
                        break;
                    }
                }
                if (topInteractiveElement != null) break;
            }

            // Снимаем Hover со всех элементов, кроме самого верхнего
            for (int i = hoveredElements.Count - 1; i >= 0; i--)
            {
                var el = hoveredElements[i];
                if (el != topInteractiveElement)
                {
                    hoveredElements.RemoveAt(i);
                    for (int c = 0; c < el.components.Count; c++)
                        if (el.components[c] is IPointerExitHandler handler) handler.OnPointerExit();
                }
            }

            // Навешиваем Hover только на самый верхний интерактивный элемент
            if (topInteractiveElement != null && !hoveredElements.Contains(topInteractiveElement))
            {
                hoveredElements.Add(topInteractiveElement);
                for (int c = 0; c < topInteractiveElement.components.Count; c++)
                    if (topInteractiveElement.components[c] is IPointerEnterHandler handler) handler.OnPointerEnter();
            }
        }
    }
}