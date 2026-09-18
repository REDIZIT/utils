using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

public static class ScopeBinder
{
    private static readonly Dictionary<Type, FieldInfo[]> fieldsCache = new();
    
    private static readonly HashSet<Type> ignoredTypes = new()
    {
        typeof(string),
        typeof(decimal)
    };

    public static void Bind(object target, ReactiveScope scope)
    {
        if (target == null || scope == null) return;
        
        // Используем стандартный HashSet с компарером по ссылке, 
        // чтобы избежать циклических ссылок и не зависеть от версии .NET
        var visited = new HashSet<object>(new IdentityComparer());
        BindInternal(target, scope, visited);
    }

    private static void BindInternal(object target, ReactiveScope scope, HashSet<object> visited)
    {
        if (target == null) return;

        Type type = target.GetType();

        if (type.IsPrimitive || type.IsEnum || ignoredTypes.Contains(type))
            return;

        if (!visited.Add(target))
            return;

        // 1. Если сам объект реактивный (RV, RList и т.д.)
        if (target is IReactive reactive)
        {
            reactive.Scope = scope;

            if (reactive.RawValue != null && !(reactive.RawValue is IEnumerable))
            {
                BindInternal(reactive.RawValue, scope, visited);
            }
        }

        // 2. Если это коллекция (RList, List и т.д.)
        if (target is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                    BindInternal(item, scope, visited);
            }
            return;
        }

        // 3. Если это обычный POCO-класс (ScanSession, ScanChecklist и т.д.)
        var fields = GetOrCacheFields(type);
        for (int i = 0; i < fields.Length; i++)
        {
            object val = fields[i].GetValue(target);
            if (val != null)
            {
                BindInternal(val, scope, visited);
            }
        }
    }

    private static FieldInfo[] GetOrCacheFields(Type type)
    {
        if (fieldsCache.TryGetValue(type, out var cached))
            return cached;

        var allFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var validFields = new List<FieldInfo>(allFields.Length);

        for (int i = 0; i < allFields.Length; i++)
        {
            var field = allFields[i];

            // ИГНОРИРУЕМ поля с [JsonIgnore] (например, uploadProgress)
            if (field.IsDefined(typeof(JsonIgnoreAttribute), true))
                continue;

            Type fType = field.FieldType;
            if (fType.IsPrimitive || fType.IsEnum || fType == typeof(string))
                continue;

            validFields.Add(field);
        }

        var result = validFields.ToArray();
        fieldsCache[type] = result;
        return result;
    }

    // Вспомогательный компарер для сравнения объектов строго по ссылке (ReferenceEquals)
    private sealed class IdentityComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
    }
}