using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class CanvasReconciler
    {
        private readonly CanvasService service;
        private readonly DiContainer container;
        private readonly ILogger<CanvasReconciler> logger;

        public CanvasReconciler(CanvasService service, DiContainer container)
        {
            this.service = service;
            this.container = container;
            logger = container.Resolve<ILogger<CanvasReconciler>>();
        }

        public void Reconcile(CanvasElement element, Node_Element node)
        {
	        logger.LogDebug($"Reconcile '{element}' with {node} and {node.properties.Count} properties");
	        
	        element.service = service;
	        element.reconciler = this;
	        
	        foreach (Node_Property prop in node.properties)
	        {
		        ApplyElementProperty(element, prop);
	        }
	        
            ReconcileChildren(element, node.children);
            ReconcileComponents(element, node.components);
        }

        private void ApplyElementProperty(CanvasElement e, Node_Property prop)
        {
	        logger.LogDebug($"ApplyElementProperty '{e.key}' with {prop.name}");
	        
	        CanvasTransform t = e.transform;
	        Node_Expression expr = prop.value;

	        switch (prop.name)
	        {
		        case "isEnabled":
			        e.isEnabled = EvaluateValue<bool>(expr);
			        break;
		        
		        case "size":
			        if (expr is Node_TupleLiteral tuple && tuple.elements.Count >= 2)
			        {
				        t.size.x = ParseDimension(tuple.elements[0]);
				        t.size.y = ParseDimension(tuple.elements[1]);
			        }
			        else
			        {
				        float dim = ParseDimension(expr);
				        t.size.x = dim;
				        t.size.y = dim;
			        }
			        break;
		        
		        case "w":
		        case "width": 
			        t.size.x = ParseDimension(expr);
					break;
		        
		        case "h":
		        case "height":
			        t.size.y = ParseDimension(expr);
			        break;
		        
		        case "pos": 
			        t.pos = EvaluateValue<float2>(expr);
			        logger.LogDebug($"Pos: {t.pos}");
			        break;
		        
		        case "x":
			        t.pos.x = EvaluateValue<float>(expr);
			        break;
		        
		        case "y":
			        t.pos.y = EvaluateValue<float>(expr);
			        break;
		        
		        case "angle":
			        t.angle = EvaluateValue<float>(expr);
			        break;
	            
		        case "layer":
			        e.layerOffset = EvaluateValue<int>(expr);
			        break;
	            
		        default:
			        throw new ResolveException($"Invalid element property '{prop.name}'");
	        }
        }

        private void ReconcileComponents(CanvasElement element, List<Node_Component> nodes)
        {
	        logger.LogDebug($"ReconcileComponents '{element.key}' with {nodes.Count} components");
	        
            List<CanvasComponent> newComponents = null;
        
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!service.module.componentTypes.TryGetValue(node.typeName, out Type type)) continue;
        
                CanvasComponent comp = null;
                foreach (CanvasComponent c in element.Components)
                {
	                if (c.GetType() == type)
	                {
		                comp = c; 
		                break;
	                }
                }
        
                bool isNew = comp == null;
                if (isNew)
                {
                    comp = service.InstantiateComponent(type, container);
                    comp.Element = element;
                    element.AddComponent(comp);
                    
                    newComponents ??= new();
                    newComponents.Add(comp);
                }
        
                comp.id = node.id;
                foreach (Node_Property t in node.properties)
                {
	                ApplyComponentProperty(comp, t);
                }
            }
        
            if (newComponents != null)
            {
	            foreach (CanvasComponent c in newComponents)
	            {
		            WireFields(c, element);
		            c.OnAttached();
	            }
            }
        }

        private void ReconcileChildren(CanvasElement parent, List<Node_Element> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Node_Element node = nodes[i];
                CanvasElement child = null;

                if (!string.IsNullOrEmpty(node.key))
                {
	                foreach (CanvasElement parentChild in parent.Children)
	                {
		                if (parentChild.key == node.key)
		                {
			                child = parentChild;
			                break;
		                }
	                }
                }
                else if (i < parent.Children.Count && string.IsNullOrEmpty(parent.Children.ElementAt(i).key))
                {
                    child = parent.Children.ElementAt(i);
                }

                if (child == null)
                {
	                child = new()
	                {
		                key = node.key,
		                parent = parent,
		                service = service,
		                reconciler = this
	                };
	                parent.AddChild(child);
                }

                Reconcile(child, node);
            }

            if (parent.Children.Count > nodes.Count)
            {
	            for (int i = parent.Children.Count - 1; i >= nodes.Count; i--)
	            {
		            parent.RemoveChild(i);
	            }
            }
        }

        private void ApplyComponentProperty(CanvasComponent comp, Node_Property prop)
        {
            MemberInfo member = TypeMetadataCache.GetSettableMember(comp.GetType(), prop.name);
            if (member == null) return;

            Type targetType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;
            
            object value = EvaluateValue(prop.value, targetType);

            if (member is PropertyInfo p2) p2.SetValue(comp, value);
            else ((FieldInfo)member).SetValue(comp, value);
        }

        public void PostProcessBindings(CanvasElement root)
        {
	        foreach (CanvasComponent component in root.Components) WireFields(component, root);
	        foreach (CanvasElement child in root.Children) PostProcessBindings(child);
        }

        private void WireFields(CanvasComponent target, CanvasElement root)
        {
            var fields = TypeMetadataCache.GetWireFields(target.GetType(), service);

            for (int i = 0; i < fields.Length; i++)
            {
                ref var fInfo = ref fields[i];
                object value = null;

                if (fInfo.isComponent)
                {
                    if (fInfo.isTemplate)
                    {
                        value = Activator.CreateInstance(fInfo.fieldType);
                    }
                    else
                    {
                        value = FindComponentByID(root, fInfo.fieldType, fInfo.field.Name) ?? 
                                FindComponentByType(root, fInfo.fieldType);
                    }
                }
                else if (fInfo.isElement)
                {
                    value = FindElementByKey(root, fInfo.field.Name);
                }

                if (value != null)
                {
                    fInfo.field.SetValue(target, value);
                }
                else
                {
                    throw new WireException($"Requested element '{fInfo.field.Name}' of type {fInfo.fieldType} not found");
                }
            }
        }

        
        private CanvasComponent FindComponentByID(CanvasElement e, Type t, string id)
        {
	        foreach (CanvasComponent component in e.Components)
	        {
		        if (t.IsAssignableFrom(component.GetType()) && string.Equals(component.id, id, StringComparison.OrdinalIgnoreCase))
		        {
			        return component;
		        }
	        }
	        
	        foreach (CanvasElement child in e.Children)
	        {
		        var f = FindComponentByID(child, t, id);
		        if (f != null) return f;
	        }
	        
            return null;
        }

        private CanvasComponent FindComponentByType(CanvasElement e, Type t)
        {
	        foreach (CanvasComponent component in e.Components)
	        {
		        if (t.IsAssignableFrom(component.GetType())) return component;
	        }
	        
	        foreach (CanvasElement child in e.Children)
	        {
		        var f = FindComponentByType(child, t);
		        if (f != null) return f;
	        }
	        
            return null;
        }

        private CanvasElement FindElementByKey(CanvasElement e, string key)
        {
            if (string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase)) return e;
            foreach (CanvasElement child in e.Children)
            {
	            CanvasElement f = FindElementByKey(child, key);
	            if (f != null) return f;
            }
            return null;
        }

        private T EvaluateValue<T>(Node_Expression expr)
        {
	        return (T)EvaluateValue(expr, typeof(T));
        }
        
        private object EvaluateValue(Node_Expression expr, Type target)
        {
	        // Debug.Log($"{expr} for {target}");
	        
	        if (expr is Node_NumberLiteral numberLiteral)
	        {
		        if (target == typeof(int)) return (int)numberLiteral.value;
		        if (target == typeof(float2)) return new float2(numberLiteral.value);
		        if (target == typeof(float3)) return new float3(numberLiteral.value);
		        if (target == typeof(float4)) return new float4(numberLiteral.value);
		        
		        return numberLiteral.value;
	        }
	        
            if (expr is Node_StringLiteral stringLiteral)
            {
	            if (target == typeof(Sprite)) return service.assetDatabase.GetSprite(stringLiteral.value);
                return stringLiteral.value;
            }

            if (expr is Node_BooleanLiteral boolLiteral)
            {
	            return boolLiteral.value;
            }

            if (expr is Node_ColorLiteral colorLiteral)
            {
                ColorUtility.TryParseHtmlString(colorLiteral.hex, out Color col);
                return QualitySettings.activeColorSpace == ColorSpace.Linear ? col.linear : col;
            }
            
            if (expr is Node_IdentifierReference ident)
            {
                if (target.IsEnum) return Enum.Parse(target, ident.name, true);
                return ident.name;
            }
            
            if (expr is Node_TupleLiteral tuple)
            {
	            if (target == typeof(float2))
	            {
		            return new float2(
			            EvaluateValue<float>(tuple.elements[0]),
			            EvaluateValue<float>(tuple.elements[1]));
	            }
	            
	            if (target == typeof(float4))
	            {
		            return new float4(
			            EvaluateValue<float>(tuple.elements[0]),
			            EvaluateValue<float>(tuple.elements[1]), 
			            EvaluateValue<float>(tuple.elements[2]),  
			            EvaluateValue<float>(tuple.elements[3]));
	            }
            }

            throw new ResolveException($"Invalid expression '{expr?.GetType().Name}' for target type '{target?.Name}'");
        }

        private static float ParseDimension(Node_Expression expr)
        {
            if (expr is Node_NumberLiteral num) return num.value;
            throw new WireException($"Invalid dimension type: {expr.GetType().Name}");
        }
    }
}