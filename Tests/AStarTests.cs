using NUnit.Framework;

using static Unity.Collections.Allocator;
using static Sark.Pathfinding.Tests.TestMapUtils;

using Sark.Pathfinding;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Sark.Pathfinding.Tests;
using System;

public class AStarTests
{
    [Test]
    public void TestPathfind()
    {
        string str = 
            ".........." +
            ".S........" +
            "***......." +
            ".E........" +
            "..........";
        var (map, start, end) = GetInt2MapFromString(10, 5, str, Allocator.Temp);
        var astar = new AStar<int2>(map.Length, Temp);
        var path = new NativeList<int2>(map.Length, Temp);

        astar.FindPath(map, start, end, path);

        Assert.IsFalse(path.IsEmpty);

        Assert.AreEqual(start, path[0]);
        Assert.AreEqual(end, path[path.Length - 1]);

        Assert.AreEqual(new int2(2, 3), path[1]);
        Assert.AreEqual(new int2(3, 3), path[2]);
        Assert.AreEqual(new int2(3, 2), path[3]);
        Assert.AreEqual(new int2(3, 1), path[4]);
        Assert.AreEqual(new int2(2, 1), path[5]);
    }

    [Test]
    public void TestWindyPathFind()
    {
        string str =
            ".....*......" +
            "..*..*.*...." +
            "..*..*.****." +
            "..*..*....*." +
            ".S*..****.*." +
            "..*.......*E";

        var (map, start, end) = GetInt2MapFromString(12, 6, str, Allocator.Temp);
        var astar = new AStar<int2>(map.Length, Temp);
        var path = new NativeList<int2>(map.Length, Temp);

        astar.FindPath(map, start, end, path);

        Assert.AreEqual(36, path.Length);

        Assert.AreEqual(new int2(1, 1), path[0]);
        Assert.AreEqual(new int2(11, 0), path[path.Length - 1]);
    }

    [Test]
    public void BurstTest()
    {
        var path = new NativeList<int>(100, Allocator.TempJob);
        var (map, start, end) = GetIntMapWithObstacles(10, 10, Allocator.TempJob);
        var astar = new AStar<int>(map.Length, Allocator.TempJob);

        RunJob(astar, map, start, end, path);

        Assert.AreEqual(start, path[0]);
        Assert.AreEqual(end, path[path.Length - 1]);

        path.Dispose();
        astar.Dispose();
        map.Dispose();
    }

    static bool AreEqual<T>(NativeList<T> a, NativeList<T> b) 
        where T : unmanaged, IEquatable<T>
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; ++i)
            if (!a[i].Equals(b[i]))
                return false;

        return true;
    }

    void RunJob(AStar<int> astar, TestMapInt map, int start, int end, NativeList<int> path)
    {
        new FindPathJob
        {
            AStar = astar,
            Map = map,
            Start = start,
            End = end,
            Path = path
        }.Run();
    }

    [BurstCompile]
    struct FindPathJob : IJob
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