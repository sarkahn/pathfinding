using NUnit.Framework;
using Sark.Pathfinding;
using Sark.Pathfinding.Tests;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BasicPathingMapTest 
{

    [Test]
    public void TestGetPath()
    {
        var map = new BasicPathingMap(30, 30, Allocator.TempJob);
        var astar = new AStar<int>(map.Length, Allocator.TempJob);
        var path = new NativeList<int>(map.Length, Allocator.TempJob);

        int start = 10;
        int end = 13;

        new FindPathJob
        {
            AStar = astar,
            Map = map,
            Path = path,
            Start = start,
            End = end
        }.Run();

        Assert.AreNotEqual(0, path.Length);
        Assert.AreEqual(start, path[0]);
        Assert.AreEqual(end, path[path.Length - 1]);

        map.Dispose();
        path.Dispose();
        astar.Dispose();
    }

    [BurstCompile]
    struct FindPathJob : IJob
    {
        public AStar<int> AStar;
        public NativeList<int> Path;
        public BasicPathingMap Map;
        public int Start;
        public int End;

        public void Execute()
        {
            AStar.FindPath(Map, Start, End, Path);
        }
    }
}
