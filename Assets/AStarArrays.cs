using Unity.Collections;
using Unity.Jobs;

using Sark.Common.NativeListExtensions;
using Sark.Pathfinding.Samples;
using Unity.Burst;

namespace Sark.Pathfinding
{
    // https://www.redblobgames.com/pathfinding/a-star/implementation.html
    /// <summary>
    /// A generic pathfinding struct that works in jobs and burst.
    /// </summary>
    /// <typeparam name="int">The type used to represent a point in the path. IE: int2, int.</typeparam>
    /// 
    public struct AStarArrays : INativeDisposable 
    {
        NativePriorityQueue<int> _frontier;
        NativeArray<int> _parents;
        NativeArray<int> _costs;
        NativeList<int> _neighbours;

        public AStarArrays(int len, Allocator allocator)
        {
            _frontier = new NativePriorityQueue<int>(len, allocator);
            _parents = new NativeArray<int>(len, allocator);
            _costs = new NativeArray<int>(len, allocator);
            _neighbours = new NativeList<int>(len, allocator);

            Clear();
        }

        public void FindPath<Map>(Map map, int start, int end, NativeList<int> output) 
            where Map : IPathingMap<int>
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

                    int nextCost = _costs[next];
                    if(nextCost == -1 || newCost < nextCost)
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

        void GetPath(int start, int end, NativeList<int> output)
        {
            if (_parents[end] == -1)
                // Pathfinding failed
                return;

            int curr = end;

            while ( !curr.Equals(start) )
            {
                output.Insert(0, curr);
                curr = _parents[curr];
            }

            output.Insert(0, start);
        }

        //public NativeKeyValueArrays<int,int> GetCosts(Allocator allocator)
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
            for (int i = 0; i < _costs.Length; ++i)
                _costs[i] = -1;
            for (int i = 0; i < _parents.Length; ++i)
                _parents[i] = -1;
        }

        public PathJob GetJob(TestMapInt map, int start, int end, NativeList<int> path)
        {
            return new PathJob
            {
                AStar = this,
                Map = map,
                Path = path,
                Start = start,
                End = end
            };
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            _frontier.Dispose(inputDeps);
            _parents.Dispose(inputDeps);
            _costs.Dispose(inputDeps);
            return inputDeps;
        }

        public void Dispose()
        {
            _frontier.Dispose();
            _parents.Dispose();
            _costs.Dispose();
        }

        [BurstCompile]
        public struct PathJob : IJob
        {
            public AStarArrays AStar;
            public NativeList<int> Path;
            public TestMapInt Map;
            public int Start;
            public int End;

            public void Execute()
            {
                AStar.FindPath(Map, Start, End, Path);
            }
        }
    }
}
