using System;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace Sark.Pathfinding
{
	public interface IPathingMap<T> where T : unmanaged, IEquatable<T>
	{
		FixedList64<T> GetAvailableExits(T pos);
		int GetCost(T a, T b);
		float GetDistance(T a, T b);
    }
}
