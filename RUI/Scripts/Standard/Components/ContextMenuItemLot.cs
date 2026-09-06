using UnityEngine;

namespace REDIZIT.RUI
{
    // Явно реализуем IPointerEnterHandler для отслеживания наведения мыши
    public class ContextMenuItemLot : CanvasLot<ContextMenuItem>, IPointerEnterHandler
    {
        private Label text;
        private Label shortcut;
        private Image icon;
        private Image arrow;
        private Image separator;
        private Button btn;

        public ContextMenuService menuService;
        public int menuLevel;

        public override void OnAttached()
        {
            base.OnAttached();

            btn.onClick += () =>
            {
                if (model == null || !model.isEnabled || model.isSeparator) return;

                if (model.HasSubmenu)
                {
                    OpenSubmenu();
                }
                else
                {
                    menuService?.CloseAll();
                    model.action?.Invoke();
                }
            };
        }

        // Реализация интерфейса IPointerEnterHandler
        public void OnPointerEnter()
        {
            if (model != null && model.HasSubmenu && model.isEnabled)
            {
                OpenSubmenu();
            }
            else
            {
                // При наведении на пункт без подменю закрываем все более глубокие уровни
                menuService?.CloseSubmenusAbove(menuLevel);
            }
        }

        private void OpenSubmenu()
        {
            Vector4 bounds = Element.GetScreenBounds();
            // Позиция подменю: правый верхний угол текущей плашки пункта
            Vector2 subMenuPos = new Vector2(bounds.z, bounds.w);
            menuService?.OpenSubmenu(subMenuPos, model.subItems, menuLevel + 1, bounds);
        }

        protected override void OnRefresh()
        {
            bool isSep = model.isSeparator;

            if (separator != null) separator.Element.isEnabled = isSep;
            if (btn != null) btn.Element.isEnabled = !isSep;

            if (isSep) return;

            text.text = model.text ?? "";

            if (shortcut != null)
            {
                shortcut.text = model.shortcut ?? "";
                shortcut.Element.isEnabled = !string.IsNullOrEmpty(model.shortcut);
            }

            if (icon != null)
            {
                // Резолвим Sprite по имени через UIAssetDatabase сервиса:
                if (!string.IsNullOrEmpty(model.icon) && Element?.service?.assetDatabase != null)
                {
                    icon.sprite = Element.service.assetDatabase.GetSprite(model.icon);
                    icon.Element.isEnabled = icon.sprite != null;
                }
                else
                {
                    icon.Element.isEnabled = false;
                }
            }

            if (arrow != null)
            {
                arrow.Element.isEnabled = model.HasSubmenu;
            }

            btn.isEnabled = model.isEnabled;
            text.color = model.isEnabled ? Color.white : new Color(1, 1, 1, 0.4f);
        }
    }
}