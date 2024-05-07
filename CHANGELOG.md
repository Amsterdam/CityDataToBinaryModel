# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.3.0] - 07-5-2024

### Added

- Compress *-data.bin as well next to *.bin files if compression is enabled


## [0.2.0] - 15-4-2024

### Added

- Added this changelog
- Added ability to drag .bin files (or set as first parameter) into tool to generate .txt report of contents
- Added log file for tilebaketool run to show what problems were reported
- Added new 'minHoleSize' config parameter to skip holes in Solids below square meter surface area threshold
- Added new 'minHoleVertices' config parameter to skip holes with a vertex count below threshold
