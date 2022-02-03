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
public class ConfigFile
{
	//Mandatory settings. The application should not start without them specified by the user:
	public string sourceFolder { get; set; }
	public string outputFolder { get; set; }
	public bool replaceExistingObjects { get; set; }
	public string identifier { get; set; }
	public string removePartOfIdentifier { get; set; }
	public bool exportUVCoordinates { get; set; }
	public float lod { get; set; }
	public string tilingMethod { get; set; }


	//Optional settings with predefined default values:
	public int tileSize { get; set; } = 1000;
	public float mergeVerticesBelowAngle { get; set; } = 5;
	public bool brotliCompression { get; set; } = false;
	public bool removeSpikes { get; set; } = false;
	public float removeSpikesAbove { get; set; } = 25;
	public float removeSpikesBelow { get; set; } = -10;

	public CityObjectFilter[] cityObjectFilters { get; set; }
}

public class CityObjectFilter
{
	public string objectType { get; set; }
	public int defaultSubmeshIndex { get; set; }
	public AttributeFilter[] attributeFilters { get; set; }
	public float maxVerticesPerSquareMeter { get; set; }
}

public class AttributeFilter
{
	public string attributeName { get; set; }
	public ValueToSubmesh[] valueToSubMesh { get; set; }
}

public class ValueToSubmesh
{
	public string value { get; set; }
	public int submeshIndex { get; set; }
}
