using System;
using System.Collections.Generic;
using System.Reflection;

namespace REDIZIT.RUI
{
    public static class TypeMetadataCache
    {
        public struct WireFieldInfo
        {
            public FieldInfo field;
            public Type fieldType;
            public bool isComponent;
            public bool isElement;
            public bool isTemplate;
        }

        private static readonly Dictionary<Type, WireFieldInfo[]> wireFieldsCache = new();
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> membersCache = new();

        public static WireFieldInfo[] GetWireFields(Type type, CanvasService service)
        {
            if (wireFieldsCache.TryGetValue(type, out var cached))
                return cached;

            var rawFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            var list = new List<WireFieldInfo>();

            for (int i = 0; i < rawFields.Length; i++)
            {
                var f = rawFields[i];
                if (f.Name == "Element" || f.Name == "Transform" || f.Name == "id") continue;
                if (f.IsDefined(typeof(WireIgnoreAttribute), false)) continue; // Быстрая проверка без аллокаций GetCustomAttribute!

                Type ft = f.FieldType;
                bool isComp = typeof(CanvasComponent).IsAssignableFrom(ft);
                bool isElem = ft == typeof(CanvasElement);

                if (isComp || isElem)
                {
                    list.Add(new WireFieldInfo
                    {
                        field = f,
                        fieldType = ft,
                        isComponent = isComp,
                        isElement = isElem,
                        isTemplate = isComp && service.module.templates.ContainsKey(ft)
                    });
                }
            }

            var result = list.ToArray();
            wireFieldsCache[type] = result;
            return result;
        }

        public static MemberInfo GetSettableMember(Type type, string memberName)
        {
            if (!membersCache.TryGetValue(type, out var memberDict))
            {
                memberDict = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);
                membersCache[type] = memberDict;
            }

            if (memberDict.TryGetValue(memberName, out var m))
                return m;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            MemberInfo found = (MemberInfo)type.GetProperty(memberName, flags) ?? type.GetField(memberName, flags);

            memberDict[memberName] = found;
            return found;
        }
    }
}