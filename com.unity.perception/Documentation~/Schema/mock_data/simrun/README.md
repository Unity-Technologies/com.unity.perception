Mockup of Synthetic Dataset

This is a mock dataset that is created according to this schema [design](https://docs.google.com/document/d/1lKPm06z09uX9gZIbmBUMO6WKlIGXiv3hgXb_taPOnU0)

Included in this mockup:

- 1 ego car
- 2 sensors: 1 camera and 1 LIDAR
- 19 labels
- 3 captures, 2 metrics, 1 sequence, 2 steps
    - the first includes 1 camera capture and 1 semantic segmentation annotation.
    - two captures, 1 camera capture and 1 LIDAR capture, are triggered at the same time. For the camera, semantic segmentation, instance segmentation and 3d bounding box annotations are provided. For the LIDAR sensor, semantic segmentation annotation of point cloud is included.
    - one of the metric event is emitted for metrics at capture level. The other one is emitted at annotation level.
- 4 types of annotations: semantic segmentation, 2d bounding box, 3d bounding box and LIDAR semantic segmentation.
- 1 type of metric: object count
