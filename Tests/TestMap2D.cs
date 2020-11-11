using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Sark.Common.GridUtil;

namespace Sark.Pathfinding.Tests
{
    public struct TestMapInt : IPathingMap<int>, INativeDisposable
    {
        NativeArray<byte> _map;
        int2 _size;

        public int Length => _map.Length;

        public NativeArray<byte> GetData() => _map;

        public TestMapInt(int w, int h, Allocator allocator)
        {
            _map = new NativeArray<byte>(w * h, allocator);
            _size = new int2(w, h);
        }

        public TestMapInt(int width, int height, string mapString, Allocator allocator) :
            this(width, height, allocator)
        {
            ParseString(width, height, mapString, _map);
        }

        void ParseString(int w, int h, string str, NativeArray<byte> map)
        {
            str = str.Replace("\n", string.Empty);
            str = str.Replace("\r\n", string.Empty);

            for (int charIndex = 0; charIndex < str.Length; ++charIndex)
            {
                char c = str[charIndex];

                // String has y reversed. Convert to map space
                int2 stringPos = IndexToPos(charIndex);
                stringPos.y = h - 1 - stringPos.y;
                int mapIndex = PosToIndex(stringPos.x, stringPos.y);

                if (c == '#' || c == '*')
                    map[mapIndex] = 1;
                else
                    map[mapIndex] = 0;
            }
        }

        public void SetTile(int x, int y, byte value)
        {
            int i = PosToIndex(x, y);
            _map[i] = value;
        }

        public int GetTile(int x, int y)
        {
            int i = PosToIndex(x, y);
            return _map[i];
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            _map.Dispose(inputDeps);
            return inputDeps;
        }

        public void Dispose()
        {
            _map.Dispose();
        }

        public FixedList64<int> GetAvailableExits(int pIndex)
        {
            int2 p = IndexToPos(pIndex);
            var neighbours = new FixedList64<int>();

            for (int i = 0; i < Grid2D.Directions4Way.Length; ++i)
            {
                int2 dir = Grid2D.Directions4Way[i];
                int2 adjPos = dir + p;

                if (!IsInBounds(adjPos) || !IsPathable(adjPos))
                    continue;

                neighbours.Add(PosToIndex(adjPos.x, adjPos.y));
            }

            return neighbours;
        }

        public bool IsPathable(int2 p) =>
            IsPathable(PosToIndex(p));

        public bool IsPathable(int p)
        {
            return _map[p] == 0;
        }

        public int GetCost(int posIndexA, int posIndexB)
        {
            return 1;
        }

        public float GetDistance(int aIndex, int bIndex)
        {
            var a = IndexToPos(aIndex);
            var b = IndexToPos(bIndex);
            return Grid2D.TaxicabDistance(a, b);
        }

        public bool IsInBounds(int2 p) => Grid2D.InBounds(p, _size);
        public int PosToIndex(int2 p) => PosToIndex(p.x, p.y);
        public int PosToIndex(int x, int z) => Grid2D.PosToIndex(x, z, _size.x);
        public int2 IndexToPos(int i) => Grid2D.IndexToPos(i, _size.x);

    }

    public struct TestMapInt2 : IPathingMap<int2>, INativeDisposable
    {
        NativeArray<byte> _map;
        int2 _size;

        public int Length => _map.Length;

        public NativeArray<byte> GetData() => _map;

        public TestMapInt2(int2 size, Allocator allocator)
        {
            _map = new NativeArray<byte>(size.x * size.y, allocator);
            _size = size;
        }

        public TestMapInt2(int width, int height, string mapString, Allocator allocator)
        {
            _size = new int2(width, height);
            _map = new NativeArray<byte>(width * height, allocator);
            ParseString(width, height, mapString, _map);
        }

        void ParseString(int w, int h, string str, NativeArray<byte> map)
        {
            str = str.Replace("\n", string.Empty);
            str = str.Replace("\r\n", string.Empty);

            for (int charIndex = 0; charIndex < str.Length; ++charIndex)
            {
                char c = str[charIndex];

                // String has y reversed. Convert to map space
                int2 stringPos = IndexToPos(charIndex);
                stringPos.y = h - 1 - stringPos.y;
                int mapIndex = PosToIndex(stringPos.x, stringPos.y);

                if (c == '#' || c == '*')
                    map[mapIndex] = 1;
                else
                    map[mapIndex] = 0;
            }
        }

        public void SetTile(int x, int y, byte value)
        {
            int i = PosToIndex(x, y);
            _map[i] = value;
        }

        public int GetTile(int x, int y)
        {
            int i = PosToIndex(x, y);
            return _map[i];
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            _map.Dispose(inputDeps);
            return inputDeps;
        }

        public void Dispose()
        {
            _map.Dispose();
        }

        public FixedList64<int2> GetAvailableExits(int2 p)
        {
            FixedList64<int2> neighbours = new FixedList64<int2>();
            for (int i = 0; i < Grid2D.Directions4Way.Length; ++i)
            {
                int2 dir = Grid2D.Directions4Way[i];
                int2 adjPos = dir + p;

                if (!IsInBounds(adjPos) || !IsPathable(adjPos))
                    continue;

                neighbours.Add(adjPos);
            }

            return neighbours;
        }

        public bool IsPathable(int2 pos)
        {
            int i = PosToIndex(pos.x, pos.y);
            return _map[i] == 0;
        }

        public int GetCost(int2 posIndexA, int2 posIndexB)
        {
            return 1;
        }

        public float GetDistance(int2 a, int2 b)
        {
            return Grid2D.TaxicabDistance(a, b);
        }

        public bool IsInBounds(int2 p) => Grid2D.InBounds(p, _size);
        public int PosToIndex(int x, int z) => Grid2D.PosToIndex(x, z, _size.x);
        public int2 IndexToPos(int i) => Grid2D.IndexToPos(i, _size.x);

    }
}