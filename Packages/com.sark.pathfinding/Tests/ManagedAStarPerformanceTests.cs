using NUnit.Framework;
using Sark.Pathfinding.Tests;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;

[TestFixture]
public class ManagedAStarPerformanceTests
{

    [Test, Performance]
    [TestCase(30)]
    [TestCase(50)]
    [TestCase(150)]
    [TestCase(350)]
    public void MapWithObstacles(int size)
    {
        ManagedMap map = default;
        ManagedAStar aStar = default;
        List<int> path = default;

        int Start = 0;
        int End = 0;

        Measure.Method(() =>
        {
            aStar.FindPath(map, Start, End, path);
        })
            .WarmupCount(3)
            .MeasurementCount(35)
            .SetUp(() =>
            {
                var (theMap, start, end) = GetMapWithObstacles(size, size);
                map = theMap;
                path = new List<int>(map.Length);
                aStar = new ManagedAStar(map.Length);
                Start = start;
                End = end;
            })
            .CleanUp(() =>
            {
            })
            .Run();
    }

    public static (ManagedMap, int, int) GetMapWithObstacles(int w, int h)
    {
        var map = new ManagedMap(w, h);

        int x = w / 2;
        for (int y = 0; y < h - 2; ++y)
            map.SetTile(x, y, 1);


        int start = map.PosToIndex(0, 0);
        int end = map.PosToIndex(w - 1, 0);
        return (map, start, end);
    }
}