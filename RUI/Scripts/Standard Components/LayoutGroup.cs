using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
	public class LayoutGroup : CanvasComponent, ILayout
    {
        public LayoutDirection direction = LayoutDirection.Vertical;
        public LayoutAlignment alignment = LayoutAlignment.Begin;
        
        // X = Left, Y = Bottom, Z = Right, W = Top
        public float4 padding = float4.zero;
        public float spacing = 0f;
        public bool fitContent = true;

        public void Solve()
        {
            if (Element == null) return;

            var children = Element.children;

            // 1. Собираем активных участников верстки
            var layoutChildren = new List<CanvasElement>();
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var le = child.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;

                layoutChildren.Add(child);
            }

            if (layoutChildren.Count == 0)
            {
                if (fitContent)
                {
                    if (direction == LayoutDirection.Horizontal)
                        Transform.size.x = padding.x + padding.z;
                    else
                        Transform.size.y = padding.y + padding.w;
                }
                return;
            }

            // 2. Первый проход: считаем суммарную длину контента по главной оси
            float totalContentLength = 0f;

            for (int i = 0; i < layoutChildren.Count; i++)
            {
                var child = layoutChildren[i];
                var le = child.GetComponent<LayoutElement>();

                if (le != null) totalContentLength += le.spacingBefore;

                totalContentLength += (direction == LayoutDirection.Horizontal)
                    ? child.transform.size.x
                    : child.transform.size.y;

                if (i < layoutChildren.Count - 1)
                    totalContentLength += spacing;

                if (le != null) totalContentLength += le.spacingAfter;
            }

            // 3. Подгоняем размер контейнера (если fitContent)
            if (fitContent)
            {
                if (direction == LayoutDirection.Horizontal)
                    Transform.size.x = padding.x + totalContentLength + padding.z;
                else
                    Transform.size.y = padding.y + totalContentLength + padding.w;
            }

            // 4. Вычисляем локальные границы контейнера С УЧЕТОМ ЕГО PIVOT
            float containerLeft   = -Transform.size.x * Transform.pivot.x;
            float containerRight  =  Transform.size.x * (1f - Transform.pivot.x);
            float containerBottom = -Transform.size.y * Transform.pivot.y;
            float containerTop    =  Transform.size.y * (1f - Transform.pivot.y);

            // 5. Второй проход: расстановка детей с учетом пивотов родителя и детей
            if (direction == LayoutDirection.Vertical)
            {
                float availableLength = Transform.size.y;
                float totalPadding = padding.y + padding.w;
                float freeSpace = availableLength - (totalPadding + totalContentLength);

                float cursor = 0f;

                if (alignment == LayoutAlignment.Begin)
                    cursor = containerTop - padding.w; // Начинаем от верхнего края вниз
                else if (alignment == LayoutAlignment.Center)
                    cursor = containerTop - padding.w - freeSpace * 0.5f;
                else // End
                    cursor = containerBottom + padding.y + totalContentLength;

                for (int i = 0; i < layoutChildren.Count; i++)
                {
                    var child = layoutChildren[i];
                    var le = child.GetComponent<LayoutElement>();

                    if (le != null) cursor -= le.spacingBefore;

                    // Нижняя граница текущего слота
                    float slotBottom = cursor - child.transform.size.y;

                    // Позиционируем с учетом pivot дочернего элемента
                    float yPos = slotBottom + child.transform.size.y * child.transform.pivot.y;

                    // По горизонтали выравниваем от левого края с учетом padding.x и pivot.x ребенка
                    float slotLeft = containerLeft + padding.x;
                    float xPos = slotLeft + child.transform.size.x * child.transform.pivot.x;

                    child.transform.localPos = new float2(xPos, yPos);

                    cursor = slotBottom - spacing;
                    if (le != null) cursor -= le.spacingAfter;
                }
            }
            else // Horizontal
            {
                float availableLength = Transform.size.x;
                float totalPadding = padding.x + padding.z;
                float freeSpace = availableLength - (totalPadding + totalContentLength);

                float cursor = 0f;

                if (alignment == LayoutAlignment.Begin)
                    cursor = containerLeft + padding.x; // Начинаем от левого края вправо
                else if (alignment == LayoutAlignment.Center)
                    cursor = containerLeft + padding.x + freeSpace * 0.5f;
                else // End
                    cursor = containerRight - padding.z - totalContentLength;

                for (int i = 0; i < layoutChildren.Count; i++)
                {
                    var child = layoutChildren[i];
                    var le = child.GetComponent<LayoutElement>();

                    if (le != null) cursor += le.spacingBefore;

                    float slotRight = cursor + child.transform.size.x;

                    // По горизонтали с учетом pivot.x ребенка
                    float xPos = cursor + child.transform.size.x * child.transform.pivot.x;

                    // По вертикали от нижнего края с учетом padding.y и pivot.y ребенка
                    float slotBottom = containerBottom + padding.y;
                    float yPos = slotBottom + child.transform.size.y * child.transform.pivot.y;

                    child.transform.localPos = new float2(xPos, yPos);

                    cursor = slotRight + spacing;
                    if (le != null) cursor += le.spacingAfter;
                }
            }
        }
    }
}