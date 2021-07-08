using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RelationalPropertyExtensions = Microsoft.EntityFrameworkCore.RelationalPropertyExtensions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal
{
    public class NpgsqlAnnotationProvider : RelationalAnnotationProvider
    {
        public NpgsqlAnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
            : base(dependencies)
        {
        }

        public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
        {
            if (!designTime)
            {
                yield break;
            }

            // Model validation ensures that these facets are the same on all mapped entity types
            var entityType = table.EntityTypeMappings.First().EntityType;

            if (entityType.GetIsUnlogged())
                yield return new Annotation(NpgsqlAnnotationNames.UnloggedTable, entityType.GetIsUnlogged());
            if (entityType[CockroachDbAnnotationNames.InterleaveInParent] != null)
                yield return new Annotation(CockroachDbAnnotationNames.InterleaveInParent, entityType[CockroachDbAnnotationNames.InterleaveInParent]);
            if (entityType[NpgsqlAnnotationNames.TablePartitioning] != null)
                yield return new Annotation(NpgsqlAnnotationNames.TablePartitioning, entityType[NpgsqlAnnotationNames.TablePartitioning]);
            foreach (var storageParamAnnotation in entityType.GetAnnotations()
                .Where(a => a.Name.StartsWith(NpgsqlAnnotationNames.StorageParameterPrefix, StringComparison.Ordinal)))
            {
                yield return storageParamAnnotation;
            }
        }

        public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
        {
            if (!designTime)
            {
                yield break;
            }

            var table = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);
            var valueGeneratedProperty = column.PropertyMappings.Where(
                    m =>
                        m.TableMapping.IsSharedTablePrincipal && m.TableMapping.EntityType == m.Property.DeclaringEntityType)
                .Select(m => m.Property)
                .FirstOrDefault(
                    p => p.GetValueGenerationStrategy(table) switch
                    {
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn => true,
                        NpgsqlValueGenerationStrategy.IdentityAlwaysColumn    => true,
                        NpgsqlValueGenerationStrategy.SerialColumn            => true,
                        _                                                     => false
                    });

            if (valueGeneratedProperty != null)
            {
                var valueGenerationStrategy = valueGeneratedProperty.GetValueGenerationStrategy();
                yield return new Annotation(NpgsqlAnnotationNames.ValueGenerationStrategy, valueGenerationStrategy);

                if (valueGenerationStrategy == NpgsqlValueGenerationStrategy.IdentityByDefaultColumn ||
                    valueGenerationStrategy == NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                {
                    if (valueGeneratedProperty[NpgsqlAnnotationNames.IdentityOptions] is string identityOptions)
                    {
                        yield return new Annotation(NpgsqlAnnotationNames.IdentityOptions, identityOptions);
                    }
                }
            }

            // If the property has a collation explicitly defined on it via the standard EF mechanism, it will get
            // passed on the Collation property (we don't need to do anything).
            // Otherwise, a model-wide default column collation exists, pass that through our custom annotation.
            if (column.PropertyMappings.All(m => RelationalPropertyExtensions.GetCollation(m.Property) == null) &&
                column.PropertyMappings.Select(m => m.Property.GetDefaultCollation())
                    .FirstOrDefault(c => c != null) is string defaultColumnCollation)
            {
                yield return new Annotation(NpgsqlAnnotationNames.DefaultColumnCollation, defaultColumnCollation);
            }

            if (column.PropertyMappings.Select(m => m.Property.GetTsVectorConfig())
                .FirstOrDefault(c => c != null) is string tsVectorConfig)
            {
                yield return new Annotation(NpgsqlAnnotationNames.TsVectorConfig, tsVectorConfig);
            }

            valueGeneratedProperty = column.PropertyMappings.Select(m => m.Property)
                .FirstOrDefault(p => p.GetTsVectorProperties() != null);
            if (valueGeneratedProperty != null)
            {
                var tableIdentifier = StoreObjectIdentifier.Table(column.Table.Name, column.Table.Schema);

                yield return new Annotation(
                    NpgsqlAnnotationNames.TsVectorProperties,
                    valueGeneratedProperty.GetTsVectorProperties()!
                        .Select(p2 => valueGeneratedProperty.DeclaringEntityType.FindProperty(p2)!.GetColumnName(tableIdentifier))
                        .ToArray());
            }
        }

        public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
        {
            if (!designTime)
            {
                yield break;
            }

            // Model validation ensures that these facets are the same on all mapped indexes
            var modelIndex = index.MappedIndexes.First();

            if (modelIndex.GetCollation() is IReadOnlyList<string> collation)
                yield return new Annotation(RelationalAnnotationNames.Collation, collation);

            if (modelIndex.GetMethod() is string method)
                yield return new Annotation(NpgsqlAnnotationNames.IndexMethod, method);
            if (modelIndex.GetOperators() is IReadOnlyList<string> operators)
                yield return new Annotation(NpgsqlAnnotationNames.IndexOperators, operators);
            if (modelIndex.GetSortOrder() is IReadOnlyList<SortOrder> sortOrder)
                yield return new Annotation(NpgsqlAnnotationNames.IndexSortOrder, sortOrder);
            if (modelIndex.GetNullSortOrder() is IReadOnlyList<SortOrder> nullSortOrder)
                yield return new Annotation(NpgsqlAnnotationNames.IndexNullSortOrder, nullSortOrder);
            if (modelIndex.GetTsVectorConfig() is string configName)
                yield return new Annotation(NpgsqlAnnotationNames.TsVectorConfig, configName);
            if (modelIndex.GetIncludeProperties() is IReadOnlyList<string> includeProperties)
            {
                var tableIdentifier = StoreObjectIdentifier.Table(index.Table.Name, index.Table.Schema);

                yield return new Annotation(
                    NpgsqlAnnotationNames.IndexInclude,
                    includeProperties
                        .Select(p => modelIndex.DeclaringEntityType.FindProperty(p)!.GetColumnName(tableIdentifier))
                        .ToArray());
            }

            var isCreatedConcurrently = modelIndex.IsCreatedConcurrently();
            if (isCreatedConcurrently.HasValue)
            {
                yield return new Annotation(
                    NpgsqlAnnotationNames.CreatedConcurrently,
                    isCreatedConcurrently.Value);
            }
        }

        public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
        {
            if (!designTime)
            {
                return Array.Empty<IAnnotation>();
            }

            return model.Model.GetAnnotations().Where(
                a =>
                    a.Name.StartsWith(NpgsqlAnnotationNames.PostgresExtensionPrefix, StringComparison.Ordinal)
                    || a.Name.StartsWith(NpgsqlAnnotationNames.EnumPrefix, StringComparison.Ordinal)
                    || a.Name.StartsWith(NpgsqlAnnotationNames.RangePrefix, StringComparison.Ordinal)
                    || a.Name.StartsWith(NpgsqlAnnotationNames.CollationDefinitionPrefix, StringComparison.Ordinal));
        }
    }
}
