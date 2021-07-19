// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal
{
    internal static class PostgresXlDistributeByAnnotationNames
    {
        public const string Prefix = NpgsqlAnnotationNames.Prefix + "PostgresXL:";

        public const string DistributeBy = Prefix + "DistributeBy";
    }
}
