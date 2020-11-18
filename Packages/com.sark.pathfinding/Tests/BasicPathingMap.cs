using Sark.Common.GridUtil;
using Sark.Pathfinding;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Sark.Pathfinding.Tests
{
    public struct BasicPathingMap : IPathingMap<int>, IDisposable
    {
        int2 _size;
        NativeArray<int> _points;

        public int2 Size => _size;


        public int Length => _points.Length;

        public BasicPathingMap(int w, int h, Allocator allocator)
        {
            _size = new int2(w, h);
            _points = new NativeArray<int>(w * h, allocator);
        }

        public void Clear()
        {
            for (int i = 0; i < _points.Length; ++i)
                _points[i] = 0;
        }

        public bool IsObstacle(int2 p)
        {
            int i = PosToIndex(p);
            return _points[i] != 0;
        }

        public void SetIsObstacle(int x, int y, bool b) =>
            SetIsObstacle(new int2(x, y), b);

        public void SetIsObstacle(int2 p, bool b)
        {
            int i = PosToIndex(p);
            _points[i] = b == false ? 0 : 1;
        }

        public bool IsInBounds(int2 p) =>
            Grid2D.InBounds(p, _size);

        int PosToIndex(int2 p) => Grid2D.PosToIndex(p, _size.x);
        int2 IndexToPos(int i) => Grid2D.IndexToPos(i, _size.x);

        public void GetAvailableExits(int posIndex, NativeList<int> output)
        {
            int2 p = IndexToPos(posIndex);
            foreach (var d in Grid2D.Directions4Way)
            {
                int2 adj = p + d;
                if (!Grid2D.InBounds(adj, _size))
                    continue;

                if (!IsObstacle(adj))
                    output.Add(PosToIndex(adj));
            }
        }

        public int GetCost(int a, int b)
        {
            return 1;
        }

        public float GetDistance(int a, int b)
        {
            int2 pa = Grid2D.IndexToPos(a, _size.x);
            int2 pb = Grid2D.IndexToPos(b, _size.x);
            return Grid2D.TaxicabDistance(pa, pb);
        }

        public void Dispose()
        {
            _points.Dispose();
        }
    } 
}
