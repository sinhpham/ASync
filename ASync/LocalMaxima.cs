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

        public void StressTest()
        {
            var rnd = new Random(0);

            var start = 1000000;
            var count = 1000;

            for (var i = start; i <= start + count; ++i)
            {
                var list = new List<int>();
                for (var j = 0; j < i; ++j)
                {
                    list.Add(rnd.Next(0, 1000000000));
                }
                //var x0 = LocalMaximaNaive(list);
                var x1 = LocalMaximaNaive(list);
                var x2 = LocalMaxima2(list);



                //if (!Check(x0, x1))
                //{
                //    Console.WriteLine("Failed for localmaxima1");
                //}
                if (!Check(x1, x2))
                {
                    Console.WriteLine("Failed for localmaxima2");
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

        static List<KeyValuePair<int, int>> LocalMaximaNaive(List<int> list)
        {
            var ret = new List<KeyValuePair<int, int>>();

            var h = 1000;

            for (var i = 0; i < list.Count; ++i)
            {
                var isOk = true;
                for (var j = i - h; j <= i + h; ++j)
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

        static List<KeyValuePair<int, int>> LocalMaxima1(List<int> list)
        {
            // Local maxima: F[i] max in [F[i - h], F[i + h]]
            var ret = new List<KeyValuePair<int, int>>();

            var h = 1000;

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


                if (greedyList.Count > 0 && greedyList[0].Key == lIdx - h)
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
                if (greedyList.Count > 0 && greedyList[0].Key == lIdx - h)
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

        public List<KeyValuePair<int, int>> LocalMaxima2(List<int> list)
        {
            var ret = new ConcurrentQueue<KeyValuePair<int, int>>();

            var currBlockStart = 0;
            var currBlockEnd = LocalMaximaH;

            var liveCanPrevBlockIdx = -1;
            var liveCanPrevBlockVal = 0;
            var currBlockGreedySeq = new List<KeyValuePair<int, int>>();
            var prevBlockGreedySeq = new List<KeyValuePair<int, int>>();
            var currBlock = new int[LocalMaximaH + 1];
            var prevBlock = new int[LocalMaximaH + 1];

            while (currBlockStart < list.Count)
            {
                if (currBlockEnd > list.Count - 1)
                {
                    currBlockEnd = list.Count - 1;
                }
                // Construct current block
                
                for (var i = currBlockStart; i <= currBlockEnd; ++i)
                {
                    currBlock[i - currBlockStart] = list[i];
                }
                var afterEndIdx = currBlockEnd - currBlockStart + 1;
                if (afterEndIdx < currBlock.Length)
                {
                    Array.Clear(currBlock, afterEndIdx, currBlock.Length - afterEndIdx);
                }

                var currLiveCanIdx = LocalMaxima2Block(currBlock, currBlockStart, currBlockGreedySeq,
                    prevBlock, ref liveCanPrevBlockIdx, liveCanPrevBlockVal,
                    ret);

                // Move on to the next block.
                if (currLiveCanIdx != -1)
                {
                    for (var i = 0; i < prevBlockGreedySeq.Count; ++i)
                    {
                        if (prevBlockGreedySeq[i].Key < currLiveCanIdx +1)
                        {
                            break;
                        }
                        if (prevBlockGreedySeq[i].Value >= currBlock[currLiveCanIdx])
                        {
                            currLiveCanIdx = -1;
                            break;
                        }
                    }
                }

                prevBlockGreedySeq = currBlockGreedySeq;
                currBlockGreedySeq = new List<KeyValuePair<int, int>>();
                liveCanPrevBlockIdx = currLiveCanIdx;
                if (liveCanPrevBlockIdx != -1)
                {
                    liveCanPrevBlockVal = currBlock[liveCanPrevBlockIdx];
                }

                currBlockStart += LocalMaximaH + 1;
                currBlockEnd += LocalMaximaH + 1;

                var temp = currBlock;
                currBlock = prevBlock;
                prevBlock = temp;
            }
            if (liveCanPrevBlockIdx != -1)
            {
                var ans = new KeyValuePair<int, int>(currBlockStart - prevBlock.Length + liveCanPrevBlockIdx, liveCanPrevBlockVal);
                ret.Enqueue(ans);
            }

            return ret.ToList();
        }

        private readonly int _localMaximaH = 1000;
        public int LocalMaximaH { get { return _localMaximaH; } }

        private int LocalMaxima2Block(IList<int> currBlock, int currBlockStartPos, List<KeyValuePair<int, int>> currGreedySeq,
            IList<int> prevBlock, ref int liveCanPrevBlockIdx, int liveCanPrevBlockVal,
            ConcurrentQueue<KeyValuePair<int, int>> localMaximaPos)
        {
            var currBlockStart = 0;
            var currBlockEnd = currBlock.Count - 1;

            currGreedySeq.Clear();
            currGreedySeq.Add(new KeyValuePair<int, int>(currBlockEnd, currBlock[currBlockEnd]));
            var currLiveCanIdx = currBlockEnd;

            if (liveCanPrevBlockIdx == -1)
            {
                // No live candidate in the previous block, do the ordinary run.
                OrdinaryRun(currBlock, currBlockStart, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);
            }
            else
            {
                // Live candidate in the prev block, do modified run on this block
                // Ordinary until m + h
                OrdinaryRun(currBlock, liveCanPrevBlockIdx + LocalMaximaH + 1 - prevBlock.Count, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);

                var lastInCurrGreedySeq = currGreedySeq[currGreedySeq.Count - 1].Value;
                var modIdx = liveCanPrevBlockIdx + LocalMaximaH - prevBlock.Count;

                if (lastInCurrGreedySeq >= liveCanPrevBlockVal)
                {
                    // F(g) >= F(m)
                    while (modIdx >= currBlockStart)
                    {
                        if (currBlock[modIdx] == liveCanPrevBlockVal)
                        {
                            // Kill m
                            liveCanPrevBlockIdx = -1;
                            --modIdx;
                            break;
                        }
                        if (currBlock[modIdx] > liveCanPrevBlockVal)
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
                            currGreedySeq.Add(new KeyValuePair<int, int>(modIdx, currBlock[modIdx]));
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
                if (liveCanPrevBlockIdx != -1)
                {
                    var ans = new KeyValuePair<int, int>(currBlockStartPos - prevBlock.Count + liveCanPrevBlockIdx, liveCanPrevBlockVal);
                    localMaximaPos.Enqueue(ans);
                }
            }
            return currLiveCanIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OrdinaryRun(IList<int> list, int start, int end, List<KeyValuePair<int, int>> currGreedySeq, ref int currLiveCanIdx)
        {
            var lastValue = currGreedySeq[currGreedySeq.Count - 1].Value;
            for (var i = end; i >= start; --i)
            {
                if (list[i] > lastValue)
                {
                    // Strictly greater than, add to the greedy sequence.
                    currGreedySeq.Add(new KeyValuePair<int, int>(i, list[i]));
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
