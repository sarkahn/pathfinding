
using Unity.Collections;
using UnityEngine;

using Sark.Pathfinding;
using UnityEngine.Profiling;
using Sark.PathfindingRuntime;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

public class SpamPathfindingProfilerInt2 : MonoBehaviour
{
    TestMapInt2 _map;
    AStar<int2> _astar;
    NativeList<int2> _path;

    int _start;
    int _end;

    [SerializeField]
    int2 _mapSize = new int2(30, 30);

    private void OnEnable()
    {
        var (map, start, end) = GetMapWithObstacles(_mapSize.x, _mapSize.y, Allocator.Persistent);
        _astar = new AStar<int2>(map.Length, Allocator.Persistent);
        _path = new NativeList<int2>(map.Length, Allocator.Persistent);
        _map = map;
        _start = start;
        _end = end;
    }

    private void OnDisable()
    {
        _map.Dispose();
        _astar.Dispose();
        _path.Clear();
    }

    private void Update()
    {
        _path.Clear();
        _astar.Clear();

        FindPathBurst();
    }

    public void FindPathDirect()
    {
        _astar.FindPath(_map, _start, _end, _path);
    }

    public void FindPathBurst()
    {
        new PathJob 
        { 
            AStar = _astar, 
            Path = _path, 
            Map = _map, 
            Start = _start, 
            End = _end 
        }.Run();
    }

    [BurstCompile]
    public struct PathJob : IJob
    {
        public AStar<int2> AStar;
        public TestMapInt2 Map;
        public NativeList<int2> Path;
        public int Start;
        public int End;

        public void Execute()
        {
            AStar.FindPath(Map, Start, End, Path);
        }
    }

    public static (TestMapInt2, int, int) GetMapWithObstacles(int w, int h, Allocator allocator)
    {
        var map = new TestMapInt2(new int2(w,h), allocator);

        int x = w / 2;
        for (int y = 0; y < h - 2; ++y)
            map.SetTile(x, y, 1);


        int start = map.PosToIndex(0, 0);
        int end = map.PosToIndex(w - 1, 0);
        return (map, start, end);
    }
}
