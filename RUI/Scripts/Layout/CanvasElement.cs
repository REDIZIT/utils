using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasElement
    {
        public string key;
        public CanvasElement parent;
        public CanvasTransform transform = new();
        public int layerOffset = 0;

        public Action onTreeDirty;
        
        public CanvasService service;
        public CanvasReconciler reconciler;
        
        
        private readonly List<CanvasElement> children = new();
        private readonly List<CanvasComponent> components = new();
        
        private bool _isEnabled = true;
        
        public bool isEnabled
        {
	        get => _isEnabled;
	        set
	        {
		        if (_isEnabled == value) return;
		        _isEnabled = value;
		        MarkDirty();
	        }
        }
        
        public IReadOnlyCollection<CanvasElement> Children => children;
        public IReadOnlyCollection<CanvasComponent> Components => components;

        public void MarkDirty()
        {
            if (parent != null) parent.MarkDirty();
            else onTreeDirty?.Invoke();
        }

        public void AddChild(CanvasElement child)
        {
            child.parent = this;
            children.Add(child);
            MarkDirty();
        }

        public void RemoveChild(int childIndex)
        {
	        CanvasElement child = children[childIndex];
	        RemoveChild(child);
        }

        public void RemoveChildren(int startIndex, int count)
        {
	        children.RemoveRange(startIndex, count);
	        MarkDirty();
        }
        
        public void RemoveChild(CanvasElement child)
        {
	        child.parent = null;
	        children.Remove(child);
	        MarkDirty();
        }

        public void InsertChild(int index, CanvasElement child)
        {
	        child.parent = this;
	        children.Insert(index, child);
	        MarkDirty();
        }

        public T AddComponent<T>() where T : CanvasComponent, new()
        {
            var component = new T { Element = this };
            AddComponent(component);
            return component;
        }
        
        public void AddComponent<T>(T component) where T : CanvasComponent
        {
	        components.Add(component);
	        MarkDirty();
        }

        public T? TryGetComponent<T>() where T : class
        {
	        foreach (CanvasComponent c in components)
	        {
		        if (c is T match) return match;
	        }
	        return null;
        }
        
        public CanvasComponent? TryGetComponent(Type type, string? name = null)
        {
	        foreach (CanvasComponent c in components)
	        {
		        if ((name == null || c.id == name) && c.GetType() == type) return c;
	        }
	        return null;
        }

        public CanvasComponent GetComponent(Type targetType)
        {
            for (int i = 0; i < components.Count; i++)
                if (targetType.IsAssignableFrom(components[i].GetType())) return components[i];
            return null;
        }

        public Matrix4x4 LocalToRoot
        {
            get
            {
                if (parent == null) return transform.LocalToParent;
                return parent.LocalToRoot * transform.LocalToParent;
            }
        }

        public void Solve(SizeConstraints constraints)
        {
	        ILayoutSolver? solver = null;
	        foreach (CanvasComponent comp in components)
	        {
		        if (comp is ILayoutSolver s)
		        {
			        solver = s;
			        break;
		        }
	        }

	        if (solver != null)
	        {
		        solver.Solve(constraints);
	        }
	        else
	        {
		        SolveChildren(constraints);
	        }
        }

        public void SolveChildren(SizeConstraints? containerConstraints = null)
        {
	        containerConstraints ??= new(transform.size);
	        
	        foreach (CanvasElement child in children)
	        {
		        child.Solve(containerConstraints.Value);
	        }
        }
        
        public bool TryMeasure(SizeConstraints constraints, out PreferredSize preferredSize)
        {
	        foreach (CanvasComponent comp in components)
	        {
		        if (comp is IMeasurable m)
		        {
			        preferredSize = m.Measure(constraints);
			        return true;
		        }
	        }
	        
	        foreach (CanvasElement child in children)
	        {
		        if (child.TryMeasure(constraints, out preferredSize)) return true;
	        }

	        preferredSize = default;
	        return false;
        }


        public void UpdateTree()
        {
            if (!isEnabled) return;

            foreach (CanvasComponent c in components)
            {
	            if (c.isEnabled) c.Update();
            }

            foreach (CanvasElement child in children)
            {
	            child.UpdateTree();
            }
        }

        public void RenderTree(CanvasGenerationContext ctx)
        {
	        if (!isEnabled) return;

	        // Сохраняем старый офсет, чтобы вернуть его после отрисовки детей
	        int previousOffset = ctx.currentLayerOffset;
	        ctx.currentLayerOffset += layerOffset;

	        Mask mask = TryGetComponent<Mask>();
	        bool hasMask = mask != null && mask.enabled;
	        if (hasMask) ctx.PushClipRect(mask.GetWorldClipRect());

	        foreach (CanvasComponent comp in components)
	        {
		        if (comp.isEnabled) comp.GenerateMesh(ctx);
	        }
	        
	        foreach (CanvasElement child in children)
	        {
		        if (child.isEnabled) child.RenderTree(ctx);
	        }

	        if (hasMask) ctx.PopClipRect();

	        // Возвращаем офсет назад
	        ctx.currentLayerOffset = previousOffset;
        }

        public Vector4 GetScreenBounds()
        {
            Matrix4x4 m = LocalToRoot;

            float2 size = transform.size;
            
            Vector3 p0 = m.MultiplyPoint3x4(new(0, 0, 0));
            Vector3 p1 = m.MultiplyPoint3x4(new(size.x, 0, 0));
            Vector3 p2 = m.MultiplyPoint3x4(new(0, size.y, 0));
            Vector3 p3 = m.MultiplyPoint3x4(new(size.x, size.y, 0));

            float minX = math.min(math.min(p0.x, p1.x), math.min(p2.x, p3.x));
            float minY = math.min(math.min(p0.y, p1.y), math.min(p2.y, p3.y));
            float maxX = math.max(math.max(p0.x, p1.x), math.max(p2.x, p3.x));
            float maxY = math.max(math.max(p0.y, p1.y), math.max(p2.y, p3.y));

            return new(minX, minY, maxX, maxY);
        }

        public string GetPath()
        {
	        if (parent == null) return "<root>";

	        string parentPath = parent.GetPath();

	        if (key != null)
	        {
		        return $"{parentPath}/{key}";
	        }
	        else
	        {
		        int childIndex = parent.children.IndexOf(this);
		        return $"{parentPath}/[{childIndex}]";
	        }
        }
    }
}