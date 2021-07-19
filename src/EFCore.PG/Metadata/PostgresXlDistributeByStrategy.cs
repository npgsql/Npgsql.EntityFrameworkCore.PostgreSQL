// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public enum PostgresXlDistributeByStrategy
    {
        None = 0,
        Replication = 1,
        RoundRobin = 2,
        Randomly = 3,
    }
}
