using System;
using System.Collections.Generic;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class ToggleGroup : CanvasComponent
    {
        // Явный список найденных ниже Toggle'ов
        public readonly List<Toggle> toggles = new();

        // Запрещает полностью выключать все кнопки (режим Radio)
        public bool allowSwitchOff = false;

        public Toggle activeToggle { get; private set; }
        public int activeIndex { get; private set; } = -1;

        public event Action<Toggle> onActiveToggleChanged;
        public event Action<int> onActiveIndexChanged;

        private bool isUpdating = false;

        public override void OnAttached()
        {
            base.OnAttached();
            RebuildTogglesList();
        }

        public void RebuildTogglesList()
        {
            // Отписываемся от старых кнопок
            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].group = null;
            }
            toggles.Clear();

            // Рекурсивно собираем все Toggle'ы в поддереве
            FindTogglesRecursive(Element, toggles);

            Toggle firstActive = null;

            // Настраиваем группу и подписываемся
            for (int i = 0; i < toggles.Count; i++)
            {
                Toggle t = toggles[i];
                t.group = this;

                if (t.isOn)
                {
                    if (firstActive == null)
                    {
                        firstActive = t;
                    }
                    else
                    {
                        // Только один тоггл может быть включен изначально
                        t.SetIsOnWithoutNotify(false);
                    }
                }
            }

            // Если ни один не включен, включаем первый по умолчанию
            if (firstActive == null && !allowSwitchOff && toggles.Count > 0)
            {
                toggles[0].SetIsOnWithoutNotify(true);
                firstActive = toggles[0];
            }

            activeToggle = firstActive;
            activeIndex = firstActive != null ? toggles.IndexOf(firstActive) : -1;

            for (int i = 0; i < toggles.Count; i++)
            {
                toggles[i].UpdateVisuals();
            }
        }

        public void OnToggleChanged(Toggle toggle, bool isOn)
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;

                if (isOn)
                {
                    // Выключаем остальные тогглы
                    for (int i = 0; i < toggles.Count; i++)
                    {
                        Toggle other = toggles[i];
                        if (other != toggle && other.isOn)
                        {
                            other.SetIsOn(false, notifyGroup: false);
                        }
                    }

                    activeToggle = toggle;
                    activeIndex = toggles.IndexOf(toggle);

                    onActiveToggleChanged?.Invoke(toggle);
                    onActiveIndexChanged?.Invoke(activeIndex);
                }
                else if (!allowSwitchOff)
                {
                    // Нельзя снять выбор с единственного активного тоггла
                    bool anyActive = false;
                    for (int i = 0; i < toggles.Count; i++)
                    {
                        if (toggles[i].isOn)
                        {
                            anyActive = true;
                            break;
                        }
                    }

                    if (!anyActive)
                    {
                        toggle.SetIsOnWithoutNotify(true);
                    }
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        public void SetIsOnWithoutNotify(int index)
        {
	        for (int i = 0; i < toggles.Count; i++)
	        {
		        toggles[i].SetIsOnWithoutNotify(index == i);
	        }
        }

        public void Select(int index)
        {
            if (index >= 0 && index < toggles.Count)
            {
                toggles[index].isOn = true;
            }
        }

        private void FindTogglesRecursive(CanvasElement current, List<Toggle> result)
        {
            foreach (CanvasComponent comp in current.Components)
            {
                if (comp is Toggle t)
                {
                    result.Add(t);
                }
            }

            foreach (CanvasElement child in current.Children)
            {
                // Если внутри встретилась другая ToggleGroup, не лезем в чужую зону ответственности
                if (child.TryGetComponent<ToggleGroup>() == null)
                {
                    FindTogglesRecursive(child, result);
                }
            }
        }
    }
}