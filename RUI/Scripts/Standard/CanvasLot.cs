namespace REDIZIT.RUI
{
	public abstract class CanvasLot<TModel> : CanvasComponent, ICanvasLot
	{
		public TModel model { get; private set; }

		public void Refresh(TModel model)
		{
			this.model = model;
			Refresh();
			MarkDirty();
		}

		public void RefreshUntyped(object model)
		{
			if (model is TModel typedModel)
			{
				Refresh(typedModel);
			}
		}

		protected abstract void Refresh();
	}
}