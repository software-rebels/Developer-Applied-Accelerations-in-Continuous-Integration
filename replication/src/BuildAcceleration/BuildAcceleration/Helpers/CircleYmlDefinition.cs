using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;

namespace ForecastBuildTime.Helpers;

public class CircleYmlDefinition
{
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = default!;

    [YamlMember(Alias = "jobs")]
    public Dictionary<string, Job> Jobs { get; set; } = default!;

    public sealed class Job
    {
        private List<object>? _steps;

        /// <summary>
        /// Can be <see cref="string"/> or <see cref="Dictionary{TKey,TValue}"/> where TKey and TValue are both <see cref="object"/>.
        /// </summary>
        [YamlMember(Alias = "steps")]
        public List<object> Steps
        {
            get => _steps ??= new();
            set => _steps = value;
        }
    }
}

internal static class YamlResolver
{
    [return: NotNullIfNotNull(nameof(yamlObject))]
    public static T? ConvertToType<T>(object? yamlObject) where T : new()
    {
        return (T?)ConvertToType(yamlObject, typeof(T));
    }

    [return: NotNullIfNotNull(nameof(yamlObject))]
    private static object? ConvertToType(object? yamlObject, Type type)
    {
        if (yamlObject == null)
        {
            return default;
        }

        if (yamlObject.GetType().IsAssignableTo(type))
        {
            return yamlObject;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (yamlObject is not List<object?> yamlList)
            {
                throw new ArgumentException("Expected list");
            }
            var elementType = type.GetGenericArguments()[0];
            var list = (System.Collections.IList)type.GetConstructor(Array.Empty<Type>())!.Invoke(Array.Empty<object?>());
            foreach (var item in yamlList)
            {
                list.Add(ConvertToType(item, elementType));
            }
            return list;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (yamlObject is not Dictionary<object, object?> yamlDictionary)
            {
                throw new ArgumentException("Expected dictionary");
            }

            var types = type.GetGenericArguments();
            var keyType = types[0];
            var valueType = types[1];
            var ret = (System.Collections.IDictionary)type.GetConstructor(Array.Empty<Type>())!.Invoke(Array.Empty<object?>());
            foreach (var (key, value) in yamlDictionary)
            {
                ret.Add(ConvertToType(key, keyType), ConvertToType(value, valueType));
            }
            return ret;
        }

        if (yamlObject is Dictionary<object, object?> d)
        {
            var ret = type.GetConstructor(Array.Empty<Type>())!.Invoke(Array.Empty<object?>());
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var p in properties.Where(p => p.CanRead && p.CanWrite))
            {
                var attribute = p.GetCustomAttribute<YamlMemberAttribute>();
                var fieldName = attribute?.Alias ?? p.Name;
                var fieldType = p.PropertyType;
                p.SetValue(ret, ConvertToType(d.GetValueOrDefault(fieldName), fieldType));
            }
            return ret;
        }

        return Convert.ChangeType(yamlObject, type);
    }
}