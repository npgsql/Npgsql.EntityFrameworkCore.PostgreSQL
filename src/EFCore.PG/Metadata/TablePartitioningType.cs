namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    /// Represents the supported forms of postgres table partitioning as defined in the official documentation:
    /// https://www.postgresql.org/docs/current/ddl-partitioning.html#DDL-PARTITIONING-OVERVIEW
    /// </summary>
    public enum TablePartitioningType
    {
        /// <summary>
        /// <para>
        /// The table is partitioned into “ranges” defined by a key column or set of columns, 
        /// with no overlap between the ranges of values assigned to different partitions.
        /// </para>
        /// </summary>
        Range,

        /// <summary>
        /// <para>
        /// The table is partitioned by explicitly listing which key value(s) appear in each partition.
        /// </para>
        /// </summary>
        List,

        /// <summary>
        /// The table is partitioned by specifying a modulus and a remainder for each partition
        /// </summary>
        Hash
    }
}
