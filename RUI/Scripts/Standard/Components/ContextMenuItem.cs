using System;
using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class ContextMenuItem
	{
		public string text;
		public string icon;         // Имя спрайта в UIAssetDatabase (например "arrow", "eye")
		public string shortcut;     // Например "F2" или "Ctrl+D"
		public Action action;       // Колбэк при клике
		public bool isSeparator;    // Разделитель
		public bool isEnabled = true;

		// Вложенные пункты подменю:
		public List<ContextMenuItem> subItems;

		public bool HasSubmenu => subItems != null && subItems.Count > 0;
	}
}