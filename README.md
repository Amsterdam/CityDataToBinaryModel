# CityDataToBinaryModel



## Using the Tile Bake Tool

Download the latest release from the [releases page](releases/)
Extract with .zip file with all its contents to a folder.
Please note TileBakeTool.exe is not portable. So all .dll files are required for the tool to run.

Use `TileBikeTool.exe --help` to display the available parameters.
Best practice is to supply the required parameters via a config .json file.

Please take a look at the example [JSON config files](config/) in the config folder.

Use `TileBikeTool.exe --config PathToYourConfigFile.json` to start the tool using the config file.

## Binary tile data

[Binary tile byte order](docs/BinaryFileContents.md)

