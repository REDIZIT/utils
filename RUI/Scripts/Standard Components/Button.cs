using System;
using UnityEngine;

namespace InGame.UI
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
        }

        public override void Update()
        {
            Vector2 mousePos = Input.mousePosition;

            Vector3 rootPos = Element.LocalToRoot.MultiplyPoint3x4(Vector3.zero);
            float width = Transform.size.x * Transform.scale.x;
            float height = Transform.size.y * Transform.scale.y;

            bool inside = mousePos.x >= rootPos.x && mousePos.x <= rootPos.x + width &&
                          mousePos.y >= rootPos.y && mousePos.y <= rootPos.y + height;

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

        public void ApplyVisualState()
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