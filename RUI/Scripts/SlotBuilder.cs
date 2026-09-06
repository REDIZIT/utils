using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class SlotBuilder
	{
		public readonly List<SlotDescriptor> slots = new();

		public SlotBuilder Slot<TLot, TModel>(string key, TModel model) where TLot : CanvasLot<TModel>
		{
			slots.Add(new SlotDescriptor
			{
				key = key,
				lotType = typeof(TLot),
				model = model
			});
			return this;
		}

		public SlotBuilder Slot<TLot>(string key) where TLot : CanvasComponent
		{
			slots.Add(new SlotDescriptor
			{
				key = key,
				lotType = typeof(TLot),
				model = null
			});
			return this;
		}
	}
}