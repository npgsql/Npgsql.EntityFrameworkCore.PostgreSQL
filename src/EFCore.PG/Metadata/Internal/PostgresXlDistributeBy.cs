// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable
using System;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal
{
    public class PostgresXlDistributeBy
    {
        private const string AnnotationName = PostgresXlDistributeByAnnotationNames.DistributeBy;

        private readonly IReadOnlyAnnotatable _annotatable;

        public virtual Annotatable Annotatable
            => (Annotatable)_annotatable;

        public PostgresXlDistributeBy(IReadOnlyAnnotatable annotatable)
            => _annotatable = annotatable;

        public virtual PostgresXlDistributeByStrategy DistributionStrategy
        {
            get => GetData().DistributionStrategy;
            set
            {
                var (_, distributeByColumnFunction, distributionStyle, columnName) = GetData();
                SetData(value, distributeByColumnFunction, distributionStyle, columnName);
            }
        }

        public virtual PostgresXlDistributeByColumnFunction DistributeByColumnFunction
        {
            get => GetData().DistributeByColumnFunction;
            set
            {
                var (distributionStrategy, _, distributionStyle, columnName) = GetData();
                SetData(distributionStrategy, value, distributionStyle, columnName);
            }
        }

        public virtual PostgresXlDistributionStyle DistributionStyle
        {
            get => GetData().DistributionStyle;
            set
            {
                var (distributionStrategy, distributeByColumnFunction, _, columnName) = GetData();
                SetData(distributionStrategy, distributeByColumnFunction, value, columnName);
            }
        }

        public virtual string? DistributeByPropertyName
        {
            get => GetData().ColumnName;
            set
            {
                var (distributionStrategy, distributeByColumnFunction, distributionStyle, _) = GetData();
                SetData(distributionStrategy, distributeByColumnFunction, distributionStyle, value);
            }
        }

        public void Deconstruct(out PostgresXlDistributeByStrategy distributionStrategy,
            out PostgresXlDistributeByColumnFunction distributeByColumnFunction,
            out PostgresXlDistributionStyle distributionStyle,
            out string? distributeByColumnName)
        {
            distributionStrategy = DistributionStrategy;
            distributeByColumnFunction = DistributeByColumnFunction;
            distributionStyle = DistributionStyle;
            distributeByColumnName = DistributeByPropertyName;
        }

        private (PostgresXlDistributeByStrategy DistributionStrategy, PostgresXlDistributeByColumnFunction DistributeByColumnFunction, PostgresXlDistributionStyle DistributionStyle, string? ColumnName) GetData()
        {
            var str = Annotatable[AnnotationName] as string;
            return str == null
                ? (0, 0, 0, null)
                : Deserialize(str);
        }

        private void SetData(
            PostgresXlDistributeByStrategy distributionStrategy,
            PostgresXlDistributeByColumnFunction distributeByColumnFunction,
            PostgresXlDistributionStyle postgresXlDistributionStyle,
            string? distributeByColumnName)
        {
            Annotatable[AnnotationName] = Serialize(distributionStrategy, distributeByColumnFunction, postgresXlDistributionStyle, distributeByColumnName);
        }

        private string Serialize(
            PostgresXlDistributeByStrategy distributionStrategy,
            PostgresXlDistributeByColumnFunction distributeByColumnFunction,
            PostgresXlDistributionStyle postgresXlDistributionStyle,
            string? distributeByColumnName)
        {
            var stringBuilder = new StringBuilder();

            EscapeAndQuote(stringBuilder, distributionStrategy);
            stringBuilder.Append(",");
            EscapeAndQuote(stringBuilder, distributeByColumnFunction);
            stringBuilder.Append(",");
            EscapeAndQuote(stringBuilder, postgresXlDistributionStyle);
            stringBuilder.Append(",");
            EscapeAndQuote(stringBuilder, distributeByColumnName);

            return stringBuilder.ToString();
        }

        private (PostgresXlDistributeByStrategy DistributionStrategy,
            PostgresXlDistributeByColumnFunction DistributeByColumnFunction,
            PostgresXlDistributionStyle DistributionStyle,
            string? ColumnName)
            Deserialize(string str)
        {
            var position = 0;
            var distributionStrategy = Enum.Parse<PostgresXlDistributeByStrategy>(ExtractValue(str, ref position)!);
            var distributeByColumnFunction = Enum.Parse<PostgresXlDistributeByColumnFunction>(ExtractValue(str, ref position)!);
            var distributionStyle = Enum.Parse<PostgresXlDistributionStyle>(ExtractValue(str, ref position)!);
            var columnName = ExtractValue(str, ref position);

            return (distributionStrategy, distributeByColumnFunction, distributionStyle, columnName);
        }

        private static void EscapeAndQuote(StringBuilder builder, object? value)
        {
            builder.Append("'");

            if (value != null)
            {
                builder.Append(value.ToString()?.Replace("'", "''"));
            }

            builder.Append("'");
        }

        private static string? ExtractValue(string value, ref int position)
        {
            position = value.IndexOf('\'', position) + 1;

            var end = value.IndexOf('\'', position);

            while (end + 1 < value.Length
                && value[end + 1] == '\'')
            {
                end = value.IndexOf('\'', end + 2);
            }

            var extracted = value.Substring(position, end - position).Replace("''", "'");
            position = end + 1;

            return extracted.Length == 0 ? null : extracted;
        }
    }
}
