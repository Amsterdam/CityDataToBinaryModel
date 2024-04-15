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

using System.Collections.Generic;
using System.IO;
using Bunny83.SimpleJSON;
using TileBakeLibrary.Coordinates;
using System.Numerics;
using System;

namespace Netherlands3D.CityJSON
{
	public class CityJSON
	{
		public string sourceFilePath = "";

		public JSONNode cityJsonNode;
		public Vector3Double[] vertices;
		private List<Vector2> textureVertices;
		private List<Surfacetexture> Textures;
		private List<Material> Materials;

		private float LOD = -1;
		private string filterType = "";

		private Vector3Double transformScale;
		private Vector3Double transformOffset;

		public Vector3Double TransformOffset { get => transformOffset; }

		private int minHoleVertices = 3;
		private float minHoleSize = 1;

		public CityJSON(string filepath, bool applyTransformScale = true, bool applyTransformOffset = true, int minHoleVertices = 3, float minHoleSize = 1)
		{
			sourceFilePath = filepath;
			cityJsonNode = JSON.StreamParse(filepath);
			this.minHoleVertices = minHoleVertices;
			this.minHoleSize = minHoleSize;

			if (cityJsonNode == null || cityJsonNode["CityObjects"] == null)
			{
				Console.WriteLine($"Failed to parse CityJSON file {filepath}");
				return;
			}

			//Get vertices
			textureVertices = new List<Vector2>();
			Textures = new List<Surfacetexture>();

			//Optionaly parse transform scale and offset
			transformScale = (applyTransformScale && cityJsonNode["transform"] != null && cityJsonNode["transform"]["scale"] != null) ? new Vector3Double(
				cityJsonNode["transform"]["scale"][0].AsDouble,
				cityJsonNode["transform"]["scale"][1].AsDouble,
				cityJsonNode["transform"]["scale"][2].AsDouble
			) : new Vector3Double(1, 1, 1);

			transformOffset = (applyTransformOffset && cityJsonNode["transform"] != null && cityJsonNode["transform"]["translate"] != null) ? new Vector3Double(
				   cityJsonNode["transform"]["translate"][0].AsDouble,
				   cityJsonNode["transform"]["translate"][1].AsDouble,
				   cityJsonNode["transform"]["translate"][2].AsDouble
			) : new Vector3Double(0, 0, 0);

			//Load all the vertices with the scaler and offset applied
			vertices = new Vector3Double[cityJsonNode["vertices"].Count];
			int counter = 0;
			foreach (JSONNode node in cityJsonNode["vertices"])
			{
				var vertCoordinates = new Vector3Double(
						node[0].AsDouble * transformScale.X + transformOffset.X,
						node[1].AsDouble * transformScale.Y + transformOffset.Y,
						node[2].AsDouble * transformScale.Z + transformOffset.Z
				);
				vertices[counter++] = vertCoordinates;
			}
			//Get texture vertices
			foreach (JSONNode node in cityJsonNode["appearance"]["vertices-texture"])
			{
				textureVertices.Add(new Vector2(node[0].AsFloat, node[1].AsFloat));
			}
			foreach (JSONNode node in cityJsonNode["appearance"]["textures"])
			{
				Surfacetexture texture = new Surfacetexture();
				texture.path = filepath + node["image"];
				texture.wrapmode = node["wrapMode"];
				Textures.Add(texture);
			}
		}

		public int CityObjectCount()
        {
			return cityJsonNode["CityObjects"].Count;
        }

		public CityObject LoadCityObjectByIndex(int index, float lod)
        {
			this.LOD = lod;
			this.filterType = "";
			string key = cityJsonNode["CityObjects"].getKeyAtIndex(index);
			var node = cityJsonNode["CityObjects"][index];
			CityObject cityObject = ReadCityObject(node, filterType);
			if (cityObject != null)
			{
				cityObject.keyName = key;
			}
			return cityObject;
		}

		public void ClearCityObject(string key)
        {
			cityJsonNode["CityObjects"][key] = null;
        }

		public CityObject LoadCityObjectByKey(string key)
        {
			CityObject cityObject = ReadCityObject(cityJsonNode["CityObjects"][key], filterType);
			cityObject.keyName = key;
			return cityObject;
		}

		public List<CityObject> LoadCityObjects(float lod, string filterType = "")
		{
			List<CityObject> cityObjects = new List<CityObject>();
			this.LOD = lod;
			this.filterType = filterType;
			
			//Traverse the cityobjects as a key value pair, so we can read the unique key name and use it as a default identifier
			int counter = 0;
			
			foreach (KeyValuePair<string, JSONNode> kvp in (JSONObject)cityJsonNode["CityObjects"])
			{
				Console.Write("\r" + counter++);
				CityObject cityObject = ReadCityObject(kvp.Value, filterType);
				if (cityObject != null)
				{
					cityObject.keyName = kvp.Key;
					cityObjects.Add(cityObject);
				}
			}

			return cityObjects;
		}

		private CityObject ReadCityObject(JSONNode node, string filter = "")
		{
			CityObject cityObject = new CityObject();

			//Read filer object type
			var type = node["type"];
            if (type == null)
            {
				return null;
            }
			cityObject.cityObjectType = type.Value;
			if (filter != "" && (type == null || type != filter))
			{
				return null;
			}

			//read attributes
			List<Semantics> semantics = ReadSemantics(node["attributes"]);
			cityObject.semantics = semantics;

			//read surface geometry ( if we did not filter LOD, or the LOD matches )
			List<Surface> surfaces = new List<Surface>();
			foreach (JSONNode geometrynode in node["geometry"])
			{
				if (LOD == -1 || geometrynode["lod"].AsFloat == LOD)
				{
					if (geometrynode["type"] == "Solid")
					{
						JSONNode boundaries = geometrynode["boundaries"];
						surfaces = ReadSolid(geometrynode, cityObject);
					}
					else if (geometrynode["type"] == "MultiSurface")
					{
						surfaces = ReadMultiSurface(geometrynode, cityObject);
					}
					else
					{
						Console.WriteLine($"Unknown geometry type: {geometrynode["type"]}");
					}

					//read textureValues
					JSONNode texturenode = geometrynode["texture"];
					if (texturenode != null)
					{
						int counter = 0;
						JSONNode valuesNode = texturenode[0]["values"];
						if (geometrynode["type"] == "Solid")
						{
							JSONNode exteriorshell = valuesNode[0];
							foreach (JSONNode surfacenode in exteriorshell)
							{
								surfaces[counter] = AddSurfaceUVs(surfacenode, surfaces[counter]);
								counter++;
							}
							JSONNode interiorshell = valuesNode[1];
							if (interiorshell != null)
							{
								foreach (JSONNode surfacenode in interiorshell)
								{
									surfaces[counter] = AddSurfaceUVs(surfacenode, surfaces[counter]);
									counter++;
								}
							}
						}
						else if (geometrynode["type"] == "MultiSurface")
						{
							foreach (JSONNode surfacenode in valuesNode)
							{
								surfaces[counter] = AddSurfaceUVs(surfacenode, surfaces[counter]);
								counter++;
							}
						}
					}

					//read SurfaceAttributes
					JSONNode semanticsnode = geometrynode["semantics"];
					if (semanticsnode != null)
					{
						if (geometrynode["type"] == "Solid")
						{
							for (int i = 0; i < semanticsnode["values"][0].Count; i++)
							{
								int semanticsID = semanticsnode["values"][0][i].AsInt;
								surfaces[i].semantics = ReadSemantics(semanticsnode["surfaces"][semanticsID]);
							}
						}
						if (geometrynode["type"] == "MultiSurface")
						{
							for (int i = 0; i < semanticsnode["values"].Count; i++)
							{
								int semanticsID = semanticsnode["values"][i].AsInt;
								surfaces[i].semantics = ReadSemantics(semanticsnode["surfaces"][semanticsID]);
							}
						}

					}

					// read MaterialValues
					JSONNode materialnode = geometrynode["material"];
				}
			}

			cityObject.surfaces = surfaces;

			//process children
			JSONNode childrenNode = node["children"];
			if (childrenNode != null)
			{
				List<CityObject> children = new List<CityObject>();
				for (int i = 0; i < childrenNode.Count; i++)
				{
					string childname = childrenNode[i];
					JSONNode childnode = cityJsonNode["CityObjects"][childname];
					if (childnode == null)
					{
						Console.WriteLine($"Could not find child with key: {childname}");
					}

					CityObject child = ReadCityObject(childnode);
					child.keyName = childname;

					if (child != null)
					{
						children.Add(child);
					}
				}
				cityObject.children = children;
			}

			return cityObject;
		}

		private List<Surface> ReadSolid(JSONNode geometrynode, CityObject sourceCityObject)
		{
			JSONNode boundariesNode = geometrynode["boundaries"];
			List<Surface> result = new List<Surface>();

			//Read exterior shell
			foreach (JSONNode surfacenode in boundariesNode[0])
			{		
				result.Add(ReadSurfaceVectors(surfacenode,sourceCityObject,true,true));
			}

			return result;
		}

		private List<Surface> ReadMultiSurface(JSONNode geometrynode, CityObject sourceCityObject)
		{
			JSONNode boundariesNode = geometrynode["boundaries"];
			List<Surface> result = new List<Surface>();
			foreach (JSONNode node in boundariesNode)
			{
				Surface surf = new Surface();
				foreach (JSONNode vertexnode in node[0])
				{
					surf.outerRing.Add(vertices[vertexnode.AsInt]);
				}
				for (int i = 1; i < node.Count; i++)
				{
					List<Vector3Double> innerRing = new List<Vector3Double>();
					foreach (JSONNode vertexnode in node[i])
					{
						innerRing.Add(vertices[vertexnode.AsInt]);

					}
					surf.innerRings.Add(innerRing);
				}
				result.Add(surf);
			}
			//add semantics
			JSONNode semanticsnode = geometrynode["semantics"];
			JSONNode ValuesNode = semanticsnode["values"];
			for (int i = 0; i < ValuesNode.Count; i++)
			{
				string surfacetype = geometrynode["semantics"]["surfaces"][ValuesNode[i].AsInt]["type"];
				result[i].SurfaceType = geometrynode["semantics"]["surfaces"][ValuesNode[i].AsInt]["type"];
				result[i].semantics = ReadSemantics(geometrynode["semantics"]["surfaces"][ValuesNode[i].AsInt]);
			}

			return result;
		}

		private Surface AddSurfaceUVs(JSONNode UVValueNode, Surface surf)
		{
			List<Vector2> UVs = new List<Vector2>();

			foreach (JSONNode vectornode in UVValueNode[0])
			{
				if (surf.TextureNumber == -1)
				{
					surf.TextureNumber = vectornode.AsInt;
					surf.surfacetexture = Textures[surf.TextureNumber];
				}
				else
				{
					UVs.Add(textureVertices[vectornode.AsInt]);
				}
			}
			//UVs.Reverse();
			surf.outerringUVs = UVs;

			//inner rings
			for (int i = 1; i < UVValueNode.Count; i++)
			{
				UVs = new List<Vector2>();
				int counter = 0;
				foreach (JSONNode vectornode in UVValueNode[i])
				{
					if (counter > 0)
					{
						UVs.Add(textureVertices[vectornode.AsInt]);
					}
					counter++;
				}
				//UVs.Reverse();
				surf.innerringUVs.Add(UVs);
			}
			return surf;
		}
		private Surface ReadSurfaceVectors(JSONNode surfacenode, CityObject sourceCityObject, bool createOuterRing = true, bool createInnerRings = true)
		{
			Surface surf = new Surface();
			//read exteriorRing
			List<Vector3Double> verts = new List<Vector3Double>();

			if(createOuterRing)
			{
				foreach (JSONNode vectornode in surfacenode[0])
				{
					verts.Add(vertices[vectornode.AsInt]);
				}
				surf.outerRing = verts;	
			}			

			if(!createInnerRings)
				return surf;

			Vector3Double v1;
			Vector3Double v2;
			for (int i = 1; i < surfacenode.Count; i++)
			{
				verts = new List<Vector3Double>();
				foreach (JSONNode vectornode in surfacenode[i])
				{
					verts.Add(vertices[vectornode.AsInt]);
				}

				//Skip certain holes if they are too small, of dont have enough vertices
				if(verts.Count < minHoleVertices) 
				{
					sourceCityObject.holeWarnings += $"- Found a hole with less than {minHoleVertices} vertices, skipping this hole.\n";
				}
				else
				{
					//Calculate the area of the hole
					double holeSize = 0;
					for (int j = 0; j < verts.Count; j++)
					{
						v1 = verts[j];
						v2 = verts[(j + 1) % verts.Count];
						holeSize += (v1.X * v2.Z - v1.Z * v2.X);
					}
					holeSize = Math.Abs(holeSize) / 2;
					
					if(holeSize < minHoleSize)
					{
						sourceCityObject.holeWarnings += $"- Found a hole with an area size of {holeSize}, skipping this hole.\n";
						continue;
					}

					surf.innerRings.Add(verts);	
				}
			}
			
			return surf;
		}

		private List<Semantics> ReadSemantics(JSONNode semanticsNode)
		{
			List<Semantics> result = new List<Semantics>();

			if (semanticsNode!=null)
			{
				foreach (KeyValuePair<string, JSONNode> kvp in semanticsNode)
				{
					result.Add(new Semantics(kvp.Key, kvp.Value));
				}
			}

			return result;
		}
	}
	public class CityObject
	{
		public List<Semantics> semantics;
		public List<Surface> surfaces;
		public List<CityObject> children;
		public string cityObjectType;
		public string keyName = "";

		public string holeWarnings = "";
		public string triangulationWarnings = "";

		public CityObject()
		{
			semantics = new();
			surfaces = new();
			children = new();
		}
	}

	public class Surface
	{
		public List<Vector3Double> outerRing;
		public List<List<Vector3Double>> innerRings;
		public List<Vector2> outerringUVs;
		public List<List<Vector2>> innerringUVs;
		public string SurfaceType;
		public List<Semantics> semantics;
		public Surfacetexture surfacetexture;
		public int TextureNumber;

		public Surface()
		{
			TextureNumber = -1;
			semantics = new List<Semantics>();
			outerRing = new List<Vector3Double>();
			innerRings = new List<List<Vector3Double>>();
			outerringUVs = new List<Vector2>();
			innerringUVs = new List<List<Vector2>>();
		}
	}

	public class Surfacetexture
	{
		public string path;
		public string wrapmode;
	}

	[System.Serializable]
	public class Semantics
	{
		public string name;
		public string value;
		public Semantics(string Name, string Value)
		{
			name = Name;
			value = Value;
		}
	}

	public class Material
	{
		public string name;
		public float r;
		public float g;
		public float b;
		public float a;
	}
}
