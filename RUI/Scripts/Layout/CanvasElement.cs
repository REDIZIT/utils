using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasElement
    {
        public string key;
        public CanvasElement parent;
        public IComposer composer;
        public int layerOffset = 0;

        private bool internalIsEnabled = true;

        public bool isEnabled
        {
            get => internalIsEnabled;
            set
            {
                if (internalIsEnabled == value) return;
                internalIsEnabled = value;
                MarkDirty();
            }
        }
        
        public IReadOnlyCollection<CanvasElement> Children => children;
        public IReadOnlyCollection<CanvasComponent> Components => components;

        public PreferredSize? preferredSize;

        public ResolvedTransform? layoutTransform;
        public ResolvedTransform? renderTransform;

        private readonly List<CanvasElement> children = new();
        private readonly List<CanvasComponent> components = new();

        public Action onTreeDirty;
        
        public CanvasService service;
        public CanvasReconciler reconciler;

        public void ResetTransforms()
        {
	        preferredSize = null;
	        layoutTransform = null;
	        renderTransform = null;
        }

        public void ResetTransformsRecursive()
        {
	        ResetTransforms();
	        foreach (CanvasElement child in children) child.ResetTransformsRecursive();
        }

        public void ResolveTransform(ResolvedTransform t)
        {
	        if (layoutTransform != null || renderTransform != null)
	        {
		        throw new LayoutSolveException($"Transform of '{key}' already resolved. Multiple resolution is not allowed.");
	        }
	        
	        layoutTransform = t;
	        renderTransform = t;
        }

        public void CheckResolvedRecursive()
        {
	        if (renderTransform == null) throw new LayoutSolveException($"Render transform of '{key}' is not resolved");
	        if (preferredSize == null) throw new LayoutSolveException($"Preferred size of '{key}' is not resolved");
	        foreach (CanvasElement child in children) child.CheckResolvedRecursive();
        }

        private PreferredSize MeasureSelf(SizeConstraints constraints)
        {
	        foreach (CanvasComponent comp in components)
	        {
		        if (comp is IMeasurable measurable)
		        {
			        return measurable.Measure(constraints);
		        }
	        }
	        throw new LayoutSolveException($"No {nameof(IMeasurable)} component on element '{key}'");
        }
        
        public PreferredSize Measure(SizeConstraints constraints)
        {
	        PreferredSize size = composer?.Measure(constraints) ?? MeasureSelf(constraints);
	        preferredSize = constraints.Clamp(size);

	        return preferredSize!.Value;
        }

        public void Arrange(ResolvedTransform transform)
        {
	        ResolveTransform(transform);
	        
	        composer?.Arrange(transform.size);
        }

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
	        // for (int i = startIndex - count - 1; i >= startIndex; i--)
	        // {
		       //  CanvasElement child = children[i];
		       //  child.parent = null;
		       //  children.Remove(child);
	        // }
	        
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

        public T GetComponent<T>() where T : class
        {
            for (int i = 0; i < components.Count; i++)
                if (components[i] is T match) return match;
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
                if (parent == null) return renderTransform!.Value.localToParent;
                return parent.LocalToRoot * renderTransform!.Value.localToParent;
            }
        }

        // Блокировка обновления неактивных элементов
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

        // Блокировка рендеринга неактивных элементов
        public void RenderTree(CanvasGenerationContext ctx)
        {
	        if (!isEnabled) return;

	        // Сохраняем старый офсет, чтобы вернуть его после отрисовки детей
	        int previousOffset = ctx.currentLayerOffset;
	        ctx.currentLayerOffset += layerOffset;

	        Mask mask = GetComponent<Mask>();
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

            Vector3 p0 = m.MultiplyPoint3x4(new(0, 0, 0));
            Vector3 p1 = m.MultiplyPoint3x4(new(renderTransform!.Value.size.x, 0, 0));
            Vector3 p2 = m.MultiplyPoint3x4(new(0, renderTransform!.Value.size.y, 0));
            Vector3 p3 = m.MultiplyPoint3x4(new(renderTransform!.Value.size.x, renderTransform!.Value.size.y, 0));

            float minX = math.min(math.min(p0.x, p1.x), math.min(p2.x, p3.x));
            float minY = math.min(math.min(p0.y, p1.y), math.min(p2.y, p3.y));
            float maxX = math.max(math.max(p0.x, p1.x), math.max(p2.x, p3.x));
            float maxY = math.max(math.max(p0.y, p1.y), math.max(p2.y, p3.y));

            return new Vector4(minX, minY, maxX, maxY);
        }
    }
}