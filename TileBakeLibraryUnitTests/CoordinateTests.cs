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
		public void AngleBetweenNormals()
		{
			var normalA = new Vector3(0, 1, 0);
			var normalB = new Vector3(1, 1, 0);

			var angle = (int)Math.Round((VertexNormalCombination.AngleBetweenNormals(normalA, normalB)));
			Assert.AreEqual(angle, 45, $"Angle should be 45 between normals: {normalA} -> {normalB}");
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
				normal = new Vector3(1, 1, 0.9998f)
			};

			Assert.AreEqual(vertAndNormalA, vertAndNormalB, $"Vertices should be considered equal: {vertAndNormalA} -> {vertAndNormalB}");
		}
	}
}
