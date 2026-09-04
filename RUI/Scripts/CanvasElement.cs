using System;
using System.Collections.Generic;
using UnityEngine;

namespace InGame.UI
{
    public class CanvasElement
    {
        public string key;
        public CanvasElement parent;
        public readonly List<CanvasElement> children = new List<CanvasElement>();
        public readonly CanvasTransform transform = new CanvasTransform();
        public readonly List<CanvasComponent> components = new List<CanvasComponent>();

        // Событие для корня дерева (на него подписывается холст)
        public Action onTreeDirty;

        public void MarkDirty()
        {
            if (parent != null)
            {
                parent.MarkDirty();
            }
            else
            {
                // Мы дошли до корня дерева
                onTreeDirty?.Invoke();
            }
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

        public T GetComponent<T>() where T : CanvasComponent
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T match)
                    return match;
            }
            return null;
        }

        public CanvasComponent GetComponent(Type targetType)
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (targetType.IsAssignableFrom(components[i].GetType()))
                    return components[i];
            }
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

        public void UpdateTree()
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].Update();
            }

            for (int i = 0; i < children.Count; i++)
            {
                children[i].UpdateTree();
            }
        }

        public void RenderTree(CanvasGenerationContext ctx)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].GenerateMesh(ctx);
            }

            for (int i = 0; i < children.Count; i++)
            {
                children[i].RenderTree(ctx);
            }
        }
    }
}