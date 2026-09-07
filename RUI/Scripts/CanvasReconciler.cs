using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class CanvasReconciler
    {
        private readonly CanvasService service;
        private readonly DiContainer container;

        public CanvasReconciler(CanvasService service, DiContainer container)
        {
            this.service = service;
            this.container = container;
        }

        public void Reconcile(CanvasElement element, Node_Element node)
        {
	        element.service = service;
	        element.reconciler = this;
	        
            if (node.composerType != null && service.module.composerTypes.TryGetValue(node.composerType, out Type cType))
            {
                if (element.composer == null || element.composer.GetType() != cType)
                {
                    element.composer = (IComposer)Activator.CreateInstance(cType);
                    cType.GetField("e")?.SetValue(element.composer, element);
                }
            }

            for (int i = 0; i < node.properties.Count; i++)
            {
                var prop = node.properties[i];
                if (prop.name.Equals("enabled", StringComparison.OrdinalIgnoreCase) ||
                    prop.name.Equals("isenabled", StringComparison.OrdinalIgnoreCase))
                {
                    object val = ConvertValue(prop.value, typeof(bool));
                    if (val is bool b) element.isEnabled = b;
                    continue;
                }

                if (!ApplyTransformProperty(element, prop.name, prop.value))
                {
                    ApplyComposerProperty(element.composer, prop);
                }
            }

            ReconcileChildren(element, node.children);
            ReconcileComponents(element, node.components);
        }

        private void ReconcileComponents(CanvasElement element, List<Node_Component> nodes)
        {
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
                for (int p = 0; p < node.properties.Count; p++)
                    ApplyComponentProperty(comp, node.properties[p]);
            }
        
            if (newComponents != null)
            {
                for (int i = 0; i < newComponents.Count; i++)
                {
                    var c = newComponents[i];
                    WireFields(c, element);
                    c.OnAttached();
                }
            }
        }

        private void ReconcileChildren(CanvasElement parent, List<Node_Element> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
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
            object rawVal = ConvertValue(prop.value, targetType);
            object finalVal = CastValue(rawVal, targetType);

            if (finalVal != null)
            {
                if (member is PropertyInfo p2) p2.SetValue(comp, finalVal);
                else ((FieldInfo)member).SetValue(comp, finalVal);
            }
        }

        private void ApplyComposerProperty(IComposer composer, Node_Property prop)
        {
            if (composer == null) return;
            MemberInfo member = TypeMetadataCache.GetSettableMember(composer.GetType(), prop.name);
            if (member == null) return;

            Type targetType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;
            object rawVal = ConvertValue(prop.value, targetType);
            object finalVal = CastValue(rawVal, targetType);

            if (finalVal != null)
            {
                if (member is PropertyInfo p2) p2.SetValue(composer, finalVal);
                else ((FieldInfo)member).SetValue(composer, finalVal);
            }
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

        private bool ApplyTransformProperty(CanvasElement e, string name, Node_Expression expr)
        {
	        return false;
	        
	        // CanvasTransform t = e.transform;

	        // switch (name.ToLower())
	        // {
	        //     case "size":
	        //         if (expr is Node_TupleLiteral tuple && tuple.elements.Count >= 2)
	        //         {
	        //             t.width = ParseDimension(tuple.elements[0]);
	        //             t.height = ParseDimension(tuple.elements[1]);
	        //         }
	        //         else
	        //         {
	        //             float? dim = ParseDimension(expr);
	        //             t.width = dim;
	        //             t.height = dim;
	        //         }
	        //         return true;
	        //     case "w": case "width": t.width = ParseDimension(expr); return true;
	        //     case "h": case "height": t.height = ParseDimension(expr); return true;
	        //     case "pos": case "localpos": t.localPos = (float2)CastValue(ConvertValue(expr, typeof(float2)), typeof(float2)); return true;
	        //     case "x": t.localPos.x = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
	        //     case "y": t.localPos.y = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
	        //     case "angle": case "rot": t.angle = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
	        //     
	        //     case "layer":
	        //     case "layeroffset":
	        //     case "order":
	        //      // Используем CastValue(..., typeof(int)), так как layerOffset - это int
	        //      e.layerOffset = (int)CastValue(ConvertValue(expr, typeof(int)), typeof(int));
	        //      return true;
	        //     
	        //     default: return false;
	        // }
        }

        private object ConvertValue(Node_Expression expr, Type target)
        {
            if (expr is Node_NumberLiteral n) return n.value;
            if (expr is Node_StringLiteral s)
            {
                if (target == typeof(Sprite)) return service.assetDatabase.GetSprite(s.value);
                if (target.IsEnum) return Enum.Parse(target, s.value, true);
                return s.value;
            }
            if (expr is Node_BooleanLiteral b) return b.value;
            if (expr is Node_ColorLiteral c)
            {
                ColorUtility.TryParseHtmlString(c.hex, out Color col);
                return QualitySettings.activeColorSpace == ColorSpace.Linear ? col.linear : col;
            }
            if (expr is Node_IdentifierReference id)
            {
                if (id.name == "auto") return 0f;
                if (id.name == "fill") return float.PositiveInfinity;
                if (target.IsEnum) return Enum.Parse(target, id.name, true);
                if (target == typeof(Sprite)) return service.assetDatabase.GetSprite(id.name);
                return id.name;
            }
            if (expr is Node_TupleLiteral t)
            {
                if (target == typeof(float2))
                    return new float2((float)CastValue(ConvertValue(t.elements[0], typeof(float)), typeof(float)), (float)CastValue(ConvertValue(t.elements[1], typeof(float)), typeof(float)));
                if (target == typeof(float4))
                    return new float4((float)CastValue(ConvertValue(t.elements[0], typeof(float)), typeof(float)), (float)CastValue(ConvertValue(t.elements[1], typeof(float)), typeof(float)), (float)CastValue(ConvertValue(t.elements[2], typeof(float)), typeof(float)), (float)CastValue(ConvertValue(t.elements[3], typeof(float)), typeof(float)));
            }
            return null;
        }

        private object CastValue(object val, Type targetType)
        {
            if (val == null) return null;
            Type valType = val.GetType();
            if (targetType.IsAssignableFrom(valType)) return val;

            if (val is float f)
            {
                if (targetType == typeof(float2)) return new float2(f, f);
                if (targetType == typeof(float4)) return new float4(f, f, f, f);
                if (targetType == typeof(int)) return (int)f;
                if (targetType == typeof(double)) return (double)f;
                if (targetType == typeof(byte)) return (byte)f;
            }

            try { return Convert.ChangeType(val, targetType, System.Globalization.CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        private static float? ParseDimension(Node_Expression expr)
        {
            if (expr is Node_NumberLiteral num) return num.value;
            if (expr is Node_IdentifierReference id && id.name.Equals("auto", StringComparison.OrdinalIgnoreCase)) return null;
            return null;
        }
    }
}