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

public class AStarIntPerformanceTests
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
        
        RunTest((allocator)=> { return GetIntMapWithObstacles(size, size, allocator); }, 35);
    }

    static NativeList<int> GetPath(int len) => new NativeList<int>(len, Allocator.TempJob);

    static public void RunTest(
        System.Func<Allocator, (TestMapInt map, int start, int end)> MapFunc, int iterations)
    {
        TestMapInt map = default;
        AStar<int> aStar = default;
        NativeList<int> path = default;
        int Start = default;
        int End = default;

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
                aStar = new AStar<int>(map.Length, Allocator.TempJob);
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
        public AStar<int> AStar;
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
