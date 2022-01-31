using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TileBakeLibrary.ExtentionMethods
{
	public static class Vector3Extentions
	{
		/// <summary>
		/// Calculate angle between normals
		/// </summary>
		/// <param name="a">This normal (normalized)</param>
		/// <param name="b">Other normal (normalized)</param>
		/// <returns></returns>
		public static double AngleBetween(this Vector3 a, Vector3 b)
		{
			return 2.0d * Math.Atan((a - b).Length() / (a + b).Length());
		}
	}
}
