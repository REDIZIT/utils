using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class ContextMenuComponent : CanvasComponent
	{
		private LotsContainer container;
		public ContextMenuService menuService;
		public int level;

		public void SetItems(List<ContextMenuItem> items, ContextMenuService service, int menuLevel)
		{
			menuService = service;
			level = menuLevel;

			container.Refresh<ContextMenuItemLot, ContextMenuItem>(items);

			// Проставляем сервис и уровень во все созданные строки
			foreach (CanvasElement child in container.Element.Children)
			{
				var lot = child.GetComponent<ContextMenuItemLot>();
				if (lot != null)
				{
					lot.menuService = service;
					lot.menuLevel = menuLevel;
				}
			}
		}
	}
}