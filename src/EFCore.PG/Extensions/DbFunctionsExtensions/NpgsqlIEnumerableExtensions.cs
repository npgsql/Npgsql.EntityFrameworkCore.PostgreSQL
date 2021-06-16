using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore
{
    public static class NpgsqlIEnumerableExtensions
    {
        public static TDest[] ArrayAgg<TDest>([NotNull]this IEnumerable<TDest> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static TDest[] ArrayAgg<TSource, TDest>([NotNull] this IEnumerable<TSource> _, [NotNull] Func<TSource, TDest> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static byte BitAnd([NotNull] this IEnumerable<byte> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static short BitAnd([NotNull] this IEnumerable<short> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static int BitAnd([NotNull] this IEnumerable<int> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static long BitAnd([NotNull] this IEnumerable<long> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static byte BitAnd<TSource>([NotNull] this IEnumerable<TSource> _, [NotNull] Func<TSource, byte> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static short BitAnd<TSource>([NotNull] this IEnumerable<TSource> _, [NotNull] Func<TSource, short> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static int BitAnd<TSource>([NotNull] this IEnumerable<TSource> _, [NotNull] Func<TSource, int> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static long BitAnd<TSource>([NotNull] this IEnumerable<TSource> _, [NotNull] Func<TSource, long> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static byte BitOr([NotNull] this IEnumerable<byte> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static short BitOr([NotNull] this IEnumerable<short> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static int BitOr([NotNull] this IEnumerable<int> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static long BitOr([NotNull] this IEnumerable<long> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static byte BitOr<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, byte> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static short BitOr<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, short> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static int BitOr<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, int> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static long BitOr<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, long> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static bool BoolAnd([NotNull]this IEnumerable<bool> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static bool BoolAnd<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, bool> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static bool BoolOr([NotNull]this IEnumerable<bool> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static bool BoolOr<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, bool> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static TDest[] JsonAgg<TDest>([NotNull]this IEnumerable<TDest> _)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static TDest[] JsonAgg<TSource, TDest>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, TDest> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static string JsonObjectAgg<TSource, TKey, TValue>([NotNull]this IEnumerable<TSource> _, [NotNull]Func<TSource, TKey> keySelector, [NotNull]Func<TSource, TValue> valueSelector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }

        public static string StringAgg([NotNull]this IEnumerable<string> _, [NotNull]string delimiter)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }
        public static string StringAgg<TSource>([NotNull]this IEnumerable<TSource> _, [NotNull]string delimiter, [NotNull]Func<TSource, string> selector)
        {
            throw new InvalidOperationException("This method is for use with Entity Framework Core only and has no in-memory implementation.");
        }


    }
}
