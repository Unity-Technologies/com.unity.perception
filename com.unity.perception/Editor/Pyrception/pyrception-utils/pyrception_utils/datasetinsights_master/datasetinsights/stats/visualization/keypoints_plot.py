""" Helper keypoints library to plot keypoint joints and skeletons  with a
simple Python API.
"""


def _get_color_from_color_node(color):
    """ Gets the color from the color node in the template.

    Args:
        color (tuple): The color's channel values expressed in a range from 0..1

    Returns: The color for the node.

    """
    r = int(color["r"] * 255)
    g = int(color["g"] * 255)
    b = int(color["b"] * 255)
    a = int(color["a"] * 255)
    return r, g, b, a


def _get_color_for_bone(bone):
    """ Gets the color for the bone from the template. A bone is a visual
        connection between two keypoints in the keypoint list of the figure.

        bone
        {
            joint1: <int> Index into the keypoint list for the first joint.
            joint2: <int> Index into the keypoint list for the second joint.
            color {
                r: <float> Value (0..1) of the red channel.
                g: <float> Value (0..1) of the green channel.
                b: <float> Value (0..1) of the blue channel.
                a: <float> Value (0..1) of the alpha channel.
            }
        }

    Args:
        bone: The active bone.

    Returns: The color of the bone.

    """
    if "color" in bone:
        return _get_color_from_color_node(bone["color"])
    else:
        return 255, 0, 255, 255


def _get_color_for_keypoint(template, keypoint):
    """ Gets the color for the keypoint from the template. A keypoint is a
        location of interest inside of a figure. Keypoints are connected
        together with bones. The configuration of keypoint locations and bone
        connections are defined in a template file.

    keypoint_template {
        template_id: <str> The UUID of the template.
        template_name: <str> Human readable name of the template.
        key_points [ <List> List of joints defined in this template
            {
                label: <str> The label of the joint.
                index: <int> The index of the joint.
                color {
                    r: <float> Value (0..1) for the red channel.
                    g: <float> Value (0..1) for the green channel.
                    b: <float> Value (0..1) for the blue channel.
                    a: <float> Value (0..1) for the alpha channel.
                }
            }, ...
        ]
        skeleton [ <List> List of skeletal connections
            {
                joint1: <int> The first joint of the connection.
                joint2: <int> The second joint of the connection.
                color {
                    r: <float> Value (0..1) for the red channel.
                    g: <float> Value (0..1) for the green channel.
                    b: <float> Value (0..1) for the blue channel.
                    a: <float> Value (0..1) for the alpha channel.
                }
            }, ...
        ]
    }

    Args:
        template: The active template.
        keypoint: The active keypoint.

    Returns: The color for the keypoint.

    """
    node = template["key_points"][keypoint["index"]]

    if "color" in node:
        return _get_color_from_color_node(node["color"])
    else:
        return 0, 0, 255, 255


def draw_keypoints_for_figure(image, figure, draw, templates, visual_width=6):
    """ Draws keypoints for a figure on an image.

    keypoints {
        label_id: <int> Integer identifier of the label.
        instance_id: <str> UUID of the instance.
        template_guid: <str> UUID of the keypoint template.
        pose: <str> String label for current pose.
        keypoints [
            {
                index: <int> Index of keypoint in template.
                x: <float> X subpixel coordinate of keypoint.
                y: <float> Y subpixel coordinate of keypoint
                state: <int> 0: keypoint does not exist,
                             1: keypoint exists but is not visible,
                             2: keypoint exists and is visible.
            }, ...
        ]
    }

    Args:
        image (PIL Image): a PIL image.
        figure: The figure to draw.
        draw (PIL ImageDraw): PIL image draw interface.
        templates (list): a list of keypoint templates.
        visual_width (int): the visual width of the joints.

    Returns: a PIL image with keypoints for a figure drawn on it.

    """
    # find the template for this
    for template in templates:
        if template["template_id"] == figure["template_guid"]:
            break
    else:
        return image

    # load the spec
    skeleton = template["skeleton"]

    for bone in skeleton:
        j1 = figure["keypoints"][bone["joint1"]]
        j2 = figure["keypoints"][bone["joint2"]]

        if j1["state"] == 2 and j2["state"] == 2:
            x1 = int(j1["x"])
            y1 = int(j1["y"])
            x2 = int(j2["x"])
            y2 = int(j2["y"])

            color = _get_color_for_bone(bone)
            draw.line((x1, y1, x2, y2), fill=color, width=visual_width)

    for k in figure["keypoints"]:
        state = k["state"]
        if state == 2:
            x = k["x"]
            y = k["y"]

            color = _get_color_for_keypoint(template, k)

            half_width = visual_width / 2

            draw.ellipse(
                (
                    x - half_width,
                    y - half_width,
                    x + half_width,
                    y + half_width,
                ),
                fill=color,
                outline=color,
            )

    return image
