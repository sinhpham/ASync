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
        const int BufferSize = 1024 * 4;

        public T[] Data = new T[BufferSize];
        public int DataSize { get; private set; }

        public bool IsFull { get { return DataSize == BufferSize; } }

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
