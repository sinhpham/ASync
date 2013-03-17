using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class DataChunk<T>
    {
        public DataChunk()
            : this(4 * 1024) // Default buffer size
        {
        }

        public DataChunk(int bufferSize)
        {
            Data = new T[bufferSize];
        }

        public T[] Data;
        public int DataSize { get; private set; }

        public bool IsFull { get { return DataSize == Data.Length; } }

        public void Add(T item)
        {
            Data[DataSize] = item;
            ++DataSize;
        }
    }

    public class BlockingCollectionDataChunk<T>
    {
        public BlockingCollectionDataChunk()
        {
            BlockingCollection = new BlockingCollection<DataChunk<T>>();
            _currChunk = new DataChunk<T>();
        }

        public BlockingCollectionDataChunk(int chunkSize)
        {
            BlockingCollection = new BlockingCollection<DataChunk<T>>();
            _currChunk = new DataChunk<T>(chunkSize);
        }

        public BlockingCollection<DataChunk<T>> BlockingCollection { get; private set; }
        private DataChunk<T> _currChunk;

        public void Add(T item)
        {
            if (!_currChunk.IsFull)
            {
                _currChunk.Add(item);
                return;
            }
            // CurrChunk is full, need to create new one and add the current one to blocking collection
            BlockingCollection.Add(_currChunk);
            _currChunk = new DataChunk<T>();
            _currChunk.Add(item);
        }

        public void CompleteAdding()
        {
            BlockingCollection.Add(_currChunk);
            BlockingCollection.CompleteAdding();
            _currChunk = null;
        }
    }
}
