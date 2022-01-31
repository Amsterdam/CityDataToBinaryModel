/*
*  Copyright (C) X Gemeente
*              	 X Amsterdam
*				 X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://joinup.ec.europa.eu/software/page/eupl
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/
using System;
using System.Numerics;
using TileBakeLibrary.Coordinates;

namespace TileBakeLibrary
{
	public struct VertexNormalCombination : IEquatable<VertexNormalCombination>
	{
		/// <summary>
		/// Angle in degrees
		/// </summary>
		public static float vertexDistanceComparisonThreshold = 0.1f; //1mm
		public static float normalAngleComparisonThreshold = 5.0f;

		public Vector3 normal;
		public Vector3Double vertex;
		public VertexNormalCombination(Vector3Double vertex, Vector3 normal)
		{
			this.vertex = vertex;
			this.normal = normal;
		}

		public bool Equals(VertexNormalCombination other)
        {
			if (Vector3Double.Distance(other.vertex,vertex) < vertexDistanceComparisonThreshold)
            {
				if (AngleBetweenNormals(normal, other.normal) < normalAngleComparisonThreshold)
				{
					return true;
				}
            }
			return false;
        }

		/// <summary>
		/// Normal in degrees
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="radiansInsteadOfDegrees">Return the angle in radians</param>
		/// <returns></returns>
		public static double AngleBetweenNormals(Vector3 a, Vector3 b, bool radiansInsteadOfDegrees = false)
		{
			var normalA = Vector3.Normalize(a);
			var normalB = Vector3.Normalize(b);

			var radians =  2.0d * Math.Atan((normalA - normalB).Length() / (normalA + normalB).Length());
			if(radiansInsteadOfDegrees)
			{
				return radians;
			}

			return (180.0f / Math.PI) * radians;
		}

		public override string ToString()
		{
			return $"v({vertex.X},{vertex.X},{vertex.X}), n({normal.X},{normal.Y},{normal.Z})";
		}
	}
}
