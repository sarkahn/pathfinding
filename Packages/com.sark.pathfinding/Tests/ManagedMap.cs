using Sark.Common.GridUtil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Sark.Pathfinding.Tests
{
    public class ManagedMap
    {
        byte[] _map;
        int2 _size;

        public int Length => _map.Length;

        public byte[] GetData() => _map;

        public ManagedMap(int w, int h)
        {
            _map = new byte[w * h];
            _size = new int2(w, h);
        }

        public ManagedMap(int width, int height, string mapString) :
            this(width, height)
        {
            ParseString(width, height, mapString, _map);
        }

        void ParseString(int w, int h, string str, byte[] map)
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

        public List<int> GetAvailableExits(int pIndex)
        {
            int2 p = IndexToPos(pIndex);
            var neighbours = new List<int>(8);

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
}