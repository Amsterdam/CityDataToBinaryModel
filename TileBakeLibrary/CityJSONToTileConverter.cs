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
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using TileBakeLibrary.Coordinates;
using Bunny83.SimpleJSON;
using System.Linq;
using Netherlands3D.CityJSON;
using JoeStrout;
using System.Threading;
using System.Collections.Concurrent;
using TileBakeLibrary.BinaryMesh;
using System.Diagnostics;

namespace TileBakeLibrary
{
	public class CityJSONToTileConverter
	{
		private string sourcePath = "";
		private string outputPath = "";
		private string identifier = "";
		private string removeFromID = "";
		private bool brotliCompress = false;
		private bool replaceExistingIDs = true;
		private bool exportUVCoordinates = false;

		private float lod = 0;

		public string TilingMethod = "OVERLAP"; //OVERLAP, TILED

		private int tileSize = 1000;

		private List<SubObject> allSubObjects = new List<SubObject>();
		private List<Tile> tiles = new List<Tile>();

		private CityJSON cityJson;
		private CityObjectFilter[] cityObjectFilters;

		private bool clipSpikes = false;
		private float spikeCeiling = 0;
		private float spikeFloor = 0;

		private int filecounter = 0;
		private int totalFiles = 0;

		/// <summary>
		/// Sets the normal angle threshold for vertex+normal combinations to be considered the same
		/// </summary>
		/// <param name="mergeVerticesBelowNormalAngle">Angle in degrees.</param>
		public void SetVertexMergeAngleThreshold(float mergeVerticesBelowNormalAngle)
		{
			Console.WriteLine($"Merging vertices with normal angle threshold: {mergeVerticesBelowNormalAngle}");
			VertexNormalCombination.normalAngleComparisonThreshold = mergeVerticesBelowNormalAngle;
		}

		/// <summary>
		/// Set vertex max floor and height to clip off spikes.
		/// Verts below floor or above ceiling will be reset to 0.
		/// </summary>
		/// <param name="setFunction"></param>
		/// <param name="ceiling">Max vertex height allowed</param>
		/// <param name="floor">Lowest vertex height allowed</param>
		public void SetClipSpikes(bool setFunction, float ceiling, float floor)
		{
			clipSpikes = setFunction;
			spikeCeiling = ceiling;
			spikeFloor = floor;
		}

		/// <summary>
		/// Sets the square tile size
		/// </summary>
		/// <param name="tilesize">Value used for width and height of the tiles</param>
		public void SetTileSize(int tilesize)
		{
			Console.WriteLine($"Tilesize set to: {tilesize}x{tilesize}m");
			tileSize = tilesize;
		}

		/// <summary>
		/// The LOD we want to parse. 
		/// </summary>
		/// <param name="targetLOD">Defaults to 0</param>
		public void SetLOD(float targetLOD)
		{
			Console.WriteLine($"Filtering on LOD: {targetLOD}");
			lod = targetLOD;
		}

		/// <summary>
		/// Determines the property that will be used as unique object identifier
		/// </summary>
		/// <param name="id">The propery field name, for example 'building_id'</param>
		/// <param name="remove">Remove this substring from the ID before storing it</param>
		public void SetID(string id, string remove)
		{
			identifier = id;
			removeFromID = remove;
		}

		/// <summary>
		/// The source folder path containing all .cityjson files that need to be converted
		/// </summary>
		/// <param name="source"></param>
		public void SetSourcePath(string source)
		{
			sourcePath = source;
		}

		/// <summary>
		/// Target folder where the generated binary tiles should be placed
		/// </summary>
		/// <param name="target"></param>
		public void SetTargetPath(string target)
		{
			outputPath = target;
		}

		/// <summary>
		/// Parse exisiting binary tile files and add the parsed objects to them
		/// </summary>
		/// <param name="add">Add to existing tiles</param>
		public void SetReplace(bool replace)
		{
			this.replaceExistingIDs = replace;
		}

		/// <summary>
		/// Sets the output of UV coordinates.
		/// CityJSON input should contain UV texture coordinates.
		/// </summary>
		/// <param name="exportUV"></param>
		public void SetExportUV(bool exportUV)
		{
			this.exportUVCoordinates = exportUV;
		}

		/// <summary>
		/// Create a brotli compressed version of the binary tiles
		/// </summary>
		public void AddBrotliCompressedFile(bool brotliCompress)
		{
			this.brotliCompress = brotliCompress;
		}

		/// <summary>
		/// Set the filter types for CityObjects
		/// </summary>
		/// <param name="cityObjectFilters"></param>
		public void SetObjectFilters(CityObjectFilter[] cityObjectFilters)
		{
			this.cityObjectFilters = cityObjectFilters;
		}

		/// <summary>
		/// Start converting the cityjson files into binary tile files
		/// </summary>
		/// 
		public void Convert()
		{
			//If no specific filename or wildcard was supplied, default to .json files
			var filter = Path.GetFileName(sourcePath);
			if (filter == "") filter = "*.json";

			//Check if source path exists
			if(!Directory.Exists(sourcePath))
            {
				Console.WriteLine($"Source path does not exist: {sourcePath}");
				Console.WriteLine($"Aborted.");
				return;
            }

			//List the files that we are going to parse
			string[] sourceFiles = Directory.GetFiles(sourcePath, filter);
			if (sourceFiles.Length == 0)
			{
				Console.WriteLine($"No \"{filter}\" files found in {sourcePath}.");
				Console.WriteLine($"Please check if the sourceFolder in your config file is correct.");
				return;
			}

			//Create a threadable task for every file, that returns a list of parsed cityobjects
			Console.WriteLine($"Parsing {sourceFiles.Length} CityJSON files...");
			totalFiles = sourceFiles.Length;
			if (sourceFiles.Length > 0)
			{
				cityJson = new CityJSON(sourceFiles[0], true, true);
			}
			for (int i = 0; i < sourceFiles.Length; i++)
            {
				//Start reading the next CityJSON in a seperate thread to prepare for the next loop
				CityJSON nextCityJSON = null;
                int nextJsonID = i + 1;
                if (i + 1 == sourceFiles.Length)
                {
                    nextJsonID = i;
                }
                Thread thread;
                thread = new Thread(() =>  {  nextCityJSON = new CityJSON(sourceFiles[nextJsonID], true, true);  });
                thread.Start();

				//Start reading current CityJSON
				filecounter++;
				Console.WriteLine($"\nProcessing file {filecounter}/{sourceFiles.Length}");
				ReadCityJSON();

                //Wait untill the thread reading the next CityJSON is read so we can start reading it
                thread.Join();
                cityJson = nextCityJSON;
            }

			//Optional compressed variant
            if (brotliCompress)
			{
				CompressFiles();
			}
		}

		/// <summary>
		/// Read the CityObjects from the current CityJSON
		/// </summary>
        private void ReadCityJSON()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            tiles = new List<Tile>();
         
            var cityObjects = CityJSONParseProcess(cityJson);
            allSubObjects.Clear();
            allSubObjects = cityObjects;

            Console.WriteLine($"\n{allSubObjects.Count} CityObjects with LOD{lod} were imported");
            PrepareTiles();
            WriteTileData();

            //Clean up
            allSubObjects.Clear();
            cityJson = null;
            GC.Collect();
            watch.Stop();
            var result = watch.Elapsed;
            string elapsedTimeString = string.Format("{0}:{1} minutes",
                                      result.Minutes.ToString("00"),
                                      result.Seconds.ToString("00"));
            Console.WriteLine($"Duration: {elapsedTimeString}");
        }


        private void PrepareTiles()
		{
			TileSubobjects();
			AddObjectsFromBinaryTile();
		}

		/// <summary>
		/// Group the SubObjects into tiles using their centroids
		/// </summary>
		private void TileSubobjects()
		{
			tiles.Clear();
			foreach (SubObject cityObject in allSubObjects)
			{
				double tileX = Math.Floor(cityObject.centroid.X / tileSize) * (int)tileSize;
				double tileY = Math.Floor(cityObject.centroid.Y / tileSize) * (int)tileSize;

				if (tileX == 0 || tileY == 0)
				{
					Console.WriteLine("Found cityObject with no geometry");
				}

				Vector2Double tileposition;
				bool found = false;
				for (int i = 0; i < tiles.Count; i++)
				{
					tileposition = tiles[i].position;
					if (tileposition.X == tileX)
					{
						if (tileposition.Y == tileY)
						{
							tiles[i].SubObjects.Add(cityObject);
							found = true;
							break;
						}

					}
				}
				if (!found)
				{
					Tile newtile = new Tile();
					newtile.size = new Vector2(tileSize, tileSize);
					newtile.position = new Vector2Double(tileX, tileY);
					newtile.filePath = $"{outputPath}{tileX}_{tileY}.{lod}.bin";
					newtile.SubObjects.Add(cityObject);
					tiles.Add(newtile);
				}
			}
		}

		/// <summary>
		/// Parse the existing binary Tile files
		/// </summary>
		private void AddObjectsFromBinaryTile()
		{
			foreach (Tile tile in tiles)
			{
				//Parse exisiting file
				if (File.Exists(tile.filePath))
				{
					ParseExistingBinaryTile(tile);
				}
			}
		}

		private void WriteTileData()
		{
			//Create binary files (if we added subobjects to it)
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
			Console.WriteLine($"\nBaking {tiles.Count} tiles");
			//Threaded writing of binary meshes + compression
			int counter = 0;
			int written = 0;
			int skipped = 0;
			int total = tiles.Count;

			Parallel.ForEach(tiles, tile =>
			{
				if (tile.SubObjects.Count == 0 || tile.filePath.Contains("NaN"))
				{
					Interlocked.Increment(ref skipped);		
				}
				else
				{
					BinaryMeshData bmd = new BinaryMeshData();
					bmd.ExportData(tile, exportUVCoordinates);
					Interlocked.Increment(ref written);
				}
				Interlocked.Increment(ref counter);
				WriteBakingLog(counter, written, skipped, total);
			});

			WriteBakingLog(counter, written, skipped, total);
			Console.WriteLine($"\n{written} tiles saved");
		}

		public static void WriteBakingLog(int done, int written, int skipped, int total)
        {
			float percentageDone = ((float)done / total) * 100.0f;
			Console.Write($"\rTile baking process: {(percentageDone):F0}% | Baked: {written} | Skipped empty tiles: {skipped}" + "             ");
        }

		/// <summary>
		/// Rewrite threaded compressing status message on the current console line.
		/// </summary>
		/// <param name="done">Total binary tiles compressed</param>
		/// <param name="total">Total binary tiles to compress</param>
		public static void WriteCompressingLog(int done, int total)
		{
			float percentageDone = ((float)done / total) * 100.0f;
			Console.WriteLine($"\rCompressing process: {(percentageDone):F0}% | Compressed: {done}/{total}       ");
		}

		private static void WriteParsingStatusToConsole(int skipped, int done, int parsing, int simplifying, int tiling)
        {
            Console.Write("\rDone: " + done + " | Skipped: " + skipped + " | Parsing: " + parsing + " | Simplifying: " + simplifying + " | Tiling: " + tiling + "             ");
        }

        /// <summary>
        /// Compress all files binary files in the output folder into brotli compressed files with .br extention 
        /// </summary>
        public void CompressFiles()
		{
			var filter = "*{lod}.bin";

			//List the files that we are going to parse
			string[] binFiles = Directory.GetFiles(Path.GetDirectoryName(outputPath), filter);
			Stopwatch watch = new Stopwatch();
			watch.Start();
			int compressedCount = 0;
			int totalcount = binFiles.Length;
			if(totalcount == 0)
            {
				Console.WriteLine("\nNo tile files found to compress. It appears no tiles were baked.");
				return;
            }

			Console.WriteLine("\nCompressing files");
			Parallel.ForEach(binFiles, filename =>
			{
				if (brotliCompress)
				{
					BrotliCompress.Compress(filename);
				}
				Interlocked.Increment(ref compressedCount);
				WriteCompressingLog(compressedCount, totalcount);
			});
			watch.Stop();
			var result = watch.Elapsed;
			string elapsedTimeString = string.Format("{0}:{1} minutes",
									  result.Minutes.ToString("00"),
									  result.Seconds.ToString("00"));
			Console.WriteLine($"\nDuration: {elapsedTimeString}");
		}

		/// <summary>
		/// Read SubObjects from existing binary tile file
		/// </summary>
		private void ParseExistingBinaryTile(Tile tile)
		{
			BinaryMeshData bmd = new BinaryMeshData();
			bmd.ImportData(tile, replaceExistingIDs);
			bmd = null;
		}

		private List<SubObject> CityJSONParseProcess(CityJSON cityJson)
		{
			List<SubObject> filteredObjects = new List<SubObject>();
			Console.WriteLine("");
			Console.WriteLine("Reading CityObjects from CityJSON");
			int cityObjectCount = cityJson.CityObjectCount();
			Console.WriteLine($"\rCityObjects found: {cityObjectCount}");
			Console.Write("---");
			int skipped = 0;
			int done = 0;
			int parsing = 0;
			int simplifying = 0;
			int tiling = 0;
			var filterObjectsBucket = new ConcurrentBag<SubObject>();
			int[] indices = Enumerable.Range(0, cityObjectCount).ToArray();

			//Turn cityobjects (and their children) into SubObject mesh data
			var partitioner = Partitioner.Create(indices, EnumerablePartitionerOptions.NoBuffering);
			Parallel.ForEach(partitioner, i =>
			{
				Interlocked.Increment(ref parsing);
				WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);

				CityObject cityObject = cityJson.LoadCityObjectByIndex(i, lod);
				var subObject = ToSubObjectMeshData(cityObject);
				cityObject = null;
				Interlocked.Decrement(ref parsing);
				cityObject = null;
				if (subObject == null)
				{
					Interlocked.Increment(ref done);
					Interlocked.Increment(ref skipped);
					return;
				}
				WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);

				if (subObject.maxVerticesPerSquareMeter > 0)
				{
					Interlocked.Increment(ref simplifying);
					WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);
					subObject.SimplifyMesh();
					Interlocked.Decrement(ref simplifying);
					WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);
				}
				else
				{
					//Always merge based on VertexNormalCombination.normalAngleComparisonThreshold
					subObject.MergeSimilarVertices();
				}
				if (clipSpikes)
				{
					subObject.ClipSpikes(spikeCeiling, spikeFloor);
				}

				if (TilingMethod == "TILED")
				{
					Interlocked.Increment(ref tiling);
					WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);
					var newSubobjects = subObject.ClipSubobject(new Vector2(tileSize, tileSize));
					if (newSubobjects.Count == 0)
					{
						subObject.CalculateNormals();
						filterObjectsBucket.Add(subObject);
					}
					else
					{
						foreach (var newsubObject in newSubobjects)
						{
							if (newsubObject != null)
							{
								filterObjectsBucket.Add(newsubObject);
							}
						}
					}
					Interlocked.Decrement(ref tiling);
					WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);
				}
				else
				{
					filterObjectsBucket.Add(subObject);
				}

				Interlocked.Increment(ref done);
				WriteParsingStatusToConsole(skipped, done, parsing, simplifying, tiling);
			}
			);

			return filterObjectsBucket.ToList();
		}

		private SubObject ToSubObjectMeshData(CityObject cityObject)
		{
			var subObject = new SubObject();
			subObject.vertices = new List<Vector3Double>();
			subObject.normals = new List<Vector3>();
			subObject.uvs = new List<Vector2>();
			subObject.triangleIndices = new List<int>();
			subObject.id = cityObject.keyName;

			int submeshindex = -1;

			// figure out the intended submesh and required meshDensity
			for (int i = 0; i < cityObjectFilters.Length; i++)
			{
				if (cityObjectFilters[i].objectType == cityObject.cityObjectType)
				{
					submeshindex = cityObjectFilters[i].defaultSubmeshIndex;
					subObject.maxVerticesPerSquareMeter = cityObjectFilters[i].maxVerticesPerSquareMeter;
					subObject.skipTrianglesBelowArea = cityObjectFilters[i].skipTrianglesBelowArea;
					for (int j = 0; j < cityObjectFilters[i].attributeFilters.Length; j++)
					{
						string attributename = cityObjectFilters[i].attributeFilters[j].attributeName;
						for (int k = 0; k < cityObjectFilters[i].attributeFilters[j].valueToSubMesh.Length; k++)
						{
							string value = cityObjectFilters[i].attributeFilters[j].valueToSubMesh[k].value;
							for (int l = 0; l < cityObject.semantics.Count; l++)
							{
								if (cityObject.semantics[l].name == attributename)
								{
									if (cityObject.semantics[l].value == value)
									{
										submeshindex = cityObjectFilters[i].attributeFilters[j].valueToSubMesh[k].submeshIndex;
									}
								}
							}
						}
					}
				}
			}

			subObject.parentSubmeshIndex = submeshindex;
			if (submeshindex == -1)
			{
				return null;
			}

			//If we supplied a specific identifier field, use it as ID instead of object key index
			if (identifier != "")
			{
				foreach (var semantic in cityObject.semantics)
				{
					//Console.WriteLine(semantic.name);
					if (semantic.name == identifier)
					{
						subObject.id = semantic.value;
                        if (removeFromID!="")
                        {
							subObject.id = subObject.id.Replace(removeFromID, "");
						}
						
						break;
					}
				}
			}
			bool calculateNormals = false;
			if (subObject.maxVerticesPerSquareMeter == 0)
			{
				calculateNormals = true;
			}

			AppendCityObjectGeometry(cityObject, subObject, calculateNormals);
			//Append all child geometry too
			for (int i = 0; i < cityObject.children.Count; i++)
			{
				var childObject = cityObject.children[i];
				//Add child geometry to our subobject. (Recursive children are not allowed in CityJson)
				AppendCityObjectGeometry(childObject, subObject, calculateNormals);
			}
			//Check if the list if triangles is complete (divisible by 3)
			if (subObject.triangleIndices.Count % 3 != 0)
			{
				Console.WriteLine($"{subObject.id} triangle list is not divisible by 3. This is not correct.");
				return null;
			}

			//Calculate centroid using the city object vertices
			Vector3Double centroid = new Vector3Double();
			for (int i = 0; i < subObject.vertices.Count; i++)
			{
				centroid.X += subObject.vertices[i].X;
				centroid.Y += subObject.vertices[i].Y;
			}
			subObject.centroid = new Vector2Double(centroid.X / subObject.vertices.Count, centroid.Y / subObject.vertices.Count);

			return subObject;
		}

		private static void AppendCityObjectGeometry(CityObject cityObject, SubObject subObject, bool calculateNormals)
		{
			List<Vector3Double> vertexlist = new List<Vector3Double>();
			Vector3 defaultNormal = new Vector3(0, 1, 0);
			List<Vector3> defaultnormalList = new List<Vector3> { defaultNormal, defaultNormal, defaultNormal };
			List<Vector3> normallist = new List<Vector3>();
			List<Vector2> uvsList = new List<Vector2>();
			List<int> indexlist = new List<int>();

			int count = subObject.vertices.Count;
			foreach (var surface in cityObject.surfaces)
			{
				//findout if ity is already a triangle
				if (surface.outerRing.Count == 3 && surface.innerRings.Count == 0)
				{
					count = vertexlist.Count + subObject.vertices.Count;
					List<int> newindices = new List<int> { count, count + 1, count + 2 };
					count += 3;
					indexlist.AddRange(newindices);
					vertexlist.AddRange(surface.outerRing);
					uvsList.AddRange(surface.outerringUVs);
					if (calculateNormals)
					{
						Vector3 normal = CalculateNormal(surface.outerRing[0], surface.outerRing[1], surface.outerRing[2]);
						normallist.Add(normal);
						normallist.Add(normal);
						normallist.Add(normal);
					}
					else
					{
						normallist.AddRange(defaultnormalList);
					}

					continue;
				}

				//Our mesh output data per surface
				Vector3[] surfaceVertices;
				Vector3[] surfaceNormals;
				Vector2[] surfaceUvs;
				int[] surfaceIndices;

				//offset using first outerRing vertex position
				Vector3Double offsetPolygons = surface.outerRing[0];
				List<Vector3> outside = new List<Vector3>();
				for (int i = 0; i < surface.outerRing.Count; i++)
				{
					outside.Add((Vector3)(surface.outerRing[i] - offsetPolygons));
				}
				List<List<Vector3>> holes = new List<List<Vector3>>();
				for (int i = 0; i < surface.innerRings.Count; i++)
				{
					List<Vector3> inner = new List<Vector3>();
					for (int j = 0; j < surface.innerRings[i].Count; j++)
					{
						inner.Add((Vector3)(surface.innerRings[i][j] - offsetPolygons));
					}
					holes.Add(inner);
				}

				//Uv's
				List<Vector2> outsideUvs = new List<Vector2>();
				List<List<Vector2>> holeUvs = new List<List<Vector2>>();
				for (int i = 0; i < surface.outerringUVs.Count; i++)
				{
					outsideUvs.Add(surface.outerringUVs[i]);
				}
				for (int i = 0; i < surface.innerringUVs.Count; i++)
				{
					holeUvs.Add(surface.innerringUVs[i]);
				}

				//Turn poly into triangulated geometry data
				Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();
				poly.outside = outside;
				poly.holes = holes;
				poly.outsideUVs = outsideUvs;
				poly.holesUVs = holeUvs;

				if (poly.outside.Count < 3)
				{
					Console.WriteLine("Polygon seems to be a line");
					continue;
				}
				//Poly2Mesh takes care of calculating normals, using a right-handed coordinate system
				Poly2Mesh.CreateMeshData(poly, out surfaceVertices, out surfaceNormals, out surfaceIndices, out surfaceUvs);

				var offset = vertexlist.Count + subObject.vertices.Count;

				//Append verts, normals and uvs
				for (int j = 0; j < surfaceVertices.Length; j++)
				{
					vertexlist.Add(((Vector3Double)surfaceVertices[j]) + offsetPolygons);
					normallist.Add(surfaceNormals[j]);

					if (surfaceUvs != null)
					{
						uvsList.Add(surfaceUvs[j]);
					}
				}

				//Append indices ( corrected to offset )
				for (int j = 0; j < surfaceIndices.Length; j++)
				{
					indexlist.Add(offset + surfaceIndices[j]);
				}
			}

			if (vertexlist.Count > 0)
			{
				subObject.vertices.AddRange(vertexlist);
				subObject.triangleIndices.AddRange(indexlist);
				subObject.normals.AddRange(normallist);
				subObject.uvs.AddRange(uvsList);
			}
		}

		private static Vector3 CalculateNormal(Vector3Double v1, Vector3Double v2, Vector3Double v3)
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
		public void Cancel()
		{
			throw new NotImplementedException();
		}
	}
}
