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
			for (int i = 0; i < container.Element.children.Count; i++)
			{
				var lot = container.Element.children[i].GetComponent<ContextMenuItemLot>();
				if (lot != null)
				{
					lot.menuService = service;
					lot.menuLevel = menuLevel;
				}
			}
		}
	}
}