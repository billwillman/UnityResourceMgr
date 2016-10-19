using System;
using System.Collections.Generic;

namespace Utils
{
	public class StructComparser<T>: IEqualityComparer<T> where T: IEquatable<T>
	{
		public static readonly StructComparser<T> Default = new StructComparser<T>();

		public bool Equals(T x, T y)
		{
			return x.Equals(y);
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
}
