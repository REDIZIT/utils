using System;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Button : CanvasComponent
    {
        private Image internalTargetGraphic;
        private Color internalNormalColor = Color.white;
        private Color internalHoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private Color internalPressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public Action onClick;
        public bool isHovered;
        public bool isPressed;

        public Image targetGraphic
        {
            get => internalTargetGraphic;
            set
            {
                if (internalTargetGraphic == value) return;
                internalTargetGraphic = value;
                ApplyVisualState();
            }
        }

        public Color normalColor
        {
            get => internalNormalColor;
            set
            {
                if (internalNormalColor == value) return;
                internalNormalColor = value;
                if (!isHovered && !isPressed)
                    ApplyVisualState();
            }
        }

        public Color hoverColor
        {
            get => internalHoverColor;
            set
            {
                if (internalHoverColor == value) return;
                internalHoverColor = value;
                if (isHovered && !isPressed)
                    ApplyVisualState();
            }
        }

        public Color pressedColor
        {
            get => internalPressedColor;
            set
            {
                if (internalPressedColor == value) return;
                internalPressedColor = value;
                if (isPressed)
                    ApplyVisualState();
            }
        }
        
        public override void OnAttached()
        {
	        base.OnAttached();

	        if (targetGraphic == null && Element != null)
	        {
		        targetGraphic = Element.GetComponent<Image>();
	        }

	        ApplyVisualState();
        }

        public override void Update()
        {
	        Vector2 mousePos = Input.mousePosition;
	        Vector4 bounds = Element.GetScreenBounds(); // (minX, minY, maxX, maxY)

	        Vector4 clickArea = bounds;
	        clickArea.z--;
	        clickArea.w--;

	        bool inside = mousePos.x >= clickArea.x && mousePos.x <= clickArea.z &&
	                      mousePos.y >= clickArea.y && mousePos.y <= clickArea.w;

            bool stateChanged = false;

            if (inside != isHovered)
            {
                isHovered = inside;
                stateChanged = true;
            }

            if (isHovered)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    isPressed = true;
                    stateChanged = true;
                }

                if (Input.GetMouseButtonUp(0) && isPressed)
                {
                    isPressed = false;
                    stateChanged = true;
                    onClick?.Invoke();
                }
            }
            else
            {
                if (isPressed)
                {
                    isPressed = false;
                    stateChanged = true;
                }
            }

            if (stateChanged)
            {
                ApplyVisualState();
            }
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
                MarkDirty(); // Вызываем локальный MarkDirty компонента!
            }
        }
    }
}