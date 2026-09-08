using System;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Button : CanvasComponent, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image internalTargetGraphic;
        private Color internalNormalColor = Color.white;
        private Color internalHoverColor = new(0.85f, 0.85f, 0.85f, 1f);
        private Color internalPressedColor = new(0.6f, 0.6f, 0.6f, 1f);

        public Action onClick;
        public Action onRightClick;
        
        public bool isHovered;
        public bool isPressed;

        private readonly TapGestureRecognizer recognizer = new();

        public Image targetGraphic
        {
            get => internalTargetGraphic;
            set { internalTargetGraphic = value; ApplyVisualState(); }
        }

        public Color normalColor
        {
            get => internalNormalColor;
            set { internalNormalColor = value; ApplyVisualState(); }
        }

        public Color hoverColor
        {
            get => internalHoverColor;
            set { internalHoverColor = value; ApplyVisualState(); }
        }

        public Color pressedColor
        {
            get => internalPressedColor;
            set { internalPressedColor = value; ApplyVisualState(); }
        }

        public override void OnAttached()
        {
            base.OnAttached();
            
            if (targetGraphic == null && Element != null)
            {
	            targetGraphic = Element.TryGetComponent<Image>();
            }

            recognizer.onDown = () =>
            {
                isPressed = true;
                ApplyVisualState();
            };

            recognizer.onCancel = () =>
            {
                isPressed = false;
                ApplyVisualState();
            };

            recognizer.onTap = () =>
            {
                isPressed = false;
                ApplyVisualState();
                onClick?.Invoke();
            };

            // Применяем визуал сразу при монтировании компонента
            ApplyVisualState();
        }

        public void OnPointerDown(PointerDownEvent e, GestureArena arena)
        {
	        if (e.button == 0)
	        {
		        recognizer.OnPointerDown(e, arena);
	        }
	        else if (e.button == 1) // ПКМ
	        {
		        onRightClick?.Invoke();
	        }
        }

        public void OnPointerMove(PointerMoveEvent e, GestureArena arena)
        {
            recognizer.OnPointerMove(e, arena);
        }

        public void OnPointerUp(PointerUpEvent e, GestureArena arena)
        {
            if (e.button == 0) recognizer.OnPointerUp(e, arena);
        }

        public void OnPointerEnter()
        {
            isHovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit()
        {
            isHovered = false;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (targetGraphic == null) return;

            Color targetColor = normalColor;
            if (isPressed) targetColor = pressedColor;
            else if (isHovered) targetColor = hoverColor;

            if (targetGraphic.color != targetColor)
            {
                targetGraphic.color = targetColor;
                MarkDirty();
            }
        }
    }
}