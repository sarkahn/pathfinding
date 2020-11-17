using Sark.Common.GridUtil;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Sark.Pathfinding.Samples.CodePage437;
using Random = Unity.Mathematics.Random;

namespace Sark.Pathfinding.Samples
{

    [RequireComponent(typeof(SimpleTerminal))]
    public class Demo : MonoBehaviour
    {
        TestMapInt _map;
        SimpleTerminal _terminal;
        bool _resizing = false;

        [SerializeField]
        int2 _size = new int2(80, 40);

        [SerializeField]
        CanvasGroup _helpUI;

        int2 ? _start = null;
        int2 ? _end = null;

        bool _dirty = false;

        AStar<int> _aStar;
        NativeList<int> _path;
        NativeList<int> _visited;

        double _pathTime = 0;

        bool _dragging = false;
        bool _dragAdding = false;
        List<int2> _draggedPoints = new List<int2>();

        [SerializeField]
        Color _startColor = Color.blue;

        [SerializeField]
        Color _endColor = Color.green;

        private void Awake()
        {
            _terminal = GetComponent<SimpleTerminal>();
            _aStar = new AStar<int>(_size.x * _size.y, Allocator.Persistent);
            _path = new NativeList<int>(_size.x * _size.y, Allocator.Persistent);
            _visited = new NativeList<int>(_size.x * _size.y, Allocator.Persistent);
        }

        private void Start()
        {
            OnResized(_size.x, _size.y);
        }

        private void OnDisable()
        {
            _map.Dispose();
            _aStar.Dispose();
            _path.Dispose();
            _visited.Dispose();
        }

        public void Update()
        {
            HandleResize();
            HandleNoise();
            HandleSetStartEnd();
            HandlePathFind();
            HandleToggleUI();
            HandleToggleWalls();

            if(_dirty)
            {
                _dirty = false;
                DrawTerminal();
            }

        }

        void HandleToggleWalls()
        {
            if( !_dragging && Input.GetMouseButtonDown(0) )
                OnDragBegin();
            
            if (_dragging && Input.GetMouseButtonUp(0))
                OnDragEnd();

            if(_dragging && Input.GetMouseButton(0))
            {
                var p = MouseToConsolePosition();
                OnDrag(p);
            }
        }

        void OnDragBegin()
        {
            Debug.Log("DragBegin");
            var p = MouseToConsolePosition();
            OnDrag(p);
            _dragging = true;
        }

        void OnDragEnd()
        {
            Debug.Log("DragEnd");
            _dragging = false;
            _draggedPoints.Clear();
        }

        void OnDrag(int2 p)
        {
            if (_draggedPoints.Contains(p))
                return;

            if (!_map.IsInBounds(p))
                return;

            int i = Grid2D.PosToIndex(p, _size.x);

            // Don't touch the path points
            if ((_start != null && _start.Value.Equals(p)) ||
                _end != null && _end.Value.Equals(p))
                return;

            Debug.Log($"OnDrag {p}");
            ClearPath();
            var existing = _map.GetTile(p.x, p.y);

            if (_draggedPoints.Count == 0)
            {
                _dragAdding = existing == 0;
            }

            _draggedPoints.Add(p);

            if(existing == 0)
            {
                if (_dragAdding)
                    _map.SetTile(p.x, p.y, 1);
            }
            if (existing == 1)
                if (!_dragAdding)
                    _map.SetTile(p.x, p.y, 0);

            _dirty = true;
        }

        void HandleResize()
        {
            float hor = Input.GetAxisRaw("Horizontal");
            float ver = Input.GetAxisRaw("Vertical");

            if ((hor != 0 || ver != 0) && !_resizing)
            {
                _resizing = true;
                _dirty = true;
                OnResized(_size.x + (int)hor, _size.y + (int)ver);
            }

            if (hor == 0 && ver == 0)
                _resizing = false;
        }

        void OnResized(int w, int h)
        {
            _size.x = w;
            _size.y = h;

            ClearPath();

            if (_map.IsCreated)
                _map.Dispose();

            _map = new TestMapInt(w, h, Allocator.Persistent);
            _terminal.Resize(w, h);
            _dirty = true;
        }

        public int2 MouseToConsolePosition()
        {
            float3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float3 local = transform.InverseTransformPoint(worldPos);
            local.xy += (float2)_size * .5f;
            local.xy = math.floor(local.xy);

            return (int2)local.xy;
        }

        void HandleToggleUI()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _helpUI.alpha = 1 - _helpUI.alpha;
        }

        void HandleSetStartEnd()
        {
            if(Input.GetMouseButtonDown(1))
            {
                ClearPath();

                int2 p = MouseToConsolePosition();
                if (!Grid2D.InBounds(p, _size))
                    return;

                if(_start == null)
                {
                    _start = p;
                    _dirty = true;
                    return;
                }

                if( _end == null )
                {
                    _end = p;
                    _dirty = true;
                    return;
                }

                ClearStartEnd();

                _start = p;

                _dirty = true;
            }
        }

        void ClearPath()
        {
            _visited.Clear();
            _aStar.Clear();
            _path.Clear();
        }

        void HandlePathFind()
        {
            if( Input.GetKeyDown(KeyCode.Space))
            {
                if(_start == null || _end == null )
                {
                    Debug.Log("Need a start and an end to pathfind.");
                    return;
                }

                int s = Grid2D.PosToIndex(_start.Value, _size.x);
                int e = Grid2D.PosToIndex(_end.Value, _size.x);

                ClearPath();

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                new PathJob 
                { 
                    Astar = _aStar, 
                    Map = _map, 
                    Start = s, 
                    End = e, 
                    Path = _path 
                }.Run();
                sw.Stop();

                _pathTime = sw.Elapsed.TotalMilliseconds;


                var v = _aStar.GetVisited(Allocator.Temp);
                string success = _path.Length == 0 ? 
                    "Couldn't find path." : 
                    $"Path length {_path.Length}.";
                Debug.Log($"Took {_pathTime} ms. {success} Visited {v.Length} nodes");
                _visited.AddRange(v);

                _dirty = true;
            }
        }

        void DrawTerminal()
        {
            _terminal.ClearScreen();
            var data = _map.GetData();
            for (int i = 0; i < data.Length; ++i)
            {
                int2 p = Grid2D.IndexToPos(i, _size.x);

                byte b = data[i];

                if (b != 0)
                    _terminal.Set(p.x, p.y, Color.white, Color.black, ToCP437('#'));
            }

            DrawStartEnd();

            if (_start == null || _end == null)
                return;

            for( int i = 0; i < _visited.Length; ++i )
            {
                var p = Grid2D.IndexToPos(_visited[i], _size.x);

                _terminal.Set(p.x, p.y, Color.red, Color.black, ToCP437('.'));
            }



            for( int i = 1; i < _path.Length; ++i )
            {
                var col = Color.Lerp(_startColor, _endColor, (float)i / (_path.Length - 2));
                var p = Grid2D.IndexToPos(_path[i], _size.x);
                _terminal.Set(p.x, p.y, col, col, ToCP437('█'));
            }

            DrawStartEnd();
        }

        void DrawStartEnd()
        {
            if(_start != null )
                _terminal.Set(_start.Value.x, _start.Value.y,
                    _startColor, Color.black, ToCP437('S'));
            if(_end != null)
                _terminal.Set(_end.Value.x, _end.Value.y,
                    _endColor, Color.black, ToCP437('E'));
        }

        void ClearStartEnd()
        {
            _end = null;
            _start = null;
        }

        const float threshold = 10f;

        void HandleNoise()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                _dirty = true;

                _map.Clear();
                ClearPath();
                ClearStartEnd();

                Random rand = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

                int iters = rand.NextInt(1, 16);
                float per = rand.NextFloat(0.15f, 0.75f);
                float scale = rand.NextFloat(0.01f, .2f);
                int low = rand.NextInt(0, 10);
                int high = rand.NextInt(low + 5, low + 15);
                int thresh = rand.NextInt(low, high);

                //new NoiseJob
                //{
                //    Map = _map,
                //    Size = _size,
                //    Iterations = _iterations,
                //    Persistence = _persistence,
                //    Scale = _scale,
                //    Low = _low,
                //    High = _high
                //}.Run();

                new NoiseJob
                {
                    Map = _map,
                    Size = _size,
                    Iterations = iters,
                    Persistence = per,
                    Scale = scale,
                    Low = low,
                    High = high,
                    Threshold = thresh
                }.Run();
            }
        }

        [BurstCompile]
        struct NoiseJob : IJob
        {
            public TestMapInt Map;
            public int2 Size;
            public int Iterations;
            public float Persistence;
            public float Scale;
            public int Low;
            public int High;
            public int Threshold;

            public void Execute()
            {
                for (int i = 0; i < Map.Length; ++i)
                {
                    int2 p = Grid2D.IndexToPos(i, Size.x);
                    float noise = SumOctave(p.x, p.y,
                        Iterations, Persistence, Scale, Low, High);
                    if(noise >= threshold)
                    {
                        Map.SetTile( p.x, p.y, 1 );
                    }
                }
            }
        }

        [BurstCompile]
        struct PathJob : IJob
        {
            public AStar<int> Astar;
            public TestMapInt Map;
            public NativeList<int> Path;
            public int Start;
            public int End;

            public void Execute()
            {
                Astar.FindPath(Map, Start, End, Path);
            }
        }

        static float SumOctave(int x, int y, int iterations, float persistence, float scale, int low, int high)
        {
            float maxAmp = 0;
            float amp = 1;
            float freq = scale;
            float v = 0;

            for (int i = 0; i < iterations; ++i)
            {
                v += noise.snoise(new float2(x * freq, y * freq)) * amp;
                maxAmp += amp;
                amp *= persistence;
                freq *= 2;
            }

            v /= maxAmp;

            v = v * (high - low) / 2f + (high + low) / 2f;

            return v;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;

            OnResized(_size.x, _size.y);
        }
#endif
    }
}