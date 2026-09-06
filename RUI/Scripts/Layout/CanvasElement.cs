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
        public IComposer composer;

        private bool internalIsEnabled = true;

        // Аналог GameObject.activeSelf
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

        // Проверка активности с учетом всей цепочки родителей
        public bool IsActiveInHierarchy => isEnabled && (parent == null || parent.IsActiveInHierarchy);

        public readonly List<CanvasElement> children = new List<CanvasElement>();
        public readonly CanvasTransform transform = new CanvasTransform();
        public readonly List<CanvasComponent> components = new List<CanvasComponent>();

        public Action onTreeDirty;

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

        public T AddComponent<T>() where T : CanvasComponent, new()
        {
            var component = new T { Element = this };
            components.Add(component);
            MarkDirty();
            return component;
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

            for (int i = 0; i < components.Count; i++)
                components[i].Update();

            for (int i = 0; i < children.Count; i++)
                children[i].UpdateTree();
        }

        // Блокировка рендеринга неактивных элементов
        public void RenderTree(CanvasGenerationContext ctx)
        {
            if (!isEnabled) return;

            Mask mask = GetComponent<Mask>();
            bool hasMask = mask != null && mask.enabled;

            if (hasMask) ctx.PushClipRect(mask.GetWorldClipRect());

            for (int i = 0; i < components.Count; i++)
                components[i].GenerateMesh(ctx);

            for (int i = 0; i < children.Count; i++)
                children[i].RenderTree(ctx);

            if (hasMask) ctx.PopClipRect();
        }

        public float2 SolveLayout(SizeConstraints constraints)
        {
            if (!isEnabled) return float2.zero;
            return composer.Solve(constraints);
        }

        public void SetComposer(IComposer newComposer)
        {
            composer = newComposer;
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

            Vector3 p0 = m.MultiplyPoint3x4(new Vector3(0, 0, 0));
            Vector3 p1 = m.MultiplyPoint3x4(new Vector3(transform.size.x, 0, 0));
            Vector3 p2 = m.MultiplyPoint3x4(new Vector3(0, transform.size.y, 0));
            Vector3 p3 = m.MultiplyPoint3x4(new Vector3(transform.size.x, transform.size.y, 0));

            float minX = math.min(math.min(p0.x, p1.x), math.min(p2.x, p3.x));
            float minY = math.min(math.min(p0.y, p1.y), math.min(p2.y, p3.y));
            float maxX = math.max(math.max(p0.x, p1.x), math.max(p2.x, p3.x));
            float maxY = math.max(math.max(p0.y, p1.y), math.max(p2.y, p3.y));

            return new Vector4(minX, minY, maxX, maxY);
        }
    }
}