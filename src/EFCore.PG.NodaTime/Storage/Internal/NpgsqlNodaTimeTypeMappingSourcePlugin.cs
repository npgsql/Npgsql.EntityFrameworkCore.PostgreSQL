using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;

// ReSharper disable once CheckNamespace
namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal
{
    public class NpgsqlNodaTimeTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
    {
        public virtual ConcurrentDictionary<string, RelationalTypeMapping[]> StoreTypeMappings { get; }
        public virtual ConcurrentDictionary<Type, RelationalTypeMapping> ClrTypeMappings { get; }

        #region TypeMapping

        readonly TimestampInstantMapping _timestampInstant = new();
        readonly TimestampLocalDateTimeMapping _timestampLocalDateTime = new();

        readonly TimestampTzInstantMapping _timestamptzInstant = new();
        readonly TimestampTzZonedDateTimeMapping _timestamptzZonedDateTime = new();
        readonly TimestampTzOffsetDateTimeMapping _timestamptzOffsetDateTime = new();

        readonly DateMapping _date = new();
        readonly TimeMapping _time = new();
        readonly TimeTzMapping _timetz = new();
        readonly PeriodIntervalMapping _periodInterval = new();
        readonly DurationIntervalMapping _durationInterval = new();


        readonly NpgsqlRangeTypeMapping _timestampLocalDateTimeRange;
        readonly NpgsqlRangeTypeMapping _timestampInstantRange;
        readonly NpgsqlRangeTypeMapping _timestamptzInstantRange;
        readonly NpgsqlRangeTypeMapping _timestamptzZonedDateTimeRange;
        readonly NpgsqlRangeTypeMapping _timestamptzOffsetDateTimeRange;
        readonly NpgsqlRangeTypeMapping _dateRange;
        readonly DateIntervalMapping _dateInterval = new();

        #endregion

        /// <summary>
        /// Constructs an instance of the <see cref="NpgsqlNodaTimeTypeMappingSourcePlugin"/> class.
        /// </summary>
        public NpgsqlNodaTimeTypeMappingSourcePlugin([NotNull] ISqlGenerationHelper sqlGenerationHelper)
        {
            _timestampLocalDateTimeRange = new NpgsqlRangeTypeMapping("tsrange", typeof(NpgsqlRange<LocalDateTime>), _timestampLocalDateTime, sqlGenerationHelper);
            _timestampInstantRange = new NpgsqlRangeTypeMapping("tsrange", typeof(NpgsqlRange<Instant>), _timestampInstant, sqlGenerationHelper);
            _timestamptzInstantRange = new NpgsqlRangeTypeMapping("tstzrange", typeof(NpgsqlRange<Instant>), _timestamptzInstant, sqlGenerationHelper);
            _timestamptzZonedDateTimeRange = new NpgsqlRangeTypeMapping("tstzrange", typeof(NpgsqlRange<ZonedDateTime>), _timestamptzZonedDateTime, sqlGenerationHelper);
            _timestamptzOffsetDateTimeRange = new NpgsqlRangeTypeMapping("tstzrange", typeof(NpgsqlRange<OffsetDateTime>), _timestamptzOffsetDateTime, sqlGenerationHelper);
            _dateRange = new NpgsqlRangeTypeMapping("daterange", typeof(NpgsqlRange<LocalDate>), _date, sqlGenerationHelper);

            var storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "timestamp", new RelationalTypeMapping[] { _timestampInstant, _timestampLocalDateTime } },
                { "timestamp without time zone", new RelationalTypeMapping[] { _timestampInstant, _timestampLocalDateTime } },
                { "timestamptz", new RelationalTypeMapping[] { _timestamptzInstant, _timestamptzZonedDateTime, _timestamptzOffsetDateTime } },
                { "timestamp with time zone", new RelationalTypeMapping[] { _timestamptzInstant, _timestamptzZonedDateTime, _timestamptzOffsetDateTime } },
                { "date", new RelationalTypeMapping[] { _date } },
                { "time", new RelationalTypeMapping[] { _time } },
                { "time without time zone", new RelationalTypeMapping[] { _time } },
                { "timetz", new RelationalTypeMapping[] { _timetz } },
                { "time with time zone", new RelationalTypeMapping[] { _timetz } },
                { "interval", new RelationalTypeMapping[] { _periodInterval } },

                { "tsrange", new RelationalTypeMapping[] { _timestampInstantRange, _timestampLocalDateTimeRange } },
                { "tstzrange", new RelationalTypeMapping[] { _timestamptzInstantRange, _timestamptzZonedDateTimeRange, _timestamptzOffsetDateTimeRange } },
                { "daterange", new RelationalTypeMapping[] { _dateRange} }
            };

            var clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
            {
                { typeof(Instant), _timestampInstant },
                { typeof(LocalDateTime), _timestampLocalDateTime },
                { typeof(ZonedDateTime), _timestamptzZonedDateTime },
                { typeof(OffsetDateTime), _timestamptzOffsetDateTime },
                { typeof(LocalDate), _date },
                { typeof(LocalTime), _time },
                { typeof(OffsetTime), _timetz },
                { typeof(Period), _periodInterval },
                { typeof(Duration), _durationInterval },

                { typeof(NpgsqlRange<Instant>), _timestampInstantRange },
                { typeof(NpgsqlRange<LocalDateTime>), _timestampLocalDateTimeRange },
                { typeof(NpgsqlRange<ZonedDateTime>), _timestamptzZonedDateTimeRange },
                { typeof(NpgsqlRange<LocalDate>), _dateRange },
                { typeof(NpgsqlRange<OffsetDateTime>), _timestamptzOffsetDateTimeRange },

                { typeof(DateInterval), _dateInterval},
            };

            StoreTypeMappings = new ConcurrentDictionary<string, RelationalTypeMapping[]>(storeTypeMappings, StringComparer.OrdinalIgnoreCase);
            ClrTypeMappings = new ConcurrentDictionary<Type, RelationalTypeMapping>(clrTypeMappings);
        }

        public virtual RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
            => FindExistingMapping(mappingInfo) ?? FindArrayMapping(mappingInfo);

        protected virtual RelationalTypeMapping FindExistingMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            var storeTypeName = mappingInfo.StoreTypeName;
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

            if (storeTypeName != null)
            {
                if (StoreTypeMappings.TryGetValue(storeTypeName, out var mappings))
                {
                    if (clrType == null)
                        return mappings[0];

                    foreach (var m in mappings)
                        if (m.ClrType == clrType)
                            return m;

                    return null;
                }

                if (StoreTypeMappings.TryGetValue(storeTypeNameBase, out mappings))
                {
                    if (clrType == null)
                        return mappings[0].Clone(in mappingInfo);

                    foreach (var m in mappings)
                        if (m.ClrType == clrType)
                            return m.Clone(in mappingInfo);

                    return null;
                }
            }

            if (clrType == null || !ClrTypeMappings.TryGetValue(clrType, out var mapping))
                return null;

            // All PostgreSQL date/time types accept a precision except for date
            // TODO: Cache size/precision/scale mappings?
            return mappingInfo.Precision.HasValue && mapping.ClrType != typeof(LocalDate)
                ? mapping.Clone($"{mapping.StoreType}({mappingInfo.Precision.Value})", null)
                : mapping;
        }

        // TODO: This is duplicated from NpgsqlTypeMappingSource
        protected virtual RelationalTypeMapping FindArrayMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            Type elementClrType = null;

            if (clrType != null && !clrType.TryGetElementType(out elementClrType))
                return null; // Not an array/list

            var storeType = mappingInfo.StoreTypeName;
            var storeTypeNameBase = mappingInfo.StoreTypeNameBase;
            if (storeType != null)
            {
                // PostgreSQL array type names are the element plus []
                if (!storeType.EndsWith("[]"))
                    return null;

                var elementStoreType = storeType.Substring(0, storeType.Length - 2);
                var elementStoreTypeNameBase = storeTypeNameBase.Substring(0, storeTypeNameBase.Length - 2);

                RelationalTypeMapping elementMapping;

                if (elementClrType == null)
                {
                    elementMapping = FindMapping(new RelationalTypeMappingInfo(
                        elementStoreType, elementStoreTypeNameBase,
                        mappingInfo.IsUnicode, mappingInfo.Size, mappingInfo.Precision, mappingInfo.Scale));
                }
                else
                {
                    elementMapping = FindMapping(new RelationalTypeMappingInfo(
                        elementClrType, elementStoreType, elementStoreTypeNameBase,
                        mappingInfo.IsKeyOrIndex, mappingInfo.IsUnicode, mappingInfo.Size, mappingInfo.IsRowVersion,
                        mappingInfo.IsFixedLength, mappingInfo.Precision, mappingInfo.Scale));

                    // If an element mapping was found only with the help of a value converter, return null and EF will
                    // construct the corresponding array mapping with a value converter.
                    if (elementMapping?.Converter != null)
                        return null;
                }

                // If no mapping was found for the element, there's no mapping for the array.
                // Also, arrays of arrays aren't supported (as opposed to multidimensional arrays) by PostgreSQL
                if (elementMapping == null || elementMapping is NpgsqlArrayTypeMapping)
                    return null;

                return new NpgsqlArrayArrayTypeMapping(storeType, elementMapping);
            }

            if (clrType == null)
                return null;

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                Debug.Assert(elementType != null, "Detected array type but element type is null");

                // If an element isn't supported, neither is its array. If the element is only supported via value
                // conversion we also don't support it.
                var elementMapping = FindMapping(new RelationalTypeMappingInfo(elementType));
                if (elementMapping == null || elementMapping.Converter != null)
                    return null;

                // Arrays of arrays aren't supported (as opposed to multidimensional arrays) by PostgreSQL
                if (elementMapping is NpgsqlArrayTypeMapping)
                    return null;

                return new NpgsqlArrayArrayTypeMapping(elementMapping, clrType);
            }

            if (clrType.IsGenericList())
            {
                var elementType = clrType.GetGenericArguments()[0];

                // If an element isn't supported, neither is its array
                var elementMapping = FindMapping(new RelationalTypeMappingInfo(elementType));
                if (elementMapping == null)
                    return null;

                // Arrays of arrays aren't supported (as opposed to multidimensional arrays) by PostgreSQL
                if (elementMapping is NpgsqlArrayTypeMapping)
                    return null;

                return new NpgsqlArrayListTypeMapping(elementMapping, clrType);
            }

            return null;
        }
    }
}
