using Unity.Collections;
using Unity.Jobs;

using Sark.Common.NativeListExtensions;
using System;
using Unity.Burst;
using UnityEditor;
using UnityEngine.Profiling;

namespace Sark.Pathfinding
{
    // https://www.redblobgames.com/pathfinding/a-star/implementation.html
    /// <summary>
    /// A generic pathfinding struct that works in jobs and burst.
    /// </summary>
    /// <typeparam name="T">The type used to represent a point in the path. IE: int2, int.</typeparam>
    /// 
    public struct AStar<T> : INativeDisposable 
        where T : unmanaged, IEquatable<T>
    {
        NativePriorityQueue<T> _frontier;
        NativeHashMap<T,T> _parents;
        NativeHashMap<T,int> _costs;
        NativeList<T> _neighbours;

        public AStar(int len, Allocator allocator)
        {
            _frontier = new NativePriorityQueue<T>(len, allocator);
            _parents = new NativeHashMap<T, T>(len, allocator);
            _costs = new NativeHashMap<T, int>(len, allocator);
            _neighbours = new NativeList<T>(8, allocator);
        }

        public void FindPath<Map>(Map map, T start, T end, NativeList<T> output) 
            where Map : IPathingMap<T>
        {
            _frontier.Enqueue(start, 0);

            _costs[start] = 0;

            while (_frontier.Length > 0)
            {
                var currNode = _frontier.Dequeue();

                var curr = currNode;
                if (curr.Equals(end))
                    break;

                _neighbours.Clear();
                map.GetAvailableExits(curr, _neighbours);

                for (int i = 0; i < _neighbours.Length; ++i)
                {
                    var next = _neighbours[i];

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

        void GetPath(T start, T end, NativeList<T> output)
        {
            if (!_parents.ContainsKey(end))
                // Pathfinding failed
                return;

            T curr = end;

            while ( !curr.Equals(start) )
            {
                output.Insert(0, curr);
                curr = _parents[curr];
            }

            output.Insert(0, start);
        }

        public NativeKeyValueArrays<T,int> GetCosts(Allocator allocator)
        {
            return _costs.GetKeyValueArrays(allocator);
        }

        public NativeArray<T> GetVisited(Allocator allocator)
        {
            return _parents.GetKeyArray(allocator);
        }

        public void Clear()
        {
            _frontier.Clear();
            _costs.Clear();
            _parents.Clear();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            _frontier.Dispose(inputDeps);
            _parents.Dispose(inputDeps);
            _costs.Dispose(inputDeps);
            _neighbours.Dispose(inputDeps);
            return inputDeps;
        }

        public void Dispose()
        {
            _frontier.Dispose();
            _parents.Dispose();
            _costs.Dispose();
            _neighbours.Dispose();
        }
    }
}
