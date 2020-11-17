using Sark.Pathfinding;
using Sark.Pathfinding.Tests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sark.Pathfinding.Tests
{
    public class ManagedAStar
    {
        FastPriorityQueue<int> _frontier;
        Dictionary<int, int> _parents;
        Dictionary<int, int> _costs;

        public ManagedAStar(int len)
        {
            _frontier = new FastPriorityQueue<int>(len);
            _parents = new Dictionary<int, int>(len);
            _costs = new Dictionary<int, int>(len);
        }

        public void FindPath(ManagedMap map, int start, int end, List<int> output)
        {
            _frontier.Enqueue(start, 0);
            _costs[start] = 0;

            while (_frontier.Count > 0)
            {
                var currNode = _frontier.Dequeue();

                var curr = currNode;
                if (curr.Equals(end))
                    break;

                var neighbours = map.GetAvailableExits(curr);

                for (int i = 0; i < neighbours.Count; ++i)
                {
                    var next = neighbours[i];

                    int newCost = _costs[curr] + map.GetCost(curr, next);

                    if (!_costs.TryGetValue(next, out int nextCost) || newCost < nextCost)
                    {
                        _costs[next] = newCost;
                        int priority = newCost + (int)map.GetDistance(next, end);
                        _frontier.Enqueue(next, priority);
                        _parents[next] = curr;
                    }
                }
            }

            GetPath(start, end, output);
        }

        void GetPath(int start, int end, List<int> output)
        {
            if (!_parents.ContainsKey(end))
                // Pathfinding failed
                return;

            int curr = end;

            while (!curr.Equals(start))
            {
                output.Insert(0, curr);
                curr = _parents[curr];
            }

            output.Insert(0, start);
        }

        //public NativeKeyValueArrays<int, int> GetCosts(Allocator allocator)
        //{
        //    return _costs.GetKeyValueArrays(allocator);
        //}

        //public NativeArray<int> GetVisited(Allocator allocator)
        //{
        //    return _parents.GetKeyArray(allocator);
        //}

        public void Clear()
        {
            _frontier.Clear();
            _costs.Clear();
            _parents.Clear();
        }
    }
}