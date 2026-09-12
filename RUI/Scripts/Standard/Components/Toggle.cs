using System;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class Toggle : CanvasComponent, IPointerDownHandler
    {
        private bool _isOn = false;

        public bool isOn
        {
            get => _isOn;
            set => SetIsOn(value, notifyGroup: true);
        }

        // Цвета для активного/неактивного состояния
        public Color activeColor = new Color(0.24f, 0.44f, 0.75f, 1f); // Активный синий в духе Blender
        public Color inactiveColor = Color.clear;
        public Color activeTextColor = Color.white;
        public Color inactiveTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public event Action<bool> onValueChanged;

        [WireIgnore] public ToggleGroup group;
        [WireIgnore] private Button button;
        [WireIgnore] private Image bgImage;
        [WireIgnore] private Label label;

        public override void OnAttached()
        {
            base.OnAttached();

            // Ищем компоненты для интерактивности и визуализации
            button = Element.TryGetComponent<Button>();
            if (button != null)
            {
                button.onClick += OnClicked;
            }

            bgImage = Element.TryGetComponent<Image>();
            label = Element.TryGetComponent<Label>();
            if (label == null && Element.Children.Count > 0)
            {
                foreach (CanvasElement child in Element.Children)
                {
                    label = child.TryGetComponent<Label>();
                    if (label != null) break;
                }
            }

            UpdateVisuals();
        }

        public void SetIsOn(bool value, bool notifyGroup = true)
        {
            if (_isOn == value) return;
            _isOn = value;

            UpdateVisuals();
            onValueChanged?.Invoke(_isOn);

            if (notifyGroup && group != null)
            {
                group.OnToggleChanged(this, _isOn);
            }
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            _isOn = value;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (bgImage != null)
            {
                bgImage.color = _isOn ? activeColor : inactiveColor;
            }

            if (label != null)
            {
                label.color = _isOn ? activeTextColor : inactiveTextColor;
            }

            Element?.MarkDirty();
        }

        private void OnClicked()
        {
            // В режиме радио-группы клик по уже активному элементу не должен его выключать
            if (group != null && !group.allowSwitchOff && _isOn)
            {
                return;
            }

            isOn = !_isOn;
        }

        public void OnPointerDown(PointerDownEvent e, GestureArena arena)
        {
            // Если на элементе нет Button, переключаем по обычному клику
            if (button == null && e.button == 0)
            {
                OnClicked();
            }
        }
    }
}