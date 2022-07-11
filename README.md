# CityDataToBinaryModel

## TileBakeTool

The TileBakeTool is and executable that converts CityJSON files from a target folder into combined binary mesh tiles, either by adding the City Objects to a tile using a OVERLAP method, or TILED.

OVERLAP places a CityObject in a mesh tile if its centroid is within the tile bounds.
TILED cuts CityObjects using the bounds and places the parts into their tiles.

The TileBakeTool can selectively reduce polycount of specific CityObjects, and/or combine double verts, reducing filesize.

## Using the Tile Bake Tool

Download the latest release from the [releases page](https://github.com/Amsterdam/CityDataToBinaryModel/releases) and extract with .zip file with all its contents to a folder.<br>
Please note TileBakeTool.exe is not portable and all .dll files are required for the tool to run.

Use `TileBakeTool.exe --help` to display the available parameters.<br>
Best practice is to supply the required parameters via a config .json file.

Please take a look at the example [JSON config files](config/) in the config folder.

Use `TileBakeTool.exe --config PathToYourConfigFile.json` to start the tool using the config file.

## Binary tile data

[Binary tile byte order](docs/BinaryFileContents.md)

## GLTF Wrapper

Gltf files are created next to the binary tiles as a wrapper for the binary data.
This way the binary tiles can be used standalone, or used/imported as Gltf files with external binary mesh data.
The Gltf files also allow you to preview the 3D output using standard 3D viewers like the one Windows 10+ offers.

## Brotli compression

The TileBakeTool can optionaly create compressed versions of the binary tiles using Brotli compression, reducing download times for streaming in the data in  web applications.
