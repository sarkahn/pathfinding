using Sark.Pathfinding;
using Sark.Pathfinding.Samples;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class VeryLongPathProfiling : MonoBehaviour
{
    AStar<int> _aStar;
    AStarArrays _aStarArrays;
    NativeList<int> _path;
    TestMapInt _map;

    [SerializeField]
    int2 _size = new int2(1000,1000);

    int _start;
    int _end;

    private void OnEnable()
    {
        var (map,start,end) = TestMapInt.GetMapWithObstacles(
            _size.x, _size.y, Allocator.Persistent);

        _map = map;
        _start = start;
        _end = end;
        _aStar = new AStar<int>(_map.Length, Allocator.Persistent);
        _aStarArrays = new AStarArrays(_map.Length, Allocator.Persistent);
        _path = new NativeList<int>(_map.Length, Allocator.Persistent);
    }

    // Update is called once per frame
    void Update()
    {
        _aStar.Clear();
        _aStarArrays.Clear();

        _path.Clear();
        Profiler.BeginSample("AStar HashMap");
        _aStarArrays.GetJob(_map, _start, _end, _path).Run();
        Profiler.EndSample();

        _path.Clear();
        Profiler.BeginSample("AStar Arrays");
        new PathJob
        {
            AStar = _aStar,
            Map = _map,
            Start = _start,
            End = _end,
            Path = _path
        }.Run();
        Profiler.EndSample();

        //_aStar.FindPath(_map, _start, _end, _path);
    }

    [BurstCompile]
    public struct PathJob : IJob
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
