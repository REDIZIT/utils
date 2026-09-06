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
        
        public readonly CanvasTransform transform = new();

        private readonly List<CanvasElement> children = new();
        private readonly List<CanvasComponent> components = new();

        public Action onTreeDirty;
        
        public CanvasService service;
        public CanvasReconciler reconciler;

        public CanvasElement()
        {
            composer = new Fill_Composer() { e = this };
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
                if (parent == null) return transform.LocalMatrix;
                return parent.LocalToRoot * transform.LocalMatrix;
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

	        for (int i = 0; i < components.Count; i++)
		        components[i].GenerateMesh(ctx);

	        for (int i = 0; i < children.Count; i++)
		        children[i].RenderTree(ctx);

	        if (hasMask) ctx.PopClipRect();

	        // Возвращаем офсет назад
	        ctx.currentLayerOffset = previousOffset;
        }

        public float2 SolveLayout(SizeConstraints constraints)
        {
	        if (!isEnabled) return float2.zero;

	        // 1. Композер рассчитывает размеры элемента и расставляет детей
	        float2 result = composer.Solve(constraints);

	        // 2. Хук завершения верстки: компоненты (ScrollView, ползунки, тултипы)
	        // получают доступ к 100% свежим размерам в этом же кадре!
	        for (int i = 0; i < components.Count; i++)
	        {
		        components[i].OnLayoutComplete();
	        }

	        return result;
        }

        public float2 GetPreferredContentSize()
        {
            float2 contentSize = float2.zero;
            for (int i = 0; i < components.Count; i++)
                contentSize = math.max(contentSize, components[i].GetPreferredSize());
            return contentSize;
        }

        // Расчет AABB с учетом поворота всех 4 углов
        public Vector4 GetScreenBounds()
        {
            Matrix4x4 m = LocalToRoot;

            Vector3 p0 = m.MultiplyPoint3x4(new(0, 0, 0));
            Vector3 p1 = m.MultiplyPoint3x4(new(transform.calculatedSize.x, 0, 0));
            Vector3 p2 = m.MultiplyPoint3x4(new(0, transform.calculatedSize.y, 0));
            Vector3 p3 = m.MultiplyPoint3x4(new(transform.calculatedSize.x, transform.calculatedSize.y, 0));

            float minX = math.min(math.min(p0.x, p1.x), math.min(p2.x, p3.x));
            float minY = math.min(math.min(p0.y, p1.y), math.min(p2.y, p3.y));
            float maxX = math.max(math.max(p0.x, p1.x), math.max(p2.x, p3.x));
            float maxY = math.max(math.max(p0.y, p1.y), math.max(p2.y, p3.y));

            return new Vector4(minX, minY, maxX, maxY);
        }
    }
}