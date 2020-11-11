using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using Unity.Jobs;
using UnityEngine;

using static Sark.Pathfinding.Tests.TestMapUtils;
using Unity.Collections;
using Sark.Pathfinding.Tests;
using Unity.Mathematics;
using Sark.Pathfinding;
using Unity.Burst;

public class AStarInt2PerformanceTests
{

    [Test, Performance]
    [TestCase(30)]
    [TestCase(50)]
    [TestCase(150)]
    [TestCase(350)]
    [TestCase(500)]
    [TestCase(1000)]
    public void MapWithObstaclesTest(int size)
    {
        
        RunTest((allocator)=> { return GetInt2MapWithObstacles(size, size, allocator); }, 35);
    }

    static NativeList<int2> GetPath(int len) => new NativeList<int2>(len, Allocator.TempJob);

    static public void RunTest(
        System.Func<Allocator, (TestMapInt2 map, int2 start, int2 end)> MapFunc, int iterations)
    {
        TestMapInt2 map = default;
        AStar<int2> aStar = default;
        NativeList<int2> path = default;
        int2 Start = default;
        int2 End = default;

        Measure.Method(() =>
        { 
            new PathJob
            {
                Map = map,
                AStar = aStar,
                Path = path,
                Start = Start,
                End = End
            }.Run();
        })
            .WarmupCount(3)
            .MeasurementCount(iterations)
            .SetUp(() =>
            {
                var (theMap, start, end) = MapFunc(Allocator.TempJob);
                map = theMap;
                path = GetPath(map.Length);
                aStar = new AStar<int2>(map.Length, Allocator.TempJob);
                Start = start;
                End = end;
            })
            .CleanUp(() =>
            {
                path.Dispose();
                aStar.Dispose();
                map.Dispose();
            })
            .Run();
    }

    [BurstCompile]
    struct PathJob : IJob
    {
        public AStar<int2> AStar;
        public NativeList<int2> Path;
        public TestMapInt2 Map;
        public int2 Start;
        public int2 End;

        public void Execute()
        {
            AStar.FindPath(Map, Start, End, Path);
        }
    }
}
