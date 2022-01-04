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
using System.Collections.Generic;
using System.Numerics;
using TileBakeLibrary.Coordinates;
using g3;
using gs;
using System.Linq;

namespace TileBakeLibrary
{
	public class SubObject
	{
		public List<Vector3Double> vertices = new List<Vector3Double>(); 
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector2> uvs = new List<Vector2>();
		public List<int> triangleIndices = new List<int>();
		public Vector2Double centroid = new Vector2Double();
		public int parentSubmeshIndex = 0;
		public string id = "";

		private double distanceMergeThreshold = 0.01;
		private DMesh3 mesh;
		public float maxVerticesPerSquareMeter;
		public void MergeSimilarVertices(float mergeVerticesBelowNormalAngle)
		{
			List<Vector3Double> cleanedVertices = new List<Vector3Double>();
			List<Vector3> cleanedNormals = new List<Vector3>();
			List<Vector2> cleanedUvs = new List<Vector2>();
			
			Vector3Double vertex;
			Vector3 normal;
			int oldIndex =0;
			int newIndex = 0;
			Dictionary< vertexNormalCombination,int> verts = new Dictionary<vertexNormalCombination,int>();
			Dictionary<int, int> indexmap = new Dictionary<int, int>(); // old index --> new index
            for (int i = 0; i < triangleIndices.Count; i++)
            {
				oldIndex = triangleIndices[i];
				vertex = vertices[oldIndex];
				normal = normals[oldIndex];
				vertexNormalCombination vnc = new vertexNormalCombination(vertex, normal);
                if (!verts.ContainsKey(vnc))
                {
					newIndex = cleanedVertices.Count();
					cleanedNormals.Add(normal);
					cleanedVertices.Add(vertex);
					verts.Add(vnc, newIndex);
					indexmap.Add(i, newIndex);
                }
                else
                {
					newIndex = verts[vnc];
					indexmap.Add(i, newIndex);
				}
				triangleIndices[i] = newIndex;
            }
			vertices = cleanedVertices;
			normals = cleanedNormals;


        }

		private int GetOrAddVertexIndex(int vertexIndex, List<Vector3Double> cleanedVertices, List<Vector3> cleanedNormals, List<Vector2> cleanedUvs, float angleThreshold)
		{
			bool hasnormals = true;
            if (cleanedNormals.Count==0)
            {
				hasnormals = false;
            }
			Vector3Double inputVertex = vertices[vertexIndex];
			Vector3 inputNormal = normals[vertexIndex];
			
			//Vector2 inputUv = uvs[index]; //When we support uv's, a vertex with a unique UV should not be merged and be added as a unique one

			//Find vertex on a similar threshold position, and then normal
			for (int i = 0; i < cleanedVertices.Count; i++)
			{
				var cleanedVertex = cleanedVertices[i];
				var distance = Vector3Double.Distance(inputVertex, cleanedVertex);
				if(distance < distanceMergeThreshold)
				{
                    if (!hasnormals)
                    {
						return i;
                    }
					//Compare the normal using a threshold
					var cleanedVertNormal = cleanedNormals[i];
					if (Vector3.Dot(inputNormal, cleanedVertNormal) >= angleThreshold)
					{
						//Similar enough normal reuse existing vert
						return i;
					}
				}
			}

			cleanedVertices.Add(inputVertex);
			cleanedNormals.Add(inputNormal);
			return cleanedVertices.Count - 1;
		}


		private void createMesh()
        {
			mesh = new DMesh3(false, false, false, false);

			
			for (int i = 0; i < vertices.Count; i++)
			{
				mesh.AppendVertex(new Vector3d(vertices[i].X, vertices[i].Y, vertices[i].Z));

			}
			for (int i = 0; i < triangleIndices.Count; i += 3)
			{
				mesh.AppendTriangle(triangleIndices[i], triangleIndices[i + 1], triangleIndices[i + 2]);
			}
			MeshNormals.QuickCompute(mesh);
		}

		private void saveMesh()
        {
			vertices.Clear();
			WriteMesh outputMesh = new WriteMesh(mesh);
			int vertCount = outputMesh.Mesh.VertexCount;
			Vector3d vector;
			Vector3d normal;
			int[] mapV = new int[mesh.MaxVertexID];
			int nAccumCountV = 0;
			foreach (int vi in mesh.VertexIndices())
			{
				mapV[vi] = nAccumCountV++;
				Vector3d v = mesh.GetVertex(vi);
				vertices.Add(new Vector3Double(v.x, v.y, v.z));
				normal = mesh.GetVertexNormal(vi);
				normals.Add(new Vector3((float)normal.x, (float)normal.y, (float)normal.z));
			}

			triangleIndices.Clear();
			foreach (int ti in mesh.TriangleIndices())
			{
				Index3i t = mesh.GetTriangle(ti);
				triangleIndices.Add(mapV[t[0]]);
				triangleIndices.Add(mapV[t[1]]);
				triangleIndices.Add(mapV[t[2]]);
			}
			mesh = null;
			MergeSimilarVertices(2);
		}

		public void SimplifyMesh()
        {
            if (mesh == null)
            {
				createMesh();
            }

			//MergeSimilarVertices(50);
			
			MeshNormals.QuickCompute(mesh);
            MergeCoincidentEdges merg = new MergeCoincidentEdges(mesh);
            merg.Apply();
            //if (!mesh.CheckValidity(true, FailMode.ReturnOnly))
            //{
            //    return;
            //}

            // setup up the reducer
            Reducer r = new Reducer(mesh);
            // set reducer to preserve bounds

            r.SetExternalConstraints(new MeshConstraints());
            MeshConstraintUtil.FixAllBoundaryEdges(r.Constraints, mesh);


			int edgecount = mesh.BoundaryEdgeIndices().Count(p=>p>-1);
			double area = MeshMeasurements.AreaT(mesh, mesh.TriangleIndices());
			int squareMetersPerVertex = 1000;
            int maxSurfaceCount = (int)(area* maxVerticesPerSquareMeter) +edgecount;
            if (mesh.VertexCount > maxSurfaceCount)
            {
                r.ReduceToVertexCount(maxSurfaceCount);

            }

           mesh = r.Mesh;

			saveMesh();


		}

		public void ClipSpikes(float ceiling, float floor)
        {
			List<Vector3Double> correctverts = vertices.Where(o => o.Z < ceiling && o.Z > floor).ToList();
            if (correctverts.Count == vertices.Count)
            {
				// no spikes detected
				return;
			}


			double averageHeight = (ceiling + floor) / 2;
			
            if (correctverts.Count>0)
            {
				averageHeight = correctverts.Average(o => o.Z);
			}
			double height;
			Vector3Double vertex;
            for (int i = 0; i < vertices.Count; i++)
            {
				height = vertices[i].Z;
                if (height>ceiling || height<floor)
                {
					vertex = vertices[i];
					vertex.Z = averageHeight;
					vertices[i] = vertex;
                }
            }
		}


		public List<SubObject> clipSubobject(Vector2 size)
        {
			List<SubObject> subObjects = new List<SubObject>();
			createMesh();
			var bounds = MeshMeasurements.Bounds(mesh, null);
			// find the coordinates of the tile-borders around the object
			int rdXmin = (int)Math.Floor(bounds.Min.x/size.X)*(int)size.X;
			int rdYmin = (int)Math.Floor(bounds.Min.y / size.Y) * (int)size.Y;
			int rdXmax = (int)Math.Ceiling(bounds.Max.x / size.X) * (int)size.X;
			int rdYmax = (int)Math.Ceiling(bounds.Max.y / size.Y) * (int)size.Y;

			// if the object is contained in 1 tile, no need to clip it.
            if (rdXmax-rdXmin==(int)size.X && rdYmax-rdYmin==(int)size.Y)
            {
				return subObjects;
            }

            for (int x = rdXmin; x < rdXmax; x += (int)size.X)
            {
				DMesh3 columnMesh = CutColumnMesh(x,rdYmin,size.X);
				var localbounds = MeshMeasurements.Bounds(columnMesh, null);
				int localYmin = (int)Math.Floor(localbounds.Min.y / size.Y) * (int)size.Y;
				int localYmax = (int)Math.Ceiling(localbounds.Max.y / size.Y) * (int)size.Y; ;
                if (localYmax-localYmin==(int)size.Y)
                {
					subObjects.Add(createSubobjectFromMesh(columnMesh,x,localYmin,(int)size.Y));
                }
				else
				{ 
                for (int y = localYmin; y < localYmax; y += (int)size.Y)
                {
                    SubObject newSubobject = clipMesh(columnMesh, x, y,size.Y);
                    if (newSubobject != null)
                    {

                        subObjects.Add(newSubobject);
                    }
                }
				}
			}


            return subObjects;
		}

		private SubObject createSubobjectFromMesh(DMesh3 mesh,int x,int y,int size)
        {
			//create new subobject
			SubObject subObject = new SubObject();

			subObject.centroid = new Vector2Double(x+(size/2),y+(size/2));
			subObject.id = id;
			subObject.parentSubmeshIndex = parentSubmeshIndex;
			subObject.mesh = mesh;
			subObject.saveMesh();

			return subObject;
		}

		private DMesh3 CutColumnMesh(int X, int Y, float tileSize)
        {
			DMesh3 clippedMesh = new DMesh3(false, false, false, false);
			clippedMesh.Copy(mesh);
			MeshPlaneCut mpc;
			var bounds = MeshMeasurements.Bounds(clippedMesh, null);
            if (bounds.Min.x<X)
            {
				//cut of the left side
				mpc = new MeshPlaneCut(mesh, new Vector3d(X, Y, 0), new Vector3d(-1, 0, 0));
				mpc.Cut();
				clippedMesh = mpc.Mesh;
			}
            if (bounds.Max.x>X+tileSize)
            {
				//cut off the right side
				mpc = new MeshPlaneCut(clippedMesh, new Vector3d(X + tileSize, Y, 0), new Vector3d(1, 0, 0));
				mpc.Cut();
				clippedMesh = mpc.Mesh;
			}
			return clippedMesh;
			//cut off the top
		}

		private SubObject clipMesh(DMesh3 columnMesh, int X, int Y, float tileSize)
        {
			SubObject subObject; 
			DMesh3 clippedMesh = new DMesh3(false, false, false, false);
			clippedMesh.Copy(columnMesh);

			var bounds = MeshMeasurements.Bounds(clippedMesh, null);
			//cut off the top
			MeshPlaneCut mpc;
            if (bounds.Max.y>Y+tileSize)
            {
				//cut off the top
				mpc = new MeshPlaneCut(clippedMesh, new Vector3d(X + tileSize, Y + tileSize, 0), new Vector3d(0, 1, 0));
				mpc.Cut();
				clippedMesh = mpc.Mesh;
			}
            if (bounds.Min.y<Y)
            {
				//cut off the bottom
				mpc = new MeshPlaneCut(clippedMesh, new Vector3d(X, Y, 0), new Vector3d(0, -1, 0));
				mpc.Cut();
				clippedMesh = mpc.Mesh;
			}
           
            if (clippedMesh.VertexCount>0)
            {
				//create new subobject
				subObject = new SubObject();
				//var center = MeshMeasurements.Centroid(clippedMesh);
				subObject.centroid = new Vector2Double(X+(tileSize/2f), Y+(tileSize/2f));
				subObject.id = id;
				subObject.parentSubmeshIndex = parentSubmeshIndex;
				subObject.mesh = clippedMesh;
				subObject.saveMesh();

				return subObject;
            }

			return null;
        }

		public void calculateNormals()
        {
            for (int i = 0; i < triangleIndices.Count; i+=3)
            {
				int index1 = triangleIndices[i];
				int index2 = triangleIndices[i+1];
				int index3 = triangleIndices[i + 2];
				Vector3 normal = calculateNormal(vertices[index1], vertices[index2], vertices[index3]);
				normals[index1] = normal;
				normals[index2] = normal;
				normals[index3] = normal;
			}
        }

		private static Vector3 calculateNormal(Vector3Double v1, Vector3Double v2, Vector3Double v3)
		{
			Vector3 normal = new Vector3();
			Vector3Double U = v2 - v1;
			Vector3Double V = v3 - v1;

			double X = ((U.Y * V.Z) - (U.Z * V.Y));
			double Y = ((U.Z * V.X) - (U.X * V.Z));
			double Z = ((U.X * V.Y) - (U.Y * V.X));

			// normalize it
			double scalefactor = Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
			normal.X = (float)(X / scalefactor);
			normal.Y = (float)(Y / scalefactor);
			normal.Z = (float)(Z / scalefactor);
			return normal;

		}
	}
	 struct vertexNormalCombination : IEquatable<vertexNormalCombination>
	{
		public Vector3 normal;
		public Vector3Double vertex;

		public vertexNormalCombination(Vector3Double vertex, Vector3 normal)
		{
			this.vertex = vertex;
			this.normal = normal;

		}
		public bool Equals(vertexNormalCombination other)
        {
			float deltaX = Math.Abs(other.normal.X - normal.X);
			float deltaY = Math.Abs(other.normal.Y - normal.Y);
			float deltaZ = Math.Abs(other.normal.Z - normal.Z);
			bool normalIsAlmostTheSame = false;
			if (deltaX<0.01&&deltaY<0.001&deltaZ<0.001)
			{
				normalIsAlmostTheSame = true;
			}

			if (normalIsAlmostTheSame && other.vertex == vertex)
            {
				return true;
            }
			return false;
            
        }
    }
}
