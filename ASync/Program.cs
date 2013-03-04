using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

namespace ASync
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new List<int>() { 9, 21, 19, 13, 26, 18, 18, 25, 24, 25, 25, 35 };
            var x = LocalMaxima2(l);
        }

        static void WriteFileHashValues(Stream fileStream, Stream outStream)
        {
            var blockSize = 3;
            var windowsSize = 2;
        }

        static List<KeyValuePair<int, int>> LocalMaxima1(List<int> list)
        {
            // Local maxima: F[i] max in [F[i - h], F[i + h]]
            var ret = new List<KeyValuePair<int, int>>();

            var h = 2;

            var isCandidate = true;
            var tempList = new List<KeyValuePair<int, int>>();
            tempList.Add(new KeyValuePair<int, int>(0, list[0]));

            for (var i = 1; i < list.Count; ++i)
            {
                while (tempList.Count > 0 && tempList[tempList.Count - 1].Value <= list[i])
                {
                    tempList.RemoveAt(tempList.Count - 1);
                }
                if (tempList.Count > 0 && tempList[0].Key == i - h)
                {
                    if (isCandidate)
                    {
                        ret.Add(tempList[0]);
                        isCandidate = false;
                    }
                    tempList.RemoveAt(0);
                }
                tempList.Add(new KeyValuePair<int, int>(i, list[i]));
                if (tempList.Count == 1)
                {
                    isCandidate = true;
                }
            }

            if (tempList.Count > 0 && isCandidate)
            {
                ret.Add(tempList[0]);
            }

            return ret;
        }

        static List<KeyValuePair<int, int>> LocalMaxima2(List<int> list)
        {
            var ret = new List<KeyValuePair<int, int>>();

            var h = 2;
            var currBlockStart = 0;
            var currBlockEnd = h;

            var liveCanPrevBlockIdx = -1;
            var liveCanPrevBlockVal = 0;
            var prevBlockGreedySeq = new List<KeyValuePair<int, int>>();

            while (currBlockStart < list.Count)
            {
                var currGreedySeq = new List<KeyValuePair<int, int>>();
                currGreedySeq.Add(new KeyValuePair<int, int>(currBlockEnd, list[currBlockEnd]));
                var currLiveCanIdx = currBlockEnd;

                if (liveCanPrevBlockIdx == -1)
                {
                    // No live candidate in the previous block, do the ordinary run.
                    OrdinaryRun(list, currBlockStart, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);
                }
                else
                {
                    // Live candidate in the prev block, do modified run on this block
                    // Ordinary until m + h
                    OrdinaryRun(list, liveCanPrevBlockIdx + h + 1, currBlockEnd - 1, currGreedySeq, ref currLiveCanIdx);

                    var lastInCurrGreedySeq = currGreedySeq[currGreedySeq.Count - 1].Value;
                    if (lastInCurrGreedySeq >= liveCanPrevBlockVal)
                    {
                        // F(g) >= F(m)
                        var i = liveCanPrevBlockIdx + h > currBlockEnd ? currBlockEnd : liveCanPrevBlockIdx + h;
                        while (i >= currBlockStart)
                        {
                            if (list[i] == liveCanPrevBlockVal)
                            {
                                // Kill m
                                liveCanPrevBlockIdx = -1;
                                --i;
                                break;
                            }
                            if (list[i] > liveCanPrevBlockVal)
                            {
                                // Kill m
                                liveCanPrevBlockIdx = -1;
                                break;
                            }
                            --i;
                        }
                        i = i < currGreedySeq[currGreedySeq.Count - 1].Key ? i : currGreedySeq[currGreedySeq.Count - 1].Key - 1;
                        OrdinaryRun(list, currBlockStart, i, currGreedySeq, ref currLiveCanIdx);
                        if (liveCanPrevBlockIdx != -1)
                        {
                            ret.Add(new KeyValuePair<int, int>(liveCanPrevBlockIdx, liveCanPrevBlockVal));
                        }
                    }
                    else
                    {
                        // F(g) < F(m)
                        var i = liveCanPrevBlockIdx + h;
                        var lastValue = currGreedySeq[currGreedySeq.Count - 1].Value;
                        while (i >= currBlockStart)
                        {
                            if (list[i] > lastValue)
                            {
                                // Strictly greater than, add to the greedy sequence.
                                currGreedySeq.Add(new KeyValuePair<int, int>(i, list[i]));
                                lastValue = list[i];
                                currLiveCanIdx = i;
                                if (list[i] >= liveCanPrevBlockVal)
                                {
                                    // Kill m
                                    liveCanPrevBlockIdx = -1;
                                    --i;
                                    break;
                                }
                            }
                            if (list[i] == lastValue)
                            {
                                // Equal: kill the current candidate but don't add to the greedy sequence.
                                currLiveCanIdx = -1;
                            }
                            --i;
                        }
                        i = i < currGreedySeq[currGreedySeq.Count - 1].Key ? i : currGreedySeq[currGreedySeq.Count - 1].Key - 1;
                        OrdinaryRun(list, currBlockStart, i, currGreedySeq, ref currLiveCanIdx);
                        if (liveCanPrevBlockIdx != -1)
                        {
                            ret.Add(new KeyValuePair<int, int>(liveCanPrevBlockIdx, liveCanPrevBlockVal));
                        }
                    }
                }

                // Move on to the next block.
                liveCanPrevBlockIdx = currLiveCanIdx;
                if (liveCanPrevBlockIdx != -1)
                {
                    liveCanPrevBlockVal = list[liveCanPrevBlockIdx];
                }

                currBlockStart += h + 1;
                currBlockEnd += h + 1;
                if (currBlockEnd > list.Count - 1)
                {
                    currBlockEnd = list.Count - 1;
                }
            }
            if (liveCanPrevBlockIdx != -1)
            {
                ret.Add(new KeyValuePair<int, int>(liveCanPrevBlockIdx, liveCanPrevBlockVal));
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OrdinaryRun(List<int> list, int start, int end, List<KeyValuePair<int, int>> currGreedySeq, ref int currLiveCanIdx)
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
