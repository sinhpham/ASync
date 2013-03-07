using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class LocalMaxima
    {
        static void StressTest()
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
                //var x2 = LocalMaxima2(list);



                //if (!Check(x0, x1))
                //{
                //    Console.WriteLine("Failed for localmaxima1");
                //}
                //if (!Check(x0, x2))
                //{
                //    Console.WriteLine("Failed for localmaxima2");
                //}
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

        static List<KeyValuePair<int, int>> LocalMaxima2(List<int> list)
        {
            var ret = new List<KeyValuePair<int, int>>();

            var h = 1000;
            var currBlockStart = 0;
            var currBlockEnd = h;

            var liveCanPrevBlockIdx = -1;
            var liveCanPrevBlockVal = 0;
            var prevBlockGreedySeq = new List<KeyValuePair<int, int>>();

            while (currBlockStart < list.Count)
            {
                if (currBlockEnd > list.Count - 1)
                {
                    currBlockEnd = list.Count - 1;
                }

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
                    var modIdx = liveCanPrevBlockIdx + h > currBlockEnd ? currBlockEnd : liveCanPrevBlockIdx + h;

                    if (lastInCurrGreedySeq >= liveCanPrevBlockVal)
                    {
                        // F(g) >= F(m)
                        while (modIdx >= currBlockStart)
                        {
                            if (list[modIdx] == liveCanPrevBlockVal)
                            {
                                // Kill m
                                liveCanPrevBlockIdx = -1;
                                --modIdx;
                                break;
                            }
                            if (list[modIdx] > liveCanPrevBlockVal)
                            {
                                // Kill m
                                liveCanPrevBlockIdx = -1;
                                break;
                            }
                            --modIdx;
                        }
                        modIdx = modIdx < currGreedySeq[currGreedySeq.Count - 1].Key ? modIdx : currGreedySeq[currGreedySeq.Count - 1].Key - 1;
                        OrdinaryRun(list, currBlockStart, modIdx, currGreedySeq, ref currLiveCanIdx);
                        if (liveCanPrevBlockIdx != -1)
                        {
                            ret.Add(new KeyValuePair<int, int>(liveCanPrevBlockIdx, liveCanPrevBlockVal));
                        }
                    }
                    else
                    {
                        // F(g) < F(m)
                        var lastValue = currGreedySeq[currGreedySeq.Count - 1].Value;
                        while (modIdx >= currBlockStart)
                        {
                            if (list[modIdx] > lastValue)
                            {
                                // Strictly greater than, add to the greedy sequence.
                                currGreedySeq.Add(new KeyValuePair<int, int>(modIdx, list[modIdx]));
                                lastValue = list[modIdx];
                                currLiveCanIdx = modIdx;
                                if (list[modIdx] >= liveCanPrevBlockVal)
                                {
                                    // Kill m
                                    liveCanPrevBlockIdx = -1;
                                    --modIdx;
                                    break;
                                }
                            }
                            if (list[modIdx] == lastValue)
                            {
                                // Equal: kill the current candidate but don't add to the greedy sequence.
                                currLiveCanIdx = -1;
                            }
                            --modIdx;
                        }
                        modIdx = modIdx < currGreedySeq[currGreedySeq.Count - 1].Key ? modIdx : currGreedySeq[currGreedySeq.Count - 1].Key - 1;
                        OrdinaryRun(list, currBlockStart, modIdx, currGreedySeq, ref currLiveCanIdx);
                        if (liveCanPrevBlockIdx != -1)
                        {
                            ret.Add(new KeyValuePair<int, int>(liveCanPrevBlockIdx, liveCanPrevBlockVal));
                        }
                    }
                }

                // Move on to the next block.
                if (currLiveCanIdx != -1)
                {
                    for (var i = 0; i < prevBlockGreedySeq.Count; ++i)
                    {
                        if (prevBlockGreedySeq[i].Key < currLiveCanIdx - h)
                        {
                            break;
                        }
                        if (prevBlockGreedySeq[i].Value >= list[currLiveCanIdx])
                        {
                            currLiveCanIdx = -1;
                            break;
                        }
                    }
                }

                prevBlockGreedySeq = currGreedySeq;
                liveCanPrevBlockIdx = currLiveCanIdx;
                if (liveCanPrevBlockIdx != -1)
                {
                    liveCanPrevBlockVal = list[liveCanPrevBlockIdx];
                }

                currBlockStart += h + 1;
                currBlockEnd += h + 1;

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
