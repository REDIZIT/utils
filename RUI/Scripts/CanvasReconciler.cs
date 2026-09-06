using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class CanvasReconciler
    {
        private readonly CanvasService _service;
        private readonly DiContainer _container;

        public CanvasReconciler(CanvasService service, DiContainer container)
        {
            _service = service;
            _container = container;
        }

        // --- ОСНОВНЫЕ МЕТОДЫ RECONCILE ---

        public void Reconcile(CanvasElement element, Node_Element node)
        {
            if (_service.composerTypes.TryGetValue(node.composerType, out Type cType))
            {
                if (element.composer == null || element.composer.GetType() != cType)
                {
                    element.composer = (IComposer)Activator.CreateInstance(cType);
                    cType.GetField("e")?.SetValue(element.composer, element);
                }
            }

            foreach (var prop in node.properties)
            {
                if (!ApplyTransformProperty(element.transform, prop.name, prop.value))
                {
                    ApplyComposerProperty(element.composer, prop);
                }
            }

            ReconcileComponents(element, node.components);
            ReconcileChildren(element, node.children);
        }

        private void ReconcileComponents(CanvasElement element, List<Node_Component> nodes)
        {
            foreach (var node in nodes)
            {
                if (!_service.componentTypes.TryGetValue(node.typeName, out Type type)) continue;

                CanvasComponent comp = null;
                foreach (var c in element.components) if (c.GetType() == type) { comp = c; break; }

                bool isNew = comp == null;
                if (isNew)
                {
                    comp = (CanvasComponent)_container.Instantiate(type);
                    comp.Element = element;
                    element.components.Add(comp);
                }

                comp.id = node.id;
                foreach (var prop in node.properties) ApplyComponentProperty(comp, prop);
                if (isNew) comp.OnAttached();
            }
        }

        private void ReconcileChildren(CanvasElement parent, List<Node_Element> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                CanvasElement child = null;

                if (!string.IsNullOrEmpty(node.key))
                    child = parent.children.Find(c => c.key == node.key);
                else if (i < parent.children.Count && string.IsNullOrEmpty(parent.children[i].key))
                    child = parent.children[i];

                if (child == null)
                {
                    child = new CanvasElement { key = node.key, parent = parent };
                    parent.children.Add(child);
                }

                Reconcile(child, node);
            }

            if (parent.children.Count > nodes.Count)
                parent.children.RemoveRange(nodes.Count, parent.children.Count - nodes.Count);
        }

        // --- ПРИМЕНЕНИЕ СВОЙСТВ ---

        private void ApplyComponentProperty(CanvasComponent comp, Node_Property prop)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            var member = (MemberInfo)comp.GetType().GetProperty(prop.name, flags) ?? comp.GetType().GetField(prop.name, flags);
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
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            var member = (MemberInfo)composer.GetType().GetProperty(prop.name, flags) ?? composer.GetType().GetField(prop.name, flags);
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

        private bool ApplyTransformProperty(CanvasTransform t, string name, Node_Expression expr)
        {
            switch (name.ToLower())
            {
                case "size": t.size = (float2)CastValue(ConvertValue(expr, typeof(float2)), typeof(float2)); return true;
                case "pos": case "localpos": t.localPos = (float2)CastValue(ConvertValue(expr, typeof(float2)), typeof(float2)); return true;
                case "w": case "width": t.size.x = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
                case "h": case "height": t.size.y = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
                case "x": t.localPos.x = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
                case "y": t.localPos.y = (float)CastValue(ConvertValue(expr, typeof(float)), typeof(float)); return true;
                default: return false;
            }
        }

        // --- КОНВЕРТАЦИЯ И ПРИВЕДЕНИЕ ТИПОВ ---

        private object ConvertValue(Node_Expression expr, Type target)
        {
            if (expr is Node_NumberLiteral n) return n.value;
            if (expr is Node_StringLiteral s)
            {
                if (target == typeof(Sprite)) return _service.assetDatabase.GetSprite(s.value);
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
                if (target == typeof(Sprite)) return _service.assetDatabase.GetSprite(id.name);
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

        // Умное приведение типов: обрабатывает примитивы и расширение скаляров в векторы Unity.Mathematics
        private object CastValue(object val, Type targetType)
        {
            if (val == null) return null;
            Type valType = val.GetType();

            // 1. Если типы уже совпадают
            if (targetType.IsAssignableFrom(valType)) return val;

            // 2. Если нужно расширить число (float) до вектора (float2/float4)
            if (val is float f)
            {
                if (targetType == typeof(float2)) return new float2(f, f);
                if (targetType == typeof(float4)) return new float4(f, f, f, f);
                if (targetType == typeof(int)) return (int)f;
                if (targetType == typeof(double)) return (double)f;
                if (targetType == typeof(byte)) return (byte)f;
            }

            // 3. Стандартная конвертация для примитивов (int, float, и т.д.)
            try
            {
                return Convert.ChangeType(val, targetType, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        // --- POST PROCESS BINDINGS (Авто-связывание полей) ---

        public void PostProcessBindings(CanvasElement root)
        {
            foreach (var comp in root.components) WireFields(comp, root);
            foreach (var child in root.children) PostProcessBindings(child);
        }

        private void WireFields(CanvasComponent target, CanvasElement root)
        {
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                if (field.Name == "Element" || field.Name == "Transform" || field.Name == "id") continue;

                object value = null;
                if (typeof(CanvasComponent).IsAssignableFrom(field.FieldType))
                {
                    if (_service.templates.ContainsKey(field.FieldType)) value = Activator.CreateInstance(field.FieldType);
                    else value = FindComponentById(root, field.FieldType, field.Name) ?? FindComponentByType(root, field.FieldType);
                }
                else if (field.FieldType == typeof(CanvasElement)) value = FindElementByKey(root, field.Name);

                if (value != null) field.SetValue(target, value);
            }
        }

        private CanvasComponent FindComponentById(CanvasElement e, Type t, string id)
        {
            foreach (var c in e.components) if (t.IsAssignableFrom(c.GetType()) && string.Equals(c.id, id, StringComparison.OrdinalIgnoreCase)) return c;
            foreach (var child in e.children) { var f = FindComponentById(child, t, id); if (f != null) return f; }
            return null;
        }

        private CanvasComponent FindComponentByType(CanvasElement e, Type t)
        {
            foreach (var c in e.components) if (t.IsAssignableFrom(c.GetType())) return c;
            foreach (var child in e.children) { var f = FindComponentByType(child, t); if (f != null) return f; }
            return null;
        }

        private CanvasElement FindElementByKey(CanvasElement e, string key)
        {
            if (string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase)) return e;
            foreach (var child in e.children) { var f = FindElementByKey(child, key); if (f != null) return f; }
            return null;
        }
    }
}
