using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
    public class CanvasElement
    {
        public string key;
        public CanvasElement parent;
        public int layerOffset = 0;

        public Action onTreeDirty;
        
        public CanvasService service;
        public CanvasReconciler reconciler;
        public ResolvedTransform transform;
        
        public DesiredSize DesiredSize { get; private set; }
        
        private readonly List<CanvasElement> children = new();
        private readonly List<CanvasComponent> components = new();
        
        public Node_Element sourceNode; // Исходная AST нода элемента для оверрайда свойств
        private string _style;
        private bool _isEnabled = true;
        
        public string style
        {
	        get => _style;
	        set
	        {
		        if (_style == value) return;
		        _style = value;
		        ApplyStyles();
		        MarkDirty();
	        }
        }
        
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
        
        public DesiredSize Measure(SizeConstraints constraints)
		{
		    // 1. Быстрый поиск композитора БЕЗ аллокаций и LINQ
		    IMeasurable composer = null;
		    for (int i = 0; i < components.Count; i++)
		    {
		        var comp = components[i];
		        if (comp.isEnabled && comp is IMeasurable m && comp is IComposer)
		        {
		            composer = m;
		            break;
		        }
		    }

		    if (composer != null)
		    {
		        DesiredSize = composer.Measure(constraints);
		        return DesiredSize;
		    }

		    // 2. Дефолтный Measure для контейнеров с детьми
		    if (children.Count > 0)
		    {
		        float childMaxWidth = 0;
		        float childMaxHeight = 0;

		        for (int i = 0; i < children.Count; i++)
		        {
		            var child = children[i];
		            if (!child.isEnabled) continue;
		            var size = child.Measure(constraints);
		            childMaxWidth = math.max(childMaxWidth, size.x);
		            childMaxHeight = math.max(childMaxHeight, size.y);
		        }

		        DesiredSize = constraints.Clamp(new DesiredSize(childMaxWidth, childMaxHeight));
		        return DesiredSize;
		    }

		    // 3. Листовые компоненты (Image, Label)
		    for (int i = 0; i < components.Count; i++)
		    {
		        var comp = components[i];
		        if (comp.isEnabled && comp is IMeasurable m)
		        {
		            DesiredSize = m.Measure(constraints);
		            return DesiredSize;
		        }
		    }

		    DesiredSize = constraints.Clamp(new DesiredSize(0, 0));
		    return DesiredSize;
		}

		public void Arrange(ArrangeRect rect)
		{
		    VisualTransform vt = TryGetComponent<VisualTransform>();
		    if (vt != null)
		    {
		        transform = ResolvedTransform.FromRect(rect, vt.angle, vt.scale);
		    }
		    else
		    {
		        transform = ResolvedTransform.FromRect(rect);
		    }

		    if (children.Count > 0)
		    {
		        // Ищем IComposer быстрым циклом без LINQ
		        IComposer composer = null;
		        for (int i = 0; i < components.Count; i++)
		        {
		            if (components[i].isEnabled && components[i] is IComposer c)
		            {
		                composer = c;
		                break;
		            }
		        }

		        if (composer != null)
		        {
		            composer.Arrange(rect);
		        }
		        else
		        {
		            ArrangeRect defaultChildRect = new ArrangeRect(float2.zero, rect.size);
		            for (int i = 0; i < children.Count; i++)
		            {
		                var child = children[i];
		                if (!child.isEnabled) continue;
		                child.Arrange(defaultChildRect);
		            }
		        }
		    }
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

        public T? TryGetComponent<T>() where T : CanvasComponent
        {
	        foreach (CanvasComponent c in components) if (c is T match) return match;
	        return null;
        }
        
        public bool TryGetComponent<T>(out T comp) where T : CanvasComponent
        {
	        foreach (CanvasComponent c in components)
	        {
		        if (c is T match)
		        {
			        comp = match;
			        return true;
		        }
	        }
	        comp = null;
	        return false;
        }
        
        public bool TryGetComponentInChildren<T>(out T comp) where T : CanvasComponent
        {
	        if (TryGetComponent(out comp)) return true;
	        foreach (CanvasElement child in children)
	        {
		        if (child.TryGetComponentInChildren(out comp)) return true;
	        }
	        return false;
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

        public void UpdateTree()
        {
            if (!isEnabled) return;

            // Обход компонентов с конца к началу
            for (int i = components.Count - 1; i >= 0; i--)
            {
	            if (i < components.Count) // Защита от резкого уменьшения списка
	            {
		            var c = components[i];
		            if (c.isEnabled) c.Update();
	            }
            }

            // Обход детей с конца к началу
            for (int i = children.Count - 1; i >= 0; i--)
            {
	            if (i < children.Count)
	            {
		            var child = children[i];
		            if (child.isEnabled) child.UpdateTree();
	            }
            }
        }

        // Заменяем RenderTree: передаем мировую матрицу сверху вниз
        public void RenderTree(CanvasGenerationContext ctx, Matrix4x4 parentMatrix)
        {
	        if (!isEnabled) return;

	        // Считаем мировую матрицу один раз:
	        Matrix4x4 worldMatrix = parentMatrix * transform.LocalToParent;

	        int previousOffset = ctx.currentLayerOffset;
	        ctx.currentLayerOffset += layerOffset;

	        Mask mask = TryGetComponent<Mask>();
	        bool hasMask = mask != null && mask.enabled;
	        if (hasMask) ctx.PushClipRect(mask.GetWorldClipRect());

	        for (int i = 0; i < components.Count; i++)
	        {
		        var comp = components[i];
		        if (comp.isEnabled)
		        {
			        // Передаем worldMatrix прямо в GenerateMesh
			        comp.GenerateMesh(ctx, worldMatrix);
		        }
	        }
    
	        for (int i = 0; i < children.Count; i++)
	        {
		        var child = children[i];
		        if (child.isEnabled)
		        {
			        child.RenderTree(ctx, worldMatrix);
		        }
	        }

	        if (hasMask) ctx.PopClipRect();
	        ctx.currentLayerOffset = previousOffset;
        }

        public Rect GetScreenBounds()
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

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
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

        public string PrintTree()
        {
	        StringBuilder b = new();
	        PrintTree(b, 0);
	        return b.ToString();
        }

        private void PrintTree(StringBuilder b, int depth)
        {
	        for (int i = 0; i < depth; i++) b.Append("- ");
	        
	        if (key != null) b.AppendLine($"{key}: {transform}");
	        else b.AppendLine($"[{parent.children.IndexOf(this)}]: {transform}");
	        
	        foreach (CanvasElement child in children)
	        {
		        child.PrintTree(b, depth + 1);
	        }
        }
        
        // Получение стиля с учетом каскада от родителей
        public string GetEffectiveStyle()
        {
	        if (!string.IsNullOrEmpty(_style)) return _style;
	        return parent?.GetEffectiveStyle();
        }

        public void ApplyStyles()
        {
	        reconciler?.ApplyStylesToElement(this);

	        // Каскадное обновление дочерних узлов, не имеющих своего стиля
	        foreach (CanvasElement child in children)
	        {
		        if (string.IsNullOrEmpty(child._style))
		        {
			        child.ApplyStyles();
		        }
	        }
        }
    }
}