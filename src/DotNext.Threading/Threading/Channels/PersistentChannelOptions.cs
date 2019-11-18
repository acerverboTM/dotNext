﻿using System;
using System.IO;
using System.Threading.Channels;

namespace DotNext.Threading.Channels
{
    /// <summary>
    /// Represents persistent channel options.
    /// </summary>
    public sealed class PersistentChannelOptions : ChannelOptions
    {
        private const int DefaultRecordsPerPartition = 1000;
        private const int DefaultBufferSize = 4096;
        private string location;
        private int bufferSize;
        private int recordsPerPartition;

        /// <summary>
        /// Initializes a new options with default settings.
        /// </summary>
        public PersistentChannelOptions()
        {
            location = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            bufferSize = DefaultBufferSize;
            recordsPerPartition = DefaultRecordsPerPartition;
        }

        /// <summary>
        /// Gets or sets size of internal buffer used to perform I/O operations.
        /// </summary>
        public int BufferSize
        {
            get => bufferSize;
            set => bufferSize = value > 0 ? value : DefaultBufferSize;
        }

        /// <summary>
        /// Gets or sets path used to store queue files.
        /// </summary>
        public string Location
        {
            get => location;
            set => location = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets maximum number of records per file.
        /// </summary>
        public int RecordsPerPartition
        {
            get => recordsPerPartition;
            set => recordsPerPartition = value > 0 ? value : DefaultRecordsPerPartition;
        }

        /// <summary>
        /// Gets or sets initial size of partition file, in bytes. 
        /// </summary>
        /// <remarks>
        /// This property may help to avoid fragmentation of partition
        /// file on disk during writing.
        /// </remarks>
        public long InitialPartitionSize
        {
            get;
            set;
        }
    }
}
