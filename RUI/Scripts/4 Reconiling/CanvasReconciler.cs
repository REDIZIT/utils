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

        // Очередь новых компонентов, ожидающих вызова OnAttached
        private readonly List<CanvasComponent> newComponentsQueue = new();

        public CanvasReconciler(CanvasService service, DiContainer container)
        {
            this.service = service;
            this.container = container;
            logger = container.Resolve<ILogger<CanvasReconciler>>();
        }

        public TTemplate Spawn<TTemplate>(CanvasElement parent) where TTemplate : CanvasComponent
        {
	        if (!service.module.templates.TryGetValue(typeof(TTemplate), out CanvasTemplate template))
	        {
		        throw new($"Template of type {typeof(TTemplate)} not found");
	        }
	        return Spawn(template, parent).TryGetComponent<TTemplate>();
        }
        
        public CanvasElement Spawn(CanvasTemplate template, CanvasElement parent, string key = null)
        {
	        CanvasElement inst = new()
	        {
		        key = key ?? template.templateAst.key,
		        parent = parent,
		        service = service,
		        reconciler = this
	        };

            // 1. Добавляем к родителю сразу, чтобы при связывании полей инстанс мог подниматься к родителю
            if (parent != null)
            {
                parent.AddChild(inst);
            }
        
	        Reconcile(inst, template.templateAst);
	        PostProcessBindings(inst);
            NotifyAttached(); // Активируем OnAttached для компонентов шаблона

	        return inst;
        }

        public void Reconcile(CanvasElement element, Node_Element node)
        {
	        logger.LogDebug($"Reconcile '{element}' with {node} and {node.properties.Count} properties");
	        
	        element.sourceNode = node;
	        element.service = service;
	        element.reconciler = this;
	        
	        foreach (Node_Property prop in node.properties)
	        {
		        ApplyElementProperty(element, prop);
	        }
	        
            ReconcileChildren(element, node.children);
            ReconcileComponents(element, node.components);
            ApplyStylesToElement(element);
        }

        private void ApplyElementProperty(CanvasElement e, Node_Property prop)
        {
	        logger.LogDebug($"ApplyElementProperty '{e.key}' with {prop.name}");
	        
	        Node_Expression expr = prop.value;

	        switch (prop.name)
	        {
		        case "isEnabled":
			        e.isEnabled = EvaluateValue<bool>(expr);
			        break;
		        
		        case "layer":
			        e.layerOffset = EvaluateValue<int>(expr);
			        break;
		        
		        case "style":
			        e.style = EvaluateValue<string>(expr);
			        break;
	            
		        default:
			        throw new ResolveException($"Invalid element property '{prop.name}' at '{e.GetPath()}'");
	        }
        }

        private void ReconcileComponents(CanvasElement element, List<Node_Component> nodes)
        {
	        logger.LogDebug($"ReconcileComponents '{element.key}' with {nodes.Count} components");
        
            for (int i = 0; i < nodes.Count; i++)
            {
                Node_Component node = nodes[i];
                if (service.module.componentTypes.TryGetValue(node.typeName, out Type type) == false)
                {
	                throw new WireException($"Component type '{node.typeName}' not found for element {element.GetPath()}");
                }
        
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
                    
                    // Сохраняем в очередь на OnAttached, но НЕ вызываем его прямо сейчас!
                    newComponentsQueue.Add(comp);
                }
        
                comp.id = node.id;
            }
        }

        // Вызывается ПОСЛЕ того, как все дерево построено и все поля связаны через PostProcessBindings
        public void NotifyAttached()
        {
            if (newComponentsQueue.Count == 0) return;

            // Копируем список на случай, если OnAttached вызовет создание новых компонентов
            var componentsToNotify = newComponentsQueue.ToArray();
            newComponentsQueue.Clear();

            for (int i = 0; i < componentsToNotify.Length; i++)
            {
                var comp = componentsToNotify[i];
                if (comp.Element != null)
                {
                    comp.OnAttached();
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
        
        public void ApplyStylesToElement(CanvasElement element)
        {
	        string effectiveStyleName = element.GetEffectiveStyle();
	        CanvasStyle style = null;
	        if (!string.IsNullOrEmpty(effectiveStyleName))
	        {
		        service.module.TryGetStyle(effectiveStyleName, out style);
	        }

	        Node_Element sourceNode = element.sourceNode;

	        foreach (CanvasComponent comp in element.Components)
	        {
		        Type compType = comp.GetType();
		        string typeName = compType.Name;

		        // 1. Сначала применяем значения из стиля (базовые дефолты)
		        if (style != null && style.components.TryGetValue(typeName, out Node_Component styleComp))
		        {
			        foreach (Node_Property prop in styleComp.properties)
			        {
				        ApplyComponentProperty(comp, prop);
			        }
		        }

		        // 2. Поверх перезаписываем свойствами, явно указанными на самом элементе
		        if (sourceNode != null)
		        {
			        foreach (Node_Component nodeComp in sourceNode.components)
			        {
				        if (service.module.componentTypes.TryGetValue(nodeComp.typeName, out Type t) && t == compType)
				        {
					        foreach (Node_Property prop in nodeComp.properties)
					        {
						        ApplyComponentProperty(comp, prop);
					        }
				        }
			        }
		        }
	        }
        }

        private void ApplyComponentProperty(CanvasComponent comp, Node_Property prop)
        {
            MemberInfo member = TypeMetadataCache.GetSettableMember(comp.GetType(), prop.name);
            if (member == null)
            {
	            throw new WireException($"Component '{comp}' has no settable member for property '{prop.name}'");
            }

            Type targetType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;

            object value;
            if (targetType.IsInheritedFrom(typeof(CanvasComponent)))
            {
	            if (TryResolveComponentDependency(comp.Element, targetType, out CanvasComponent component, prop.name))
	            {
		            value = component;
	            }
	            else
	            {
		            throw new WireException($"Component {targetType} '{prop.name}' not found for {comp} at or inside element '{comp.Element.key}'");
	            }
            }
            else
            {
	            value = EvaluateValue(prop.value, targetType);
            }

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
                ref TypeMetadataCache.WireFieldInfo fInfo = ref fields[i];
                object value = null;

                if (fInfo.isComponent)
                {
	                if (TryResolveComponentDependency(root, fInfo.fieldType, out CanvasComponent comp, fInfo.field.Name))
	                {
		                value = comp;
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
                    throw new WireException($"Requested element '{fInfo.field.Name}' of type {fInfo.fieldType} not found while wiring component {target} at {root.GetPath()}");
                }
            }
        }
        
        private bool TryResolveComponentDependency(CanvasElement startElement, Type componentType, out CanvasComponent comp, string? componentName = null)
		{
		    CanvasComponent firstTypeMatch = null;

		    // 1. Проверяем СЕБЯ
		    if (CheckElementComponents(startElement, componentType, componentName, ref firstTypeMatch, out comp))
		    {
		        return true; // Найдено точное совпадение (Тип + ID)
		    }

		    // 2. Ищем ВНИЗ по поддереву
		    foreach (CanvasElement child in startElement.Children)
		    {
		        if (SearchDownDFS(child, componentType, componentName, ref firstTypeMatch, out comp))
		        {
		            return true;
		        }
		    }
		    
		    // ВАЖНАЯ ЗАЩИТА: Если на САМОМ элементе уже есть компонент нужного типа, 
		    // используем его и НЕ лезем искать по чужим веткам дерева!
		    if (firstTypeMatch != null)
		    {
			    comp = firstTypeMatch;
			    return true;
		    }

		    // 3. Поднимаемся НАВЕРХ и проверяем соседние ветки
		    CanvasElement childBranch = startElement;
		    CanvasElement current = startElement.parent;

		    while (current != null)
		    {
		        if (CheckElementComponents(current, componentType, componentName, ref firstTypeMatch, out comp))
		        {
		            return true;
		        }

		        foreach (CanvasElement sibling in current.Children)
		        {
		            if (sibling == childBranch) continue;

		            if (SearchDownDFS(sibling, componentType, componentName, ref firstTypeMatch, out comp))
		            {
		                return true;
		            }
		        }

		        childBranch = current;
		        current = current.parent;
		    }

		    if (firstTypeMatch != null)
		    {
		        comp = firstTypeMatch;
		        return true;
		    }

		    comp = null;
		    return false;
		}

		private bool SearchDownDFS(CanvasElement current, Type componentType, string? componentName, ref CanvasComponent firstTypeMatch, out CanvasComponent exactMatch)
		{
		    if (CheckElementComponents(current, componentType, componentName, ref firstTypeMatch, out exactMatch))
		    {
		        return true;
		    }

		    foreach (CanvasElement child in current.Children)
		    {
		        if (SearchDownDFS(child, componentType, componentName, ref firstTypeMatch, out exactMatch))
		        {
		            return true;
		        }
		    }

		    exactMatch = null;
		    return false;
		}

		private bool CheckElementComponents(CanvasElement element, Type componentType, string? componentName, ref CanvasComponent firstTypeMatch, out CanvasComponent exactMatch)
		{
		    exactMatch = null;
		    bool hasTargetName = !string.IsNullOrEmpty(componentName);

		    foreach (CanvasComponent c in element.Components)
		    {
		        if (componentType.IsAssignableFrom(c.GetType()))
		        {
		            if (hasTargetName)
		            {
		                if (string.Equals(c.id, componentName, StringComparison.OrdinalIgnoreCase))
		                {
		                    exactMatch = c;
		                    return true;
		                }

		                firstTypeMatch ??= c;
		            }
		            else
		            {
		                exactMatch = c;
		                return true;
		            }
		        }
		    }

		    return false;
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
		            if (tuple.elements.Count == 2)
		            {
			            float xz = EvaluateValue<float>(tuple.elements[0]);
			            float yw = EvaluateValue<float>(tuple.elements[1]);
			            return new float4(xz, yw, xz, yw);
		            }
		            else if (tuple.elements.Count == 4)
		            {
			            return new float4(
				            EvaluateValue<float>(tuple.elements[0]),
				            EvaluateValue<float>(tuple.elements[1]), 
				            EvaluateValue<float>(tuple.elements[2]),  
				            EvaluateValue<float>(tuple.elements[3]));
		            }
		            else
		            {
			            throw new ResolveException($"Invalid float4 tuple count = {tuple.elements.Count}");
		            }
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