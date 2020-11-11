using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Burst;
using Sark.Common.GridUtil;
using UnityEngine.Experimental.Rendering.Universal;

namespace Sark.Pathfinding.Samples
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SimpleTerminal : MonoBehaviour
    {
        [SerializeField]
        int2 _size = new int2(40,20);

        [SerializeField]
        PixelPerfectCamera _camera;

        protected NativeArray<Tile> _tiles;

        public int Width => _size.x;
        public int Height => _size.y;
        public int CellCount => _size.x * _size.y;

        protected JobHandle _tileJobs;

        protected JobMeshDataBackend _backend = null;

        protected bool _isDirty;

        Mesh _mesh;

        private void Awake()
        {
            _mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private void OnDisable()
        {
            if (_tiles.IsCreated)
                _tiles.Dispose(_tileJobs);
        }

        public int PosToIndex(int x, int y) => Grid2D.PosToIndex(x, y, Width);

        public void ClearScreen()
        {
            _isDirty = true;

            _tileJobs.Complete();

            new ClearTiles
            {
                Tiles = _tiles
            }.Run();
        }

        void Update()
        {
            if (_isDirty)
            {
                _isDirty = false;
                _tileJobs = _backend.ScheduleUploadTileData(_tiles, _tileJobs);
            }
        }

        private void LateUpdate()
        {
            _backend.Update();
        }

        public void Resize(int w, int h)
        {
            w = math.max(1, w);
            h = math.max(1, h);
            //if (_mesh != null && w == Width && h == Height)
            //    return;

            OnResized(w, h);
        }

        void OnResized(int w, int h)
        {
            _tileJobs.Complete();

            _size = new int2(w, h);

            if (_tiles.IsCreated)
                _tiles.Dispose();

            _tiles = new NativeArray<Tile>(CellCount, Allocator.Persistent);

            ClearScreen();

            if (_backend == null)
                _backend = new JobMeshDataBackend(Width, Height, _mesh);
            else
                _backend.Resize(Width, Height);

            // Size * pixels per unit
            int2 pixels = _size * 8;
            _camera.refResolutionX = pixels.x;
            _camera.refResolutionY = pixels.y;

            _isDirty = true;
        }


        public void Set(int x, int y, Color fgColor, Color bgColor, byte glyph)
        {
            _isDirty = true;
            int i = PosToIndex(x, y);
            _tiles[i] = new Tile
            {
                fgColor = fgColor,
                bgColor = bgColor,
                glyph = glyph
            };
        }

        public void Dispose()
        {
            _tileJobs.Complete();

            _backend?.Dispose();

            if (_tiles.IsCreated)
                _tiles.Dispose();
        }


        [BurstCompile]
        struct ClearTiles : IJob
        {
            public NativeArray<Tile> Tiles;
            public void Execute()
            {
                for (int i = 0; i < Tiles.Length; ++i)
                    Tiles[i] = Tile.EmptyTile;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _size = math.max(1, _size);

            if (!isActiveAndEnabled || !Application.isPlaying)
                return;

            if(!_size.Equals(_backend.Size))
            {
                OnResized(_size.x, _size.y);
            }
        }
#endif
    } 
}
