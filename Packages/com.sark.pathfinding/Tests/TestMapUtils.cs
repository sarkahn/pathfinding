
using Sark.Common.GridUtil;
using Unity.Collections;
using Unity.Mathematics;

namespace Sark.Pathfinding.Tests
{
    public static class TestMapUtils
    {
        public static (TestMapInt2, int2, int2) GetInt2MapFromString(
            int w, int h, string str, Allocator allocator)
        {
            var map = new TestMapInt2(w, h, str, allocator);
            int2 start = ConvertToMapPos(str.IndexOf('S'), w, h);
            int2 end = ConvertToMapPos(str.IndexOf('E'), w, h);

            return (map, start, end);
        }
        static int2 ConvertToMapPos(int stringIndex, int w, int h)
        {
            int2 p = Grid2D.IndexToPos(stringIndex, w);
            p.y = h - 1 - p.y;
            return p;
        }

        /// <summary>
        /// Creates a map with a single wall in the middle
        /// </summary>
        public static (TestMapInt, int, int) GetIntMapWithObstacles(int w, int h, Allocator allocator)
        {
            var map = new TestMapInt(w, h, allocator);

            int x = w / 2;
            for (int y = 0; y < h - 2; ++y)
                map.SetTile(x, y, 1);


            int start = map.PosToIndex(0, 0);
            int end = map.PosToIndex(w - 1, 0);
            return (map, start, end);
        }

        /// <summary>
        /// Creates a map with a single wall in the middle
        /// </summary>
        public static (TestMapInt2, int, int) GetInt2MapWithObstacles(int w, int h, Allocator allocator)
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
}