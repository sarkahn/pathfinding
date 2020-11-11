using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using MeshData = UnityEngine.Mesh.MeshData;
using MeshDataArray = UnityEngine.Mesh.MeshDataArray;

using static Sark.Common.GridUtil.Grid2D;
using System.Runtime.InteropServices;
using Unity.Burst;

namespace Sark.Pathfinding.Samples
{
    public class JobMeshDataBackend
    {
        Mesh _mesh;

        JobHandle _tileDataJob;
        JobHandle _vertsJob;
        MeshDataArray _dataArray;

        bool _buildingMesh;
        //bool _sizeChanged;

        public int2 Size { get; private set; }

        int TotalTiles => Size.x * Size.y;

        public JobMeshDataBackend(int w, int h, Mesh mesh)
        {
            //Debug.Log($"Initializing backend with size of {w}, {h}");
            _mesh = mesh;
            Size = new int2(w, h);
            //_sizeChanged = true;
        }

        public void Dispose()
        {
            _tileDataJob.Complete();
        }

        public void Resize(int width, int height)
        {
            Size = new int2(width, height);
            //_sizeChanged = true;
        }

        MeshDataArray GetDataArray()
        {
            var dataArray = Mesh.AllocateWritableMeshData(1);

            dataArray[0].SetVertexBufferParams(TotalTiles * 4,
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal,
                VertexAttributeFormat.Float32, 3, 1),
            // UVs
            new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0,
                VertexAttributeFormat.Float32,
                2, 2),
            // FGColor
            new VertexAttributeDescriptor(
                VertexAttribute.TexCoord1,
                VertexAttributeFormat.Float32,
                4, 2),
            // BGColor
            new VertexAttributeDescriptor(
                VertexAttribute.TexCoord2,
                VertexAttributeFormat.Float32,
                4, 2));

            dataArray[0].SetIndexBufferParams(Size.x * Size.y * 6, IndexFormat.UInt16);

            return dataArray;
        }

        public JobHandle ScheduleUploadTileData(NativeArray<Tile> tiles, JobHandle inputDeps = default)
        {
            _vertsJob.Complete();
            _tileDataJob.Complete();

            _dataArray = GetDataArray();

            //if(_sizeChanged)
            {
                _vertsJob = new VertsJob
                {
                    MeshData = _dataArray[0],
                    Size = Size
                }.ScheduleBatch(tiles.Length, 64, inputDeps);
            }

            _tileDataJob = new TileDataJob
            {
                MeshData = _dataArray[0],
                Size = Size,
                Tiles = tiles
            }.ScheduleBatch(tiles.Length, 64, inputDeps);
            //}.ScheduleBatch(tiles.Length, 64, _vertsJob);

            _buildingMesh = true;

            return JobHandle.CombineDependencies(_vertsJob, _tileDataJob);
        }

        public void Update()
        {
            if (_buildingMesh)
            {
                _buildingMesh = false;
                _vertsJob.Complete();
                _tileDataJob.Complete();





                //if (_sizeChanged)
                {
                    var meshData = _dataArray[0];
                    meshData.subMeshCount = 1;
                    meshData.SetSubMesh(0, new SubMeshDescriptor(0,
                        meshData.GetIndexData<ushort>().Length));
                }

                Mesh.ApplyAndDisposeWritableMeshData(_dataArray, _mesh,
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontValidateIndices);

                //if(_sizeChanged)
                {
                    //_sizeChanged = false;
                    _mesh.RecalculateBounds();
                    _mesh.RecalculateNormals();
                    _mesh.RecalculateTangents();
                }
            }
        }

        public void UploadTileData(NativeArray<Tile> tiles)
        {
            ScheduleUploadTileData(tiles);
            Update();
        }

        static readonly float3 VertRight = new float3(1, 0, 0);
        static readonly float3 VertUp = new float3(0, 1, 0);

        static readonly float2 UvSize = 1f / 16f;
        static readonly float2 UvRight = new float2(UvSize.x, 0);
        static readonly float2 UvUp = new float2(0, UvSize.y);

        static float4 FromColor(Color c) =>
            new float4(c.r, c.g, c.b, c.a);

        [StructLayout(LayoutKind.Sequential)]
        struct VertTileData
        {
            public float2 UV;
            public float4 FGColor;
            public float4 BGColor;
        }

        [BurstCompile]
        public struct VertsJob : IJobParallelForBatch
        {
            public MeshData MeshData;
            public int2 Size;

            public void Execute(int startIndex, int count)
            {
                var pos = MeshData.GetVertexData<float3>(0);
                var idx = MeshData.GetIndexData<ushort>();

                float3 start = -new float3(Size.x, Size.y, 0) * .5f;

                //Debug.Log($"Settings verts from {startIndex} to {startIndex + count}");

                for (int tileIndex = startIndex; tileIndex < startIndex + count; ++tileIndex)
                {
                    int vi = tileIndex * 4; // Vert Index
                    int ti = tileIndex * 6; // Triangle index

                    // Indices
                    idx[ti + 0] = (ushort)(vi + 0);
                    idx[ti + 1] = (ushort)(vi + 1);
                    idx[ti + 2] = (ushort)(vi + 2);
                    idx[ti + 3] = (ushort)(vi + 3);
                    idx[ti + 4] = (ushort)(vi + 2);
                    idx[ti + 5] = (ushort)(vi + 1);

                    // Positions
                    int2 xy = IndexToPos(tileIndex, Size.x);
                    float3 vOrigin = new float3(xy, 0);

                    pos[vi + 0] = start + vOrigin + VertUp;
                    pos[vi + 1] = start + vOrigin + VertRight + VertUp;
                    pos[vi + 2] = start + vOrigin;
                    pos[vi + 3] = start + vOrigin + VertRight;
                }
            }
        }

        [BurstCompile]
        public struct TileDataJob : IJobParallelForBatch
        {
            public MeshData MeshData;
            public int2 Size;

            [ReadOnly]
            public NativeArray<Tile> Tiles;

            public void Execute(int startIndex, int count)
            {
                var tileData = MeshData.GetVertexData<VertTileData>(2);

                //0-1
                //|/|
                //2-3
                for (int tileIndex = startIndex; tileIndex < startIndex + count; ++tileIndex)
                {
                    var tile = Tiles[tileIndex];

                    int vi = tileIndex * 4; // Vert Index

                    int glyph = tile.glyph;

                    // UVs
                    int2 glyphIndex = new int2(
                        glyph % 16,
                        // Y is flipped on the spritesheet
                        16 - 1 - (glyph / 16));
                    float2 uvOrigin = (float2)glyphIndex * UvSize;

                    var fg = tile.fgColor;
                    var bg = tile.bgColor;

                    tileData[vi + 0] = new VertTileData
                    {
                        UV = uvOrigin + UvUp,
                        FGColor = FromColor(fg),
                        BGColor = FromColor(bg)
                    };
                    tileData[vi + 1] = new VertTileData
                    {
                        UV = uvOrigin + UvRight + UvUp,
                        FGColor = FromColor(fg),
                        BGColor = FromColor(bg)
                    };
                    tileData[vi + 2] = new VertTileData
                    {
                        UV = uvOrigin,
                        FGColor = FromColor(fg),
                        BGColor = FromColor(bg)
                    };
                    tileData[vi + 3] = new VertTileData
                    {
                        UV = uvOrigin + UvRight,
                        FGColor = FromColor(fg),
                        BGColor = FromColor(bg)
                    };
                }
            }
        }
    }
}