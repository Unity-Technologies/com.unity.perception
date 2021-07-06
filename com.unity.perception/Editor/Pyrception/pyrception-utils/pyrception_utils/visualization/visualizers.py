import pathlib
from typing import Dict, List

import numpy as np
import PIL
import streamlit as st
from PIL import ImageFont
from PIL.Image import Image
from PIL.ImageDraw import ImageDraw
from pyquaternion import Quaternion
from visualization.bbox import BBox3D
from visualization.bbox3d_plot import add_single_bbox3d_on_image

def draw_image_with_boxes(
    image: Image,
    classes: Dict,
    labels: List,
    boxes: List[List],
    colors: Dict,
):
    """
    Draws an image in streamlit with labels and bounding boxes.

    :param image: the PIL image
    :type PIL:
    :param classes: the class dictionary
    :type Dict:
    :param labels: list of integer object labels for the frame
    :type List:
    :param boxes: List of bounding boxes (as a List of coordinates) for the frame
    :type List[List]:
    :param colors: class colors
    :type Dict:
    :param header: Image header
    :type str:
    :param description: Image description
    :type str:
    """
    image = image.copy()
    image_draw = ImageDraw(image)
    # draw bounding boxes
    path_to_font = pathlib.Path(__file__).parent.parent.absolute()
    font = ImageFont.truetype(f"{path_to_font}/NairiNormal-m509.ttf", 15)

    for label, box in zip(labels, boxes):
        label = label - 1
        class_name = classes[label]
        image_draw.rectangle(box, outline=colors[class_name], width=2)
        image_draw.text(
            (box[0], box[1]), class_name, font=font, fill=colors[class_name]
        )

    return image


def draw_image_with_segmentation(
    image: Image,
    segmentation: Image,
):
    """
    Draws an image in streamlit with labels and bounding boxes.

    :param image: the PIL image
    :type PIL:
    :param height: height of the image
    :type int:
    :param width: width of the image
    :type int:
    :param segmentation: Segmentation Image
    :type PIL:
    :param header: Image header
    :type str:
    :param description: Image description
    :type str:
    """
    # image_draw = ImageDraw(segmentation)
    rgba = np.array(segmentation.copy().convert("RGBA"))
    r, g, b, a = rgba.T
    black_areas = (r == 0) & (b == 0) & (g == 0) & (a == 255)
    other_areas = (r != 0) | (b != 0) | (g != 0)
    rgba[..., 0:4][black_areas.T] = (0, 0, 0, 0)
    rgba[..., -1][other_areas.T] = int(0.6 * 255)

    foreground = PIL.Image.fromarray(rgba)
    image = image.copy()
    image.paste(foreground, (0, 0), foreground)
    return image


def draw_image_with_keypoints(
    image: Image,
    keypoints,
    dataset,
):
    image = image.copy()
    image_draw = ImageDraw(image)
    radius = int(dataset.metadata.image_size[0] * 5/500)
    for i in range(len(keypoints)):
        keypoint = keypoints[i]
        if keypoint["state"] != 2:
            continue
        coordinates = (keypoint["x"]-radius, keypoint["y"]-radius, keypoint["x"]+radius, keypoint["y"]+radius)
        color = dataset.metadata.annotations[dataset.ann_to_index['keypoints']]["spec"][0]["key_points"][i]["color"]
        image_draw.ellipse(coordinates, fill=(int(255*color["r"]), int(255*color["g"]), int(255*color["b"]), 255))

    skeleton = dataset.metadata.annotations[dataset.ann_to_index['keypoints']]["spec"][0]["skeleton"]
    for bone in skeleton:
        if keypoints[bone["joint1"]]["state"] != 2 or keypoints[bone["joint1"]]["state"] != 2:
            continue
        joint1 = (keypoints[bone["joint1"]]["x"], keypoints[bone["joint1"]]["y"])
        joint2 = (keypoints[bone["joint2"]]["x"], keypoints[bone["joint2"]]["y"])
        r = bone["color"]["r"]
        g = bone["color"]["g"]
        b = bone["color"]["b"]
        image_draw.line([joint1, joint2], fill=(int(255*r), int(255*g), int(255*b), 255), width=int(dataset.metadata.image_size[0] * 3/500))
    return image


def plot_bboxes3d(image, bboxes, projection, color, orthographic):
    """ Plot an image with 3D bounding boxes

    Currently this method should only be used for ground truth images, and
    doesn't support predictions. If a list of colors is not provided as an
    argument to this routine, the default color of green will be used.

    Args:
        image (PIL Image): a PIL image.
        bboxes (list): a list of BBox3D objects
        projection: The perspective projection of the camera which
        captured the ground truth.
        colors (list): a color list for boxes. Defaults to none. If
        colors = None, it will default to coloring all boxes green.

    Returns:
        PIL image: a PIL image with bounding boxes drawn on it.
    """
    np_image = np.array(image)
    img_height, img_width, _ = np_image.shape

    for i, box in enumerate(bboxes):
        add_single_bbox3d_on_image(np_image, box, projection, color, orthographic=orthographic)

    return PIL.Image.fromarray(np_image)


def read_bounding_box_3d(bounding_boxes_metadata):
    bboxes = []

    for b in bounding_boxes_metadata:
        label_id = b['label_id']
        translation = (b["translation"]["x"],b["translation"]["y"],b["translation"]["z"])
        size = (b["size"]["x"], b["size"]["y"], b["size"]["z"])
        rotation = b["rotation"]
        rotation = Quaternion(
            x=rotation["x"], y=rotation["y"], z=rotation["z"], w=rotation["w"]
        )

        #if label_mappings and label_id not in label_mappings:
        #    continue
        box = BBox3D(
            translation=translation,
            size=size,
            label=label_id,
            sample_token=0,
            score=1,
            rotation=rotation,
        )
        bboxes.append(box)

    return bboxes


def draw_image_with_box_3d(image, sensor, values, colors):
    #TODO: IMPLEMENT COLORS
    if 'camera_intrinsic' in sensor:
        projection = np.array(sensor["camera_intrinsic"])
    else:
        projection = np.array([[1,0,0],[0,1,0],[0,0,1]])

    boxes = read_bounding_box_3d(values)
    img_with_boxes = plot_bboxes3d(image, boxes, projection, None, orthographic=(sensor["projection"] == "orthographic"))
    return img_with_boxes
