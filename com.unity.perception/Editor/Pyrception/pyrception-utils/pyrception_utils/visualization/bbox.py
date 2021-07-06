import math

import numpy as np
from pyquaternion import Quaternion


def group_bbox2d_per_label(bboxes):
    """Group 2D bounding boxes with same label.

    Args:
        bboxes (list[BBox2D]): a list of 2D bounding boxes

    Returns:
        dict: a dictionary of 2d boundign box group.
        {label1: [bbox1, bboxes2, ...], label2: [bbox1, ...]}
    """
    bboxes_per_label = {}
    for box in bboxes:
        if box.label not in bboxes_per_label:
            bboxes_per_label[box.label] = []
        bboxes_per_label[box.label].append(box)

    return bboxes_per_label


class BBox2D:
    """Canonical Representation of a 2D bounding box.

    Attributes:
        label (str): string representation of the label.
        x (float): x pixel coordinate of the upper left corner.
        y (float): y pixel coordinate of the upper left corner.
        w (float): width (number of pixels)of the bounding box.
        h (float): height (number of pixels) of the bounding box.
        score (float): detection confidence score. Default is set to score=1.
            if this is a ground truth bounding box.

    Examples:
        Here is an example about how to use this class.

        .. code-block::

            >>> gt_bbox = BBox2D(label='car', x=2, y=6, w=2, h=4)
            >>> gt_bbox
            "label='car'|score=1.0|x=2.0|y=6.0|w=2.0|h=4.0"
            >>> pred_bbox = BBox2D(label='car', x=2, y=5, w=2, h=4, score=0.79)
            >>> pred_bbox.area
            8
            >>> pred_bbox.intersect_with(gt_bbox)
            True
            >>> pred_bbox.intersection(gt_bbox)
            6
            >>> pred_bbox.union(gt_bbox)
            10
            >>> pred_bbox.iou(gt_bbox)
            0.6

    """

    def __init__(self, label, x, y, w, h, score=1.0):
        """ Initialize 2D bounding box object

        Args:
            label (str): string representation of the label
            x (float): x pixel coordinate of the upper left corner
            y (float): y pixel coordinate of the upper left corner
            w (float): width (number of pixels)of the bounding box
            h (float): height (number of pixels) of the bounding box
            score (float): detection confidence score
        """
        self.label = label
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.score = score

    def __repr__(self):
        return (
            f"label={self.label}|score={self.score:.2f}|"
            f"x={self.x:.2f}|y={self.y:.2f}|w={self.w:.2f}|h={self.h:.2f}"
        )

    def __eq__(self, other):
        return (
            self.x == other.x
            and self.y == other.y
            and self.w == other.w
            and self.h == other.h
            and self.label == other.label
            and math.isclose(self.score, other.score, rel_tol=1e-07)
        )

    @property
    def area(self):
        """Calculate area of this bounding box

        Returns:
            width x height of the bound box
        """
        return self.w * self.h

    def intersect_with(self, other):
        """Check whether this box intersects with other bounding box

        Args:
            other (BBox2D): other bounding box object to check intersection

        Returns:
            True if two bounding boxes intersect, False otherwise
        """
        if self.x > other.x + other.w:
            return False
        if other.x > self.x + self.w:
            return False
        if self.y + self.h < other.y:
            return False
        if self.y > other.y + other.h:
            return False
        return True

    def intersection(self, other):
        """Calculate the intersection area with other bounding box

        Args:
            other (BBox2D): other bounding box object to calculate intersection

        Returns:
            float of the intersection area for two bounding boxes
        """
        x1 = max(self.x, other.x)
        y1 = max(self.y, other.y)
        x2 = min(self.x + self.w, other.x + other.w)
        y2 = min(self.y + self.h, other.y + other.h)
        return (x2 - x1) * (y2 - y1)

    def union(self, other, intersection_area=None):
        """Calculate union area with other bounding box

        Args:
            other (BBox2D): other bounding box object to calculate union
            intersection_area (float): pre-calculated area of intersection

        Returns:
            float of the union area for two bounding boxes
        """
        area_a = self.area
        area_b = other.area
        if not intersection_area:
            intersection_area = self.intersection(other)
        return float(area_a + area_b - intersection_area)

    def iou(self, other):
        """Calculate intersection over union area with other bounding box

        .. math::
                IOU = \\frac{intersection}{union}

        Args:
            other (BBox2D): other bounding box object to calculate iou

        Returns:
            float of the union area for two bounding boxes
        """
        # if boxes don't intersect
        if not self.intersect_with(other):
            return 0
        intersection_area = self.intersection(other)
        union_area = self.union(other, intersection_area=intersection_area)
        # intersection over union
        iou = intersection_area / union_area
        return iou


class BBox3D:
    """
    Class for 3d bounding boxes which can either be predictions or
    ground-truths. This class is the primary representation in this repo of 3d
    bounding boxes and is based off of the Nuscenes style dataset.
    """

    def __init__(
        self,
        translation,
        size,
        label,
        sample_token,
        score=1,
        rotation: Quaternion = Quaternion(),
        velocity=(np.nan, np.nan, np.nan),
    ):
        self.sample_token = sample_token
        self.translation = translation
        self.size = size
        self.width, self.height, self.length = size
        self.rotation = rotation
        self.velocity = velocity
        self.label = label
        self.score = score

    def _local2world_coordinate(self, x):
        """

        Args:
            x: vector describing point (x,y,z) in local coordinates (where the
            center of the box is 0,0,0)

        Returns: the x,y,z coordinates of the input point in global coordinates

        """

        y = np.array(self.translation) + self.rotation.rotate(x)
        return y

    @property
    def back_left_bottom_pt(self):
        """

        Returns: :py:class:`float`: Back-left-bottom point.

        """
        p = np.array([-self.width / 2, -self.height / 2, -self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def front_left_bottom_pt(self):
        """
        :py:class:`float`: Front-left-bottom point.
        """
        p = np.array([-self.width / 2, -self.height / 2, self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def front_right_bottom_pt(self):
        """
        :py:class:`float`: Front-right-bottom point.
        """
        p = np.array([self.width / 2, -self.height / 2, self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def back_right_bottom_pt(self):
        """
        :py:class:`float`: Back-right-bottom point.
        """
        p = np.array([self.width / 2, -self.height / 2, -self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def back_left_top_pt(self):
        """
        :py:class:`float`: Back-left-top point.
        """
        p = np.array([-self.width / 2, self.height / 2, -self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def front_left_top_pt(self):
        """
        :py:class:`float`: Front-left-top point.
        """
        p = np.array([-self.width / 2, self.height / 2, self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def front_right_top_pt(self):
        """
        :py:class:`float`: Front-right-top point.
        """
        p = np.array([self.width / 2, self.height / 2, self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def back_right_top_pt(self):
        """
        :py:class:`float`: Back-right-top point.
        """
        p = np.array([self.width / 2, self.height / 2, -self.length / 2])
        p = self._local2world_coordinate(p)
        return p

    @property
    def p(self) -> np.ndarray:
        """

        Returns: list of all 8 corners of the box beginning with the the bottom
         four corners and then the top
        four corners, both in counterclockwise order (from birds eye view)
        beginning with the back-left corner

        """
        x = np.vstack(
            [
                self.back_left_bottom_pt,
                self.front_left_bottom_pt,
                self.front_right_bottom_pt,
                self.back_right_bottom_pt,
                self.back_left_top_pt,
                self.front_left_top_pt,
                self.front_right_top_pt,
                self.back_right_top_pt,
            ]
        )
        return x
