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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileBakeTool
{
	class Constants
	{
		public static string helpText = @"

           // Netherlands3D Binary Tiles Generator //


This tool parses CityJSON files and bakes them into single-mesh binary tile files.
Seperate metadata files contain the description and geometry location of sub-objects.
Check out https://github.com/Amsterdam/CityDataToBinaryModel for example config files and help.

Required parameter:

--config <path to a .json config file>

Optional options:

--source <Override config path to source files>
--output <Override config path to output target>
--lod <Override config lod filter setting>

Pipeline example 1
TileBakeTool.exe --config Buildings.json
TileBakeTool.exe --config Terrain.json
TileBakeTool.exe --config Trees.json

Pipeline example 2
Exporting two LOD datasets with same config template:

TileBakeTool.exe --config Buildings.json --lod 1.2 --output ""C:/buildings/buildings_1.2_""
TileBakeTool.exe --config Buildings.json --lod 2.0 --output ""C:/Buildings/buildings_2.0_""
";

	}
}
