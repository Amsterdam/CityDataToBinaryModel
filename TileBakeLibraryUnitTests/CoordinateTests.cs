using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;
using TileBakeLibrary;
using TileBakeLibrary.Coordinates;

namespace TileBakeLibraryUnitTests
{
	[TestClass]
	public class CoordinateTests
	{
		[TestMethod]
		public void VerticesSharingLocationAreEqual()
		{
			var vertexA = new Vector3Double(1, 1, 1);
			var vertexB = new Vector3Double(1, 1, 1);

			Assert.AreEqual(vertexA, vertexB, $"Vertices sharing the same position should be the same.");
		}

		[TestMethod]
		public void AngleBetweenNormals()
		{
			var normalA = new Vector3(0, 1, 0); //Up
			var normalB = new Vector3(1, 1, 0); //Middle of up and right

			var angle = (int)Math.Round((VertexNormalCombination.AngleBetweenNormals(normalA, normalB)));
			Assert.AreEqual(angle, 45, $"Angle should be 45 between normals.");
		}

		[TestMethod]
		public void TestVertexMerging()
		{
			VertexNormalCombination.normalAngleComparisonThreshold = 5.0f;

			VertexNormalCombination vertAndNormalA = new VertexNormalCombination()
			{
				vertex = new Vector3Double(10,10,10),
				normal = new Vector3(1,1,1)
			};
			VertexNormalCombination vertAndNormalB = new VertexNormalCombination()
			{
				vertex = new Vector3Double(10, 10, 10),
				normal = new Vector3(1, 1, 1.15f)
			};

			var angle = VertexNormalCombination.AngleBetweenNormals(vertAndNormalA.normal, vertAndNormalB.normal);
			Console.WriteLine($"Angle: {angle}");

			var angleEqual = vertAndNormalA.Equals(vertAndNormalB);
			Console.WriteLine($"Angles similar enough: {angleEqual}");

			Assert.AreEqual(true, angleEqual, $"Vertices should be considered equal.");
		}
	}
}
