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
    AStar<int2> _aStar;
    NativeList<int2> _path;
    //AStarArrays _aStarArrays;
    TestMapInt2 _map;

    [SerializeField]
    int2 _size = new int2(1000,1000);

    int2 _start;
    int2 _end;

    private void OnEnable()
    {
        var (map,start,end) = TestMapInt2.GetMapWithObstacles(
            _size.x, _size.y, Allocator.Persistent);

        _map = map;
        _start = start;
        _end = end;
        _aStar = new AStar<int2>(_map.Length, Allocator.Persistent);
        //_aStarArrays = new AStarArrays(_map.Length, Allocator.Persistent);
        _path = new NativeList<int2>(_map.Length, Allocator.Persistent);
    }

    // Update is called once per frame
    void Update()
    {
        _aStar.Clear();
        //_aStarArrays.Clear();

        //_path.Clear();
        //Profiler.BeginSample("AStar HashMap");
        //_aStarArrays.GetJob(_map, _start, _end, _path).Run();
        //Profiler.EndSample();

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
