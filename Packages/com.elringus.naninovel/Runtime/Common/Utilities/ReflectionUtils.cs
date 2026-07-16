using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Naninovel
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Uses <see cref="Type.GetField(string, BindingFlags)"/>, but also includes private fields from all the base types.
        /// In case multiple fields with equal names exist in different base types, will return only the first most-derived one.
        /// </summary>
        public static FieldInfo GetFieldWithInheritance (this Type type, string fieldName, BindingFlags flags = BindingFlags.Default)
        {
            if (type is null) return null;
            var field = type.GetField(fieldName, flags);
            return field ?? GetFieldWithInheritance(type.BaseType, fieldName, flags);
        }

        /// <summary>
        /// Returns data of the attribute with the specified type applied to the member or null.
        /// </summary>
        public static CustomAttributeData GetAttributeData<TAttribute> (MemberInfo member)
        {
            return member.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(TAttribute));
        }

        /// <summary>
        /// Returns data of all the attributes with the specified type applied to the member or null.
        /// </summary>
        public static IEnumerable<CustomAttributeData> GetAllAttributeData<TAttribute> (MemberInfo member)
        {
            return member.CustomAttributes.Where(a => a.AttributeType == typeof(TAttribute));
        }

        /// <summary>
        /// Returns value of an attribute associated with the specified member and argument index.
        /// </summary>
        public static TValue GetAttributeValue<TAttribute, TValue> (MemberInfo member, int index)
        {
            var data = GetAttributeData<TAttribute>(member);
            return GetAttributeValue<TAttribute, TValue>(data, index);
        }

        /// <summary>
        /// Returns value of an attribute associated with the specified attribute and argument index.
        /// </summary>
        public static TValue GetAttributeValue<TAttribute, TValue> (CustomAttributeData data, int index)
        {
            if (data is null || data.ConstructorArguments.Count <= index) return default;
            var value = data.ConstructorArguments[index].Value;
            if (value is not ReadOnlyCollection<CustomAttributeTypedArgument> col) return (TValue)value;
            var elementType = typeof(TValue).GetElementType()!;
            var rawArray = col.Select(i => Convert.ChangeType(i.Value, elementType)).ToArray();
            var typedArray = Array.CreateInstance(elementType, rawArray.Length);
            Array.Copy(rawArray, typedArray, rawArray.Length);
            return (TValue)(object)typedArray;
        }

        /// <summary>
        /// Returns values of the attributes associated with the specified member and argument index.
        /// </summary>
        public static IEnumerable<TValue> GetAttributeValues<TAttribute, TValue> (MemberInfo member, int index)
        {
            return GetAllAttributeData<TAttribute>(member)
                .Select(data => GetAttributeValue<TAttribute, TValue>(data, index));
        }

        /// <summary>
        /// Checks whether specified type is a static class.
        /// </summary>
        public static bool IsStaticClass (Type type)
        {
            return type.IsClass && type.IsAbstract && type.IsSealed;
        }
    }
}
