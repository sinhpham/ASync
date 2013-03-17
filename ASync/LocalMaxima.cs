using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class LocalMaxima
    {
        public LocalMaxima(int localMaximaH)
        {
            _localMaximaH = localMaximaH;
        }

        private readonly int _localMaximaH;
        public int LocalMaximaH { get { return _localMaximaH; } }
        private int BlockSize { get { return LocalMaximaH + 1; } }

        public static void StressTest()
        {
            var rnd = new Random(0);

            var start = 0;
            var count = 100000;

            var lm = new LocalMaxima(1000);

            for (var i = start; i <= start + count; ++i)
            {
                var list = new List<int>();
                for (var j = 0; j < i; ++j)
                {
                    list.Add(rnd.Next(0, 1000));
                }
                //var x0 = LocalMaximaNaive(list);
                var x1 = lm.CalcUsingNaive(list);
                var x2 = lm.CalcUsingBlockAlgo(list);



                //if (!Check(x0, x1))
                //{
                //    Console.WriteLine("Failed for localmaxima1");
                //}
                if (!Check(x1, x2))
                {
                    throw new InvalidOperationException("wrong ans");
                }
                Console.WriteLine("N list: {0} - N local maxima: {1}", list.Count, x1.Count);
            }
        }

        static bool Check(List<KeyValuePair<int, int>> trueList, List<KeyValuePair<int, int>> checkList)
        {
            if (checkList.Count != trueList.Count)
            {
                return false;
            }
            for (var i = 0; i < trueList.Count; ++i)
            {
                if (trueList[i].Key != checkList[i].Key || trueList[i].Value != checkList[i].Value)
                {
                    return false;
                }
            }
            return true;
        }

        List<KeyValuePair<int, int>> CalcUsingNaive(List<int> list)
        {
            var ret = new List<KeyValuePair<int, int>>();

            for (var i = 0; i < list.Count; ++i)
            {
                var isOk = true;
                for (var j = i - LocalMaximaH; j <= i + LocalMaximaH; ++j)
                {
                    if (j < 0 || j >= list.Count || j == i)
                    {
                        continue;
                    }
                    if (list[j] >= list[i])
                    {
                        isOk = false;
                        break;
                    }
                }
                if (isOk)
                {
                    ret.Add(new KeyValuePair<int, int>(i, list[i]));
                }
            }
            return ret;
        }

        List<KeyValuePair<int, int>> CalcUsingGreedySeqAlgo(List<int> list)
        {
            // Local maxima: F[i] max in [F[i - h], F[i + h]]
            var ret = new List<KeyValuePair<int, int>>();

            var isCandidate = true;
            var greedyList = new List<KeyValuePair<int, int>>();
            greedyList.Add(new KeyValuePair<int, int>(0, list[0]));

            for (var lIdx = 1; lIdx < list.Count; ++lIdx)
            {
                var removeIdx = greedyList.Count - 1;
                while (removeIdx >= 0)
                {
                    if (greedyList[removeIdx].Value > list[lIdx])
                    {
                        break;
                    }
                    --removeIdx;
                }
                ++removeIdx;
                var lastRemovedValueIsEqual = false;
                if (removeIdx >= 0 && removeIdx < greedyList.Count)
                {
                    if (greedyList[removeIdx].Value == list[lIdx])
                    {
                        lastRemovedValueIsEqual = true;
                    }
                    greedyList.RemoveRange(removeIdx, greedyList.Count - removeIdx);
                }


                if (greedyList.Count > 0 && greedyList[0].Key == lIdx - LocalMaximaH)
                {
                    if (isCandidate)
                    {
                        ret.Add(greedyList[0]);
                        isCandidate = false;
                    }
                }
                greedyList.Add(new KeyValuePair<int, int>(lIdx, list[lIdx]));
                if (greedyList.Count == 1)
                {
                    isCandidate = lastRemovedValueIsEqual ? false : true;
                }
                if (greedyList.Count > 0 && greedyList[0].Key == lIdx - LocalMaximaH)
                {
                    greedyList.RemoveAt(0);
                }
            }

            if (greedyList.Count > 0 && isCandidate)
            {
                ret.Add(greedyList[0]);
            }

            return ret;
        }

        List<KeyValuePair<int, int>> CalcUsingBlockAlgo(List<int> list)
        {
            var retPos = new BlockingCollectionDataChunk<int>();

            var inList = new BlockingCollectionDataChunk<uint>();
            foreach (var i in list)
            {
                inList.Add((uint)i);
            }
            inList.CompleteAdding();

            CalcUsingBlockAlgo(inList, retPos);

            return retPos.ToList().Select(pos => new KeyValuePair<int, int>(pos, list[pos])).ToList();
        }

        public void CalcUsingBlockAlgo(BlockingCollectionDataChunk<uint> inputList, BlockingCollectionDataChunk<int> outputPos)
        {
            var currPos = 0;

            var liveCanPrevBlockIdx = -1;
            var liveCanPrevBlockVal = 0U;
            var currBlockGreedySeq = new List<KeyValuePair<int, uint>>();
            var prevBlockGreedySeq = new List<KeyValuePair<int, uint>>();
            var currBlock = new uint[BlockSize];
            var prevBlock = new uint[BlockSize];
            var currWritingIdx = 0;

            foreach (var item in inputList.BlockingCollection.GetConsumingEnumerable())
            {
                var nItemLeft = item.DataSize;
                while (nItemLeft != 0)
                {
                    // Constructing current block.
                    var copyLength = Math.Min(nItemLeft, BlockSize - currWritingIdx);
                    Array.Copy(item.Data, item.DataSize - nItemLeft, currBlock, currWritingIdx, copyLength);
                    nItemLeft -= copyLength;
                    currWritingIdx += copyLength;

                    if (currWritingIdx == BlockSize)
                    {
                        var currLiveCanIdx = ProcessOneBlock(currBlock, currPos, currBlockGreedySeq,
                            prevBlock, liveCanPrevBlockIdx, liveCanPrevBlockVal, prevBlockGreedySeq,
                            outputPos);

                        // Move on to the next block.
                        prevBlockGreedySeq = currBlockGreedySeq;
                        currBlockGreedySeq = new List<KeyValuePair<int, uint>>();
                        liveCanPrevBlockIdx = currLiveCanIdx;
                        if (liveCanPrevBlockIdx != -1)
                        {
                            liveCanPrevBlockVal = currBlock[liveCanPrevBlockIdx];
                        }

                        var temp = currBlock;
                        currBlock = prevBlock;
                        prevBlock = temp;

                        currWritingIdx = 0;
                        currPos += BlockSize;
                    }
                }
            }
            if (currWritingIdx != 0)
            {
                // Handle non-full last block.
                // Zero the remaining array.
                Array.Clear(currBlock, currWritingIdx, currBlock.Length - currWritingIdx);

                var currLiveCanIdx = ProcessOneBlock(currBlock, currPos, currBlockGreedySeq,
                    prevBlock, liveCanPrevBlockIdx, liveCanPrevBlockVal, prevBlockGreedySeq,
                    outputPos);

                liveCanPrevBlockIdx = currLiveCanIdx;
                if (liveCanPrevBlockIdx != -1)
                {
                    liveCanPrevBlockVal = currBlock[liveCanPrevBlockIdx];
                }
                currPos += BlockSize;
            }
            if (liveCanPrevBlockIdx != -1)
            {
                outputPos.Add(currPos - prevBlock.Length + liveCanPrevBlockIdx);
            }
            outputPos.CompleteAdding();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ProcessOneBlock(IList<uint> currBlock, int currBlockStartPos, List<KeyValuePair<int, uint>> currGreedySeq,
            IList<uint> prevBlock, int liveCanPrevBlockIdx, uint liveCanPrevBlockVal, List<KeyValuePair<int, uint>> prevGreddySeq,
            BlockingCollectionDataChunk<int> localMaximaPos)
        {
            var currBlockStart = 0;
            var currBlockEnd = currBlock.Count - 1;

            currGreedySeq.Clear();
            currGreedySeq.Add(new KeyValuePair<int, uint>(currBlockEnd, currBlock[currBlockEnd]));
            var currLiveCanIdx = currBlockEnd;

            if (liveCanPrevBlockIdx == -1)
            {
                // No live candidate in the previous block, do the ordinary run.
                OrdinaryRun(currBlock, currBlockStart, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);
            }
            else
            {
                // Live candidate in the prev block, do modified run on this block
                // Ordinary until m + h (exclusive)
                OrdinaryRun(currBlock, liveCanPrevBlockIdx, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);

                var lastInCurrGreedySeq = currGreedySeq[currGreedySeq.Count - 1].Value;
                var modIdx = liveCanPrevBlockIdx - 1;

                if (lastInCurrGreedySeq >= liveCanPrevBlockVal)
                {
                    // F(g) >= F(m)
                    while (modIdx >= currBlockStart)
                    {
                        if (currBlock[modIdx] >= liveCanPrevBlockVal)
                        {
                            // Kill m
                            liveCanPrevBlockIdx = -1;
                            break;
                        }
                        --modIdx;
                    }
                    OrdinaryRun(currBlock, currBlockStart, modIdx, currGreedySeq, ref currLiveCanIdx);
                }
                else
                {
                    // F(g) < F(m)
                    var lastValue = currGreedySeq[currGreedySeq.Count - 1].Value;
                    while (modIdx >= currBlockStart)
                    {
                        if (currBlock[modIdx] > lastValue)
                        {
                            // Strictly greater than, add to the greedy sequence.
                            currGreedySeq.Add(new KeyValuePair<int, uint>(modIdx, currBlock[modIdx]));
                            lastValue = currBlock[modIdx];
                            currLiveCanIdx = modIdx;
                            if (currBlock[modIdx] >= liveCanPrevBlockVal)
                            {
                                // Kill m
                                liveCanPrevBlockIdx = -1;
                                --modIdx;
                                break;
                            }
                        }
                        else if (currBlock[modIdx] == lastValue)
                        {
                            // Equal: kill the current candidate but don't add to the greedy sequence.
                            currLiveCanIdx = -1;
                        }
                        --modIdx;
                    }
                    OrdinaryRun(currBlock, currBlockStart, modIdx, currGreedySeq, ref currLiveCanIdx);
                }
                // After the modified run, if the candidate in the previous block is still alive => add to output.
                if (liveCanPrevBlockIdx != -1)
                {
                    localMaximaPos.Add(currBlockStartPos - prevBlock.Count + liveCanPrevBlockIdx);
                }
            }

            // Check if current candidate satisfies all applicable items in the previous block.
            if (currLiveCanIdx != -1)
            {
                for (var i = 0; i < prevGreddySeq.Count; ++i)
                {
                    if (prevGreddySeq[i].Key < currLiveCanIdx + 1)
                    {
                        break;
                    }
                    if (prevGreddySeq[i].Value >= currBlock[currLiveCanIdx])
                    {
                        currLiveCanIdx = -1;
                        break;
                    }
                }
            }

            return currLiveCanIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OrdinaryRun(IList<uint> list, int start, int end, List<KeyValuePair<int, uint>> currGreedySeq, ref int currLiveCanIdx)
        {
            var lastValue = currGreedySeq[currGreedySeq.Count - 1].Value;
            for (var i = end; i >= start; --i)
            {
                if (list[i] > lastValue)
                {
                    // Strictly greater than, add to the greedy sequence.
                    currGreedySeq.Add(new KeyValuePair<int, uint>(i, list[i]));
                    lastValue = list[i];
                    currLiveCanIdx = i;
                    continue;
                }
                if (list[i] == lastValue)
                {
                    // Equal: kill the current candidate but don't add to the greedy sequence.
                    currLiveCanIdx = -1;
                }
            }
        }
    }
}
