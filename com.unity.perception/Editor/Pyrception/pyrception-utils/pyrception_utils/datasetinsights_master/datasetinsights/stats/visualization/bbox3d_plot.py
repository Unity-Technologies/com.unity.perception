""" Helper bounding box 3d library to plot pretty 3D boundign
boxes with a simple Python API.
"""

import cv2
import numpy


def _add_single_bbox3d_on_image(
    image,
    front_bottom_left,
    front_upper_left,
    front_upper_right,
    front_bottom_right,
    back_bottom_left,
    back_upper_left,
    back_upper_right,
    back_bottom_right,
    color=None,
    box_line_width=2,
):
    """ Add a single 3D bounding box to the passed in image.

    For this version of the method, all of the passed in coordinates should be
    integer tuples already projected in image pixel coordinate space.

    Args:
        image (numpy array): numpy array version of the image
        front_bottom_left (int tuple): Front bottom left coordinate of the 3D
        bounding box in pixel space
        front_upper_left (int tuple): Front upper left coordinate of the 3D
        bounding box in pixel space
        front_upper_right (int tuple): Front upper right coordinate of the 3D
        bounding box in pixel space
        front_bottom_right (int tuple): Front bottom right coordinate of the 3D
        bounding box in pixel space
        back_bottom_left (int tuple): Back bottom left coordinate of the 3D
        bounding box in pixel space
        back_upper_left (int tuple): Back bottom left coordinate of the 3D
        bounding box in pixel space
        back_upper_right (int tuple): Back bottom left coordinate of the 3D
        bounding box in pixel space
        back_bottom_right (int tuple): Back bottom left coordinate of the 3D
        bounding box in pixel space
        color (tuple): RGBA color of the bounding box. Defaults to None. If
        color = None the the tuple of [0, 255, 0, 255] (Green) will be used.
        box_line_width: The width of the drawn box. Defaults to 2.
    """
    try:
        fbl = (front_bottom_left[0], front_bottom_left[1])
        ful = (front_upper_left[0], front_upper_left[1])
        fur = (front_upper_right[0], front_upper_right[1])
        fbr = (front_bottom_right[0], front_bottom_right[1])

        bbl = (back_bottom_left[0], back_bottom_left[1])
        bul = (back_upper_left[0], back_upper_left[1])
        bur = (back_upper_right[0], back_upper_right[1])
        bbr = (back_bottom_right[0], back_bottom_right[1])

    except ValueError:
        raise TypeError("all box coorinates must be a number")

    if color is None:
        color = [0, 255, 0, 255]

    cv2.line(image, fbl, ful, color, box_line_width)  # front left
    cv2.line(image, ful, fur, color, box_line_width)  # front top
    cv2.line(image, fbr, fur, color, box_line_width)  # front right
    cv2.line(image, fbl, fbr, color, box_line_width)  # front bottom

    cv2.line(image, bbl, bul, color, box_line_width)  # back left
    cv2.line(image, bul, bur, color, box_line_width)  # back top
    cv2.line(image, bbr, bur, color, box_line_width)  # back right
    cv2.line(image, bbl, bbr, color, box_line_width)  # back bottom

    cv2.line(image, ful, bul, color, box_line_width)  # top left
    cv2.line(image, fur, bur, color, box_line_width)  # top right
    cv2.line(image, fbl, bbl, color, box_line_width)  # bottom left
    cv2.line(image, fbr, bbr, color, box_line_width)  # bottom right


def add_single_bbox3d_on_image(
    image, box, proj, color=None, box_line_width=2, orthographic=False,
):
    """" Add single 3D bounding box on a given image.

    Args:
        image (numpy array): a numpy array for an image
        box (BBox3D): a 3D bounding box in camera's coordinate system
        proj (numpy 2D array): camera's 3x3 projection matrix
        color(tuple): RGBA color of the bounding box. Defaults to None. If
        color = None the the tuple of [0, 255, 0, 255] (Green) will be used.
        box_line_width (int): line width of the bounding boxes. Defaults to 2.
        orthographic (bool): true if proj is orthographic, else perspective
    """
    img_height, img_width, _ = image.shape

    fll = box.back_left_bottom_pt
    ful = box.back_left_top_pt
    fur = box.back_right_top_pt
    flr = box.back_right_bottom_pt

    bll = box.front_left_bottom_pt
    bul = box.front_left_top_pt
    bur = box.front_right_top_pt
    blr = box.front_right_bottom_pt

    project_pt_to_pixel = (
        _project_pt_to_pixel_location_orthographic
        if orthographic
        else _project_pt_to_pixel_location
    )

    fll_raster = project_pt_to_pixel(fll, proj, img_height, img_width)
    ful_raster = project_pt_to_pixel(ful, proj, img_height, img_width)
    fur_raster = project_pt_to_pixel(fur, proj, img_height, img_width)
    flr_raster = project_pt_to_pixel(flr, proj, img_height, img_width)
    bll_raster = project_pt_to_pixel(bll, proj, img_height, img_width)
    bul_raster = project_pt_to_pixel(bul, proj, img_height, img_width)
    bur_raster = project_pt_to_pixel(bur, proj, img_height, img_width)
    blr_raster = project_pt_to_pixel(blr, proj, img_height, img_width)

    _add_single_bbox3d_on_image(
        image,
        fll_raster,
        ful_raster,
        fur_raster,
        flr_raster,
        bll_raster,
        bul_raster,
        bur_raster,
        blr_raster,
        color,
        box_line_width,
    )


def _project_pt_to_pixel_location(pt, projection, img_height, img_width):
    """ Projects a 3D coordinate into a pixel location from a perspective camera.

    Applies the passed in projection matrix to project a point from the camera's
    coordinate space into pixel space.

    For a description of the math used in this method, see:
    https://www.scratchapixel.com/lessons/3d-basic-rendering/computing-pixel-coordinates-of-3d-point/

    Args:
        pt (numpy array): The 3D point to project.
        projection (numpy 2D array): The camera's 3x3 projection matrix.
        img_height (int): The height of the image in pixels.
        img_width (int): The width of the image in pixels.

    Returns:
        numpy array: a one-dimensional array with two values (x and y)
        representing a point's pixel coordinate in an image.
    """

    _pt = projection.dot(pt)

    # compute the perspective divide. Near clipping plane should take care of
    # divide by zero cases, but we will check to be sure
    if _pt[2] != 0:
        _pt /= _pt[2]

    return numpy.array(
        [
            int(-(_pt[0] * img_width) / 2.0 + (img_width * 0.5)),
            int((_pt[1] * img_height) / 2.0 + (img_height * 0.5)),
        ]
    )


def _project_pt_to_pixel_location_orthographic(
    pt, projection, img_height, img_width
):
    """ Projects a 3D coordinate into a pixel location from an orthographic camera.

        Applies the passed in projection matrix to project a point from the
        camera's coordinate space into pixel space.

        For a description of the math used in this method, see:
        https://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-
        orthographic-projection-matrix/projection-matrix-introduction

        Args:
            pt (numpy array): The 3D point to project.
            projection (numpy 2D array): The camera's 3x3 projection matrix.
            img_height (int): The height of the image in pixels.
            img_width (int): The width of the image in pixels.

        Returns:
            numpy array: a one-dimensional array with two values (x and y)
            representing a point's pixel coordinate in an image.
    """

    # The 'y' component needs to be flipped because of how Unity works
    projection = numpy.array(
        [
            [projection[0][0], 0, 0],
            [0, -projection[1][1], 0],
            [0, 0, projection[2][2]],
        ]
    )
    temp = projection.dot(pt)

    pixel = [
        int((temp[0] + 1) * 0.5 * img_width),
        int((temp[1] + 1) * 0.5 * img_height),
    ]
    return pixel
