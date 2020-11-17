using System;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace Sark.Pathfinding
{
	public interface IPathingMap<T> where T : unmanaged, IEquatable<T>
	{
		void GetAvailableExits(T pos, NativeList<T> output);
		int GetCost(T a, T b);
		float GetDistance(T a, T b);
    }
}
