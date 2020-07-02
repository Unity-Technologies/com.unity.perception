# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased] 
### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

##[0.2.0] - 2020-07-02
### Added
Added CameraLabeler, an extensible base type for all forms of dataset output from a camera.
Added LabelConfig\<T\>, a base class for mapping labels to data used by a labeler. There are two new derived types - ID label config and semantic segmentation label config.
### Changed
Moved the various forms of ground truth from PerceptionCamera into various subclasses of CameraLabeler.
Renamed SimulationManager to DatasetCapture.
Changed Semantic Segmentation to take a SemanticSegmentationLabelConfig, which maps labels to color pixel values.

## [0.1.0] - 2020-06-24
### This is the first release of the _Perception_ package
