using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for <see cref="IEntityType" /> for Npgsql-specific metadata.
    /// </summary>
    public static class NpgsqlEntityTypeExtensions
    {
        #region Storage parameters

        public static Dictionary<string, object?> GetStorageParameters(this IReadOnlyEntityType entityType)
            => entityType.GetAnnotations()
                .Where(a => a.Name.StartsWith(NpgsqlAnnotationNames.StorageParameterPrefix, StringComparison.Ordinal))
                .ToDictionary(
                    a => a.Name.Substring(NpgsqlAnnotationNames.StorageParameterPrefix.Length),
                    a => a.Value
                );

        public static string? GetStorageParameter(this IEntityType entityType, string parameterName)
        {
            Check.NotEmpty(parameterName, nameof(parameterName));

            return (string?)entityType[NpgsqlAnnotationNames.StorageParameterPrefix + parameterName];
        }

        public static void SetStorageParameter(
            this IMutableEntityType entityType,
            string parameterName,
            object? parameterValue)
        {
            Check.NotEmpty(parameterName, nameof(parameterName));

            entityType.SetOrRemoveAnnotation(NpgsqlAnnotationNames.StorageParameterPrefix + parameterName, parameterValue);
        }

        public static object SetStorageParameter(
            this IConventionEntityType entityType,
            string parameterName,
            object? parameterValue,
            bool fromDataAnnotation = false)
        {
            Check.NotEmpty(parameterName, nameof(parameterName));

            entityType.SetOrRemoveAnnotation(NpgsqlAnnotationNames.StorageParameterPrefix + parameterName, parameterValue, fromDataAnnotation);

            return parameterName;
        }

        public static ConfigurationSource? GetStorageParameterConfigurationSource(
            this IConventionEntityType index,
            string parameterName)
        {
            Check.NotEmpty(parameterName, nameof(parameterName));

            return index.FindAnnotation(NpgsqlAnnotationNames.StorageParameterPrefix + parameterName)?.GetConfigurationSource();
        }

        #endregion Storage parameters

        #region Unlogged

        public static bool GetIsUnlogged(this IReadOnlyEntityType entityType)
            => entityType[NpgsqlAnnotationNames.UnloggedTable] as bool? ?? false;

        public static void SetIsUnlogged(this IMutableEntityType entityType, bool unlogged)
            => entityType.SetOrRemoveAnnotation(NpgsqlAnnotationNames.UnloggedTable, unlogged);

        public static bool SetIsUnlogged(
            this IConventionEntityType entityType,
            bool unlogged,
            bool fromDataAnnotation = false)
        {
            entityType.SetOrRemoveAnnotation(NpgsqlAnnotationNames.UnloggedTable, unlogged, fromDataAnnotation);

            return unlogged;
        }

        public static ConfigurationSource? GetIsUnloggedConfigurationSource(this IConventionEntityType index)
            => index.FindAnnotation(NpgsqlAnnotationNames.UnloggedTable)?.GetConfigurationSource();

        #endregion Unlogged

        #region CockroachDb interleave in parent

        public static CockroachDbInterleaveInParent GetCockroachDbInterleaveInParent(this IReadOnlyEntityType entityType)
            => new(entityType);

        #endregion CockroachDb interleave in parent

        #region Postgres-xl Distribute By

        public static PostgresXlDistributeBy GetPostgresXlDistributeBy(this IReadOnlyEntityType entityType)
            => new(entityType);

        #endregion Postgres-xl Distribute By
    }
}
