using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    /// Represents the Metadata for the partitioning of a table.
    /// </summary>
    public class TablePartitioning
    {
        /// <summary>
        /// The type of partitioning to use.
        /// </summary>
        public TablePartitioningType Type { get; }

        /// <summary>
        /// The entity's properties to use as key for the partitioning.
        /// </summary>
        public IReadOnlyProperty[] PartitionKeyProperties { get; }

        /// <summary>
        /// Creates a <see cref="TablePartitioning"/>.
        /// </summary>
        /// <param name="type">The type of partitioning to use.</param>
        /// <param name="partitionKeyProperties">The entity properties to use as key for the partitioning.</param>
        /// <exception cref="ArgumentException"><paramref name="partitionKeyProperties"/></exception>
        public TablePartitioning(TablePartitioningType type, IReadOnlyProperty[] partitionKeyProperties)
        {
            Check.NotEmpty(partitionKeyProperties, nameof(partitionKeyProperties));

            Type = type;
            PartitionKeyProperties = partitionKeyProperties;
        }
    }
}
