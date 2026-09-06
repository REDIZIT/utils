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
        
        public readonly List<CanvasElement> children = new List<CanvasElement>();
        public readonly CanvasTransform transform = new CanvasTransform();
        public readonly List<CanvasComponent> components = new List<CanvasComponent>();

        // Событие для корня дерева (на него подписывается холст)
        public Action onTreeDirty;

        public CanvasElement()
        {
	        composer = new SizedBox_Composer { e = this };
        }

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

        public T GetComponent<T>() where T : class
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T match) return match;
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

        // Внутри CanvasElement.cs:
        public void RenderTree(CanvasGenerationContext ctx)
        {
	        Mask mask = GetComponent<Mask>();
	        bool hasMask = mask != null && mask.enabled;

	        if (hasMask)
	        {
		        ctx.PushClipRect(mask.GetWorldClipRect());
	        }

	        // Рисуем компоненты текущего элемента (например, фоновый Image или сам Mask)
	        for (int i = 0; i < components.Count; i++)
	        {
		        components[i].GenerateMesh(ctx);
	        }

	        // Рисуем всех детей (они уже будут отсекаться по маске!)
	        for (int i = 0; i < children.Count; i++)
	        {
		        children[i].RenderTree(ctx);
	        }

	        if (hasMask)
	        {
		        ctx.PopClipRect();
	        }
        }
        
        public float2 SolveLayout(SizeConstraints constraints)
        {
	        return composer.Solve(constraints);
        }
        
        public void SetComposer(IComposer newComposer)
        {
	        composer = newComposer;
        }
        
        public Vector4 GetScreenBounds()
        {
	        Matrix4x4 m = LocalToRoot;
	        // Левый нижний и правый верхний углы в мировых координатах экрана
	        Vector3 pMin = m.MultiplyPoint3x4(Vector3.zero);
	        Vector3 pMax = m.MultiplyPoint3x4(new Vector3(transform.size.x, transform.size.y, 0));

	        return new Vector4(
		        math.min(pMin.x, pMax.x),
		        math.min(pMin.y, pMax.y),
		        math.max(pMin.x, pMax.x),
		        math.max(pMin.y, pMax.y)
	        );
        }
    }
}