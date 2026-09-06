namespace REDIZIT.RUI
{
	public abstract class CanvasComponent
	{
		public string id;
		public CanvasElement Element { get; internal set; }
		public CanvasTransform Transform => Element?.transform;
		
		protected SubsContainer subs = new();

		public void MarkDirty()
		{
			Element?.MarkDirty();
		}
		
		public void EnsureComponent<T>() where T : CanvasComponent, new()
		{
			if (Element == null) return;
			
			T existing = Element.GetComponent<T>();
			if (existing == null) Element.AddComponent<T>();
		}

		public virtual void OnAttached() { }

		public virtual void OnDetached()
		{
			subs.Dispose();
		}

		public virtual void GenerateMesh(CanvasGenerationContext ctx) { }
		public virtual void Update() { }
	}
}
