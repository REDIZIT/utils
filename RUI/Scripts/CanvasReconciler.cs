using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
    public static class CanvasReconciler
    {
        public static void Reconcile(
            CanvasElement element, 
            Node_Element node, 
            Dictionary<string, Type> componentTypes, 
            DiContainer container,
            CanvasService service = null)
        {
            for (int i = 0; i < node.properties.Count; i++)
            {
                Node_Property prop = node.properties[i];
                ApplyTransformProperty(element.transform, prop.name, prop.value);
            }

            ReconcileComponents(element, node.components, componentTypes, container);
            ReconcileChildren(element, node.children, componentTypes, container, service);

            if (service != null)
            {
                PostProcessBindings(element, service);
            }
        }

        private static void ReconcileComponents(
            CanvasElement element, 
            List<Node_Component> componentNodes, 
            Dictionary<string, Type> componentTypes, 
            DiContainer container)
        {
            for (int i = 0; i < componentNodes.Count; i++)
            {
                Node_Component compNode = componentNodes[i];

                if (!componentTypes.TryGetValue(compNode.typeName, out Type compType))
                {
                    Debug.LogWarning($"[CanvasReconciler] Неизвестный тип компонента '{compNode.typeName}'!");
                    continue;
                }

                CanvasComponent existingComp = null;
                for (int c = 0; c < element.components.Count; c++)
                {
                    if (element.components[c].GetType() == compType)
                    {
                        existingComp = element.components[c];
                        break;
                    }
                }

                bool isNew = false;
                if (existingComp == null)
                {
	                existingComp = (CanvasComponent)container.Instantiate(compType);
	                existingComp.Element = element;
	                element.components.Add(existingComp);
	                isNew = true;
                }

                existingComp.id = compNode.id;

				// Применяем свойства из AST
                for (int p = 0; p < compNode.properties.Count; p++)
                {
	                Node_Property prop = compNode.properties[p];
	                ApplyComponentProperty(existingComp, prop);
                }

				// Если компонент новый — уведомляем его о монтировании!
                if (isNew)
                {
	                existingComp.OnAttached();
                }
            }
        }

        private static void ReconcileChildren(
            CanvasElement parent, 
            List<Node_Element> childNodes, 
            Dictionary<string, Type> componentTypes, 
            DiContainer container,
            CanvasService service)
        {
            Dictionary<string, CanvasElement> existingByKey = new Dictionary<string, CanvasElement>();
            for (int i = 0; i < parent.children.Count; i++)
            {
                CanvasElement child = parent.children[i];
                if (!string.IsNullOrEmpty(child.key) && !existingByKey.ContainsKey(child.key))
                {
                    existingByKey.Add(child.key, child);
                }
            }

            List<CanvasElement> updatedChildren = new List<CanvasElement>();

            for (int i = 0; i < childNodes.Count; i++)
            {
                Node_Element childAst = childNodes[i];
                string key = childAst.key;

                CanvasElement targetChild = null;

                if (!string.IsNullOrEmpty(key) && existingByKey.TryGetValue(key, out CanvasElement matched))
                {
                    targetChild = matched;
                    existingByKey.Remove(key);
                }
                else if (string.IsNullOrEmpty(key) && i < parent.children.Count && string.IsNullOrEmpty(parent.children[i].key))
                {
                    targetChild = parent.children[i];
                }
                else
                {
                    targetChild = new CanvasElement();
                    targetChild.key = key;
                    targetChild.parent = parent;
                }

                Reconcile(targetChild, childAst, componentTypes, container, service);
                updatedChildren.Add(targetChild);
            }

            parent.children.Clear();
            parent.children.AddRange(updatedChildren);
        }

        public static void PostProcessBindings(CanvasElement root, CanvasService service)
        {
            ResolveElementBindings(root, root, service);
        }

        private static void ResolveElementBindings(CanvasElement current, CanvasElement searchScopeRoot, CanvasService service)
        {
            for (int i = 0; i < current.components.Count; i++)
            {
                CanvasComponent comp = current.components[i];
                WireComponentFields(comp, searchScopeRoot, service);
            }

            for (int i = 0; i < current.children.Count; i++)
            {
                ResolveElementBindings(current.children[i], searchScopeRoot, service);
            }
        }

        private static void WireComponentFields(CanvasComponent targetComponent, CanvasElement rootScope, CanvasService service)
        {
            Type compType = targetComponent.GetType();

            FieldInfo[] fields = compType.GetFields(
                BindingFlags.Instance | 
                BindingFlags.Public | 
                BindingFlags.NonPublic | 
                BindingFlags.DeclaredOnly);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                Type fieldType = field.FieldType;

                if (field.Name == "Element" || field.Name == "Transform" || field.Name == "id")
                    continue;

                // 1. Поле ожидает CanvasElement
                if (fieldType == typeof(CanvasElement))
                {
                    CanvasElement matchedElement = FindElementByKey(rootScope, field.Name);
                    if (matchedElement != null)
                        field.SetValue(targetComponent, matchedElement);
                    continue;
                }

                // 2. Поле ожидает компонент
                if (typeof(CanvasComponent).IsAssignableFrom(fieldType))
                {
                    // А. Проверяем: может поле ожидает шаблон лота (например, private HierarchyLot lotTemplate)?
                    if (service != null && service.templates.TryGetValue(fieldType, out CanvasTemplate template))
                    {
                        // Создаем фантомный экземпляр-маркер для передачи в container.Refresh(lotTemplate, ...)
                        var dummyInstance = Activator.CreateInstance(fieldType);
                        field.SetValue(targetComponent, dummyInstance);
                        continue;
                    }

                    // Б. Поиск по ID компонента (Button#myStartBtn)
                    CanvasComponent matchedComp = FindComponentById(rootScope, fieldType, field.Name);

                    // В. Поиск по имени элемента
                    if (matchedComp == null)
                    {
                        CanvasElement matchedElem = FindElementByKey(rootScope, field.Name);
                        if (matchedElem != null)
                        {
                            matchedComp = matchedElem.GetComponent(fieldType);
                        }
                    }

                    // Г. Поиск по типу (для LotsContainer, где имя поля произвольное)
                    if (matchedComp == null)
                    {
                        matchedComp = FindComponentByType(targetComponent.Element, fieldType);
                        if (matchedComp == null)
                        {
                            matchedComp = FindComponentByType(rootScope, fieldType);
                        }
                    }

                    if (matchedComp != null)
                    {
                        field.SetValue(targetComponent, matchedComp);
                    }
                }
            }
        }

        private static CanvasComponent FindComponentById(CanvasElement element, Type targetType, string id)
        {
            for (int i = 0; i < element.components.Count; i++)
            {
                CanvasComponent c = element.components[i];
                if (targetType.IsAssignableFrom(c.GetType()) && string.Equals(c.id, id, StringComparison.OrdinalIgnoreCase))
                    return c;
            }

            for (int i = 0; i < element.children.Count; i++)
            {
                CanvasComponent found = FindComponentById(element.children[i], targetType, id);
                if (found != null) return found;
            }

            return null;
        }

        private static CanvasComponent FindComponentByType(CanvasElement element, Type targetType)
        {
            for (int i = 0; i < element.components.Count; i++)
            {
                CanvasComponent c = element.components[i];
                if (targetType.IsAssignableFrom(c.GetType()))
                    return c;
            }

            for (int i = 0; i < element.children.Count; i++)
            {
                CanvasComponent found = FindComponentByType(element.children[i], targetType);
                if (found != null) return found;
            }

            return null;
        }

        private static CanvasElement FindElementByKey(CanvasElement element, string key)
        {
            if (string.Equals(element.key, key, StringComparison.OrdinalIgnoreCase))
                return element;

            for (int i = 0; i < element.children.Count; i++)
            {
                CanvasElement found = FindElementByKey(element.children[i], key);
                if (found != null) return found;
            }

            return null;
        }

        public static void ApplyComponentProperty(CanvasComponent component, Node_Property prop)
        {
            Type compType = component.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            PropertyInfo property = compType.GetProperty(prop.name, flags);
            if (property != null && property.CanWrite)
            {
                object convertedValue = ConvertExpressionToType(prop.value, property.PropertyType);
                if (convertedValue != null)
                {
                    property.SetValue(component, convertedValue);
                }
                return;
            }

            FieldInfo field = compType.GetField(prop.name, flags);
            if (field != null)
            {
                object convertedValue = ConvertExpressionToType(prop.value, field.FieldType);
                if (convertedValue != null)
                {
                    field.SetValue(component, convertedValue);
                }
            }
        }

        public static bool ApplyTransformProperty(CanvasTransform transform, string name, Node_Expression expr)
        {
            string lower = name.ToLowerInvariant();

            if (lower == "size")
            {
                if (expr is Node_TupleLiteral tuple && tuple.values.Count >= 2)
                    transform.size = new float2(tuple.values[0], tuple.values[1]);
                else if (expr is Node_NumberLiteral num)
                    transform.size = new float2(num.value, num.value);
                return true;
            }

            if (lower == "width" || lower == "w")
            {
                if (expr is Node_NumberLiteral num) transform.size.x = num.value;
                return true;
            }

            if (lower == "height" || lower == "h")
            {
                if (expr is Node_NumberLiteral num) transform.size.y = num.value;
                return true;
            }

            if (lower == "localpos" || lower == "pos")
            {
                if (expr is Node_TupleLiteral tuple && tuple.values.Count >= 2)
                    transform.localPos = new float2(tuple.values[0], tuple.values[1]);
                else if (expr is Node_NumberLiteral num)
                    transform.localPos = new float2(num.value, num.value);
                return true;
            }

            if (lower == "scale")
            {
                if (expr is Node_TupleLiteral tuple && tuple.values.Count >= 2)
                    transform.scale = new float2(tuple.values[0], tuple.values[1]);
                else if (expr is Node_NumberLiteral num)
                    transform.scale = new float2(num.value, num.value);
                return true;
            }

            if (lower == "pivot")
            {
	            if (expr is Node_TupleLiteral tuple && tuple.values.Count >= 2)
		            transform.pivot = new float2(tuple.values[0], tuple.values[1]);
	            else if (expr is Node_NumberLiteral num)
		            transform.pivot = new float2(num.value, num.value);
	            return true;
            }
            
            return false;
        }

        public static object ConvertExpressionToType(Node_Expression expr, Type targetType)
        {
            if (targetType == typeof(string) && expr is Node_StringLiteral str)
                return str.value;

            if (targetType == typeof(float) && expr is Node_NumberLiteral numFloat)
                return numFloat.value;

            if (targetType == typeof(int) && expr is Node_NumberLiteral numInt)
                return (int)numInt.value;

            if (targetType == typeof(bool) && expr is Node_BooleanLiteral b)
                return b.value;

            if (targetType == typeof(Color))
            {
                if (expr is Node_ColorLiteral col && ColorUtility.TryParseHtmlString(col.hex, out Color parsedColor))
                    return parsedColor;
            }

            if (targetType == typeof(float2))
            {
                if (expr is Node_TupleLiteral tuple && tuple.values.Count >= 2)
                    return new float2(tuple.values[0], tuple.values[1]);
                if (expr is Node_NumberLiteral scalar)
                    return new float2(scalar.value, scalar.value);
            }

            if (targetType == typeof(float4))
            {
                if (expr is Node_TupleLiteral tuple)
                {
                    if (tuple.values.Count == 4)
                        return new float4(tuple.values[0], tuple.values[1], tuple.values[2], tuple.values[3]);
                    if (tuple.values.Count == 2)
                        return new float4(tuple.values[0], tuple.values[1], tuple.values[0], tuple.values[1]);
                }
                if (expr is Node_NumberLiteral scalar)
                    return new float4(scalar.value, scalar.value, scalar.value, scalar.value);
            }

            if (targetType.IsEnum)
            {
                if (expr is Node_IdentifierReference ident)
                    return Enum.Parse(targetType, ident.name, true);
                if (expr is Node_StringLiteral strEnum)
                    return Enum.Parse(targetType, strEnum.value, true);
            }

            return null;
        }
    }
}