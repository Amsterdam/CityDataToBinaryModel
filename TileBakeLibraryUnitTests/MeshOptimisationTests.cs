using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TileBakeLibrary;
using TileBakeLibrary.Coordinates;

namespace TileBakeLibraryUnitTests
{
	[TestClass]
	public class MeshOptimisationTests
	{
		[TestMethod]
		public void RemoveDoubleVertices()
		{
			VertexNormalCombination.normalAngleComparisonThreshold = 5.0f;

			var vertices = new List<Vector3Double>();
			vertices.Add(new Vector3Double(0, 0, 0));
			vertices.Add(new Vector3Double(0, 1, 0));
			vertices.Add(new Vector3Double(1, 1, 0));

			vertices.Add(new Vector3Double(0, 0, 0)); //< - Double vertex position
			vertices.Add(new Vector3Double(0, 2, 0));
			vertices.Add(new Vector3Double(2, 2, 0));

			vertices.Add(new Vector3Double(0, 0, 0)); //< - Double vertex position
			vertices.Add(new Vector3Double(0, 3, 0));
			vertices.Add(new Vector3Double(3, 3, 0));

			vertices.Add(new Vector3Double(0, 0, 0)); //< - Double vertex position
			vertices.Add(new Vector3Double(0, 4, 0));
			vertices.Add(new Vector3Double(4, 4, 0));

			var normals = new List<Vector3>();
			normals.Add(new Vector3(0, 0, 1));
			normals.Add(new Vector3(0, 1, 0)); 
			normals.Add(new Vector3(1, 0, 0));

			normals.Add(new Vector3(0, 0, 1)); // Equal normal
			normals.Add(new Vector3(0, 1, 0));
			normals.Add(new Vector3(1, 0, 0));

			normals.Add(new Vector3(0, 0, 1.0005f)); // Equal enough normal
			normals.Add(new Vector3(0, 1, 0));
			normals.Add(new Vector3(1, 0, 0));

			normals.Add(new Vector3(0, 0, 1.0005f)); // Equal enough normal
			normals.Add(new Vector3(0, 1, 0));
			normals.Add(new Vector3(1, 0, 0));

			var indices = Enumerable.Range(0, vertices.Count).ToList();

			var subObject = new SubObject();
			subObject.vertices = vertices;
			subObject.normals = normals;
			subObject.triangleIndices = indices;

			var vertexLengthBeforeMerge = subObject.vertices.Count;
			Console.WriteLine($"Before: {vertexLengthBeforeMerge}");

			subObject.MergeSimilarVertices();

			var vertexLengthAfterMerge = subObject.vertices.Count;
			Console.WriteLine($"After: {vertexLengthAfterMerge}");

			Assert.AreEqual(vertexLengthBeforeMerge, vertexLengthAfterMerge+3, $"3 Vertices should have been merged");
		}

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
		public void AngleBetweenNormalsOtherWay()
		{
			var normalA = new Vector3(0, 1, 0); //Up
			var normalB = new Vector3(10, 0, 0); //Right

			var angle = (int)Math.Round((VertexNormalCombination.AngleBetweenNormals(normalB, normalA)));
			Assert.AreEqual(angle, 90, $"Angle should be 90 between normals.");
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
