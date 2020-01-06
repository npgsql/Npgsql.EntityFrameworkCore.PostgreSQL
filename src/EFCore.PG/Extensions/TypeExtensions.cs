using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Npgsql.EntityFrameworkCore.PostgreSQL
{
    internal static class TypeExtensions
    {
        internal static bool IsGenericList(this Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        internal static bool IsArrayOrGenericList(this Type type)
            => type.IsArray || type.IsGenericList();
    }
}
