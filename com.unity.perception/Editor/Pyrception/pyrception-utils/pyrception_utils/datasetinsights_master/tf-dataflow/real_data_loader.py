#!/usr/bin/python3
# https://github.com/tensorflow/tpu/blob/master/tools/datasets/jpeg_to_tf_record.py
# https://www.tensorflow.org/tutorials/load_data/tfrecord

import _thread
import glob
import json
import logging
import os
import random
import sys

import tensorflow.compat.v1 as tf

tf.disable_eager_execution()

LOG = logging.getLogger()
logging.basicConfig(level=logging.DEBUG)
LOG.addHandler(logging.StreamHandler())


_TRAIN_VAL_RATIO = 0.8
_RECORD_SIZE_MAX_MB = 10.0
_BYTES_IN_MB = 1000000.0
_data_base_path = os.path.join("/disk", "qi")
_run_id = "downscale_real_data"
_data_path = os.path.join(_data_base_path, _run_id)
_out_path = os.path.join(_data_base_path, "downscale_real_tfrecord")
_concurrency = 10


def main():
    # Load annotations.json file and parse annotations
    annotation_files = glob.glob(
        os.path.join(_data_path, "**", "annotations.json"), recursive=True
    )
    LOG.info("Found {} annotation files.".format(len(annotation_files)))
    annotations = {}
    for afile in annotation_files:
        with open(afile, "r") as fp:
            annotation_json = json.load(fp)
            # NOTE: Assumes annotation records are for unique files - will
            # overwrite if there are multiple records for the same filename
            annotations.update(
                {
                    os.path.join(
                        os.path.dirname(afile), "images", capture["file_name"]
                    ): capture["annotations"]
                    for capture in annotation_json
                }
            )
    LOG.info("Loaded {} annotations".format(len(annotations)))

    # Load rgb images and match each image with its annotations
    # Images missing annotations and annotations without matching images
    # will be removed
    rgb_imgs = set(
        glob.glob(os.path.join(_data_path, "**", "IMG*.JPG"), recursive=True)
    )
    LOG.info("Found {} rgb images.".format(len(rgb_imgs)))
    rgb_annotations = set().union(annotations.keys())
    if rgb_imgs.symmetric_difference(rgb_annotations):
        if rgb_imgs.difference(rgb_annotations):
            LOG.error(
                "Images missing annotations: "
                + str(rgb_imgs.difference(rgb_annotations))
            )
            for key in rgb_imgs.difference(rgb_annotations):
                rgb_imgs.remove(key)
        if rgb_annotations.difference(rgb_imgs):
            LOG.error(
                "Annotations missing images: "
                + str(rgb_annotations.difference(rgb_imgs))
            )
            for key in rgb_annotations.difference(rgb_imgs):
                annotations.pop(key)

    if len(rgb_imgs) == 0:
        LOG.error("No images found!")
        return

    # Broke images into batches and write TFRecord multi-threading
    rgb_image_batches = []
    batch_size = int(len(rgb_imgs) / _concurrency)
    for i in range(_concurrency):
        rgb_image_batches.append(set(random.sample(rgb_imgs, batch_size)))
        rgb_imgs -= rgb_image_batches[i]
        LOG.info(
            "Starting thread {} with {} images.".format(
                i, len(rgb_image_batches[i])
            )
        )
        _thread.start_new_thread(
            _write_records_batch, (rgb_image_batches[i], annotations, i)
        )

    while 1:
        pass


def _write_records_batch(images, annotations, batch):
    train = set(random.sample(images, int(len(images) * _TRAIN_VAL_RATIO)))
    val = images - train

    training_record_name = "training_{:06d}"
    training_record_name += "_batch_" + str(batch) + ".tfrecord"
    _write_records(train, annotations, training_record_name, batch)

    validation_record_name = "validation_{:06d}"
    validation_record_name += "_batch_" + str(batch) + ".tfrecord"
    _write_records(val, annotations, validation_record_name, batch)

    LOG.info("Batch {} completed successfully.".format(batch))


def _write_records(images, annotations, naming_convention, batch):
    data_written_mb = 0.0
    record_num = 0
    writer = tf.io.TFRecordWriter(
        os.path.join(_out_path, naming_convention.format(record_num))
    )
    for img in images:
        example = convert_to_example(img, annotations[img])
        writer.write(example)
        data_written_mb += sys.getsizeof(example) / _BYTES_IN_MB
        if data_written_mb > _RECORD_SIZE_MAX_MB:
            writer.close()
            record_num += 1
            writer = tf.io.TFRecordWriter(
                os.path.join(_out_path, naming_convention.format(record_num))
            )
            data_written_mb = 0
    writer.close()


def _bytes_feature(value):
    """Returns a bytes_list from a string / byte."""
    if isinstance(value, type(tf.constant(0))):
        value = (
            value.numpy()
        )  # BytesList won't unpack a string from an EagerTensor.
    return tf.train.Feature(bytes_list=tf.train.BytesList(value=[value]))


def _float_feature(value):
    return _float_list_feature([value])


def _float_list_feature(value):
    """Returns a float_list from a float / double."""
    return tf.train.Feature(float_list=tf.train.FloatList(value=value))


def _int64_feature(value):
    """Returns an int64_list from a bool / enum / int / uint."""
    return _int64_list_feature([value])


def _int64_list_feature(value):
    return tf.train.Feature(int64_list=tf.train.Int64List(value=value))


def _convert_to_example(filename, image_buffer, height, width, labels, bboxes):
    colorspace = b"RGB"
    channels = 3
    image_format = b"JPG"
    xmins, xmaxes, ymins, ymaxes = bboxes

    example = tf.train.Example(
        features=tf.train.Features(
            feature={
                "image/height": _int64_feature(height),
                "image/width": _int64_feature(width),
                "image/colorspace": _bytes_feature(colorspace),
                "image/channels": _int64_feature(channels),
                "image/object/class/label": _int64_list_feature(
                    labels
                ),  # model expects 1-based
                "image/object/bbox/xmin": _float_list_feature(xmins),
                "image/object/bbox/xmax": _float_list_feature(xmaxes),
                "image/object/bbox/ymin": _float_list_feature(ymins),
                "image/object/bbox/ymax": _float_list_feature(ymaxes),
                "image/format": _bytes_feature(image_format),
                "image/filename": _bytes_feature(
                    bytes(os.path.basename(filename), "utf-8")
                ),
                "image/encoded": _bytes_feature(image_buffer),
            }
        )
    )
    return example


def _get_image_data(filename, coder):
    # Read the image file.
    with tf.gfile.GFile(filename, "rb") as ifp:
        image_data = ifp.read()

    image = coder.decode_jpeg(image_data)

    # Check that image converted to RGB
    assert len(image.shape) == 3
    height = image.shape[0]
    width = image.shape[1]
    assert image.shape[2] == 3

    return image_data, height, width


def _annotations_to_bb_normalized(values, width, height):
    x_mins = [v["bbox"][0] / 5184.0 for v in values]
    x_maxes = [x + v["bbox"][2] / 5184.0 for x, v in zip(x_mins, values)]
    y_mins = [v["bbox"][1] / 3456.0 for v in values]
    y_maxes = [y + v["bbox"][3] / 3456.0 for y, v in zip(y_mins, values)]
    return x_mins, x_maxes, y_mins, y_maxes


def convert_to_example(filename, annotations):
    """Given an image and its annotation, convert into TF example."""
    # ignore labels not in categories list
    coder = ImageCoder()
    image_buffer, height, width = _get_image_data(filename, coder)
    del coder

    # NOTE: Can break if non-bounding box annotation types are on
    # TODO: Check annotations.json to find id of bbox annotations
    labels = [v["label_id"] for v in annotations]
    bboxes = _annotations_to_bb_normalized(annotations, width, height)
    example = _convert_to_example(
        filename, image_buffer, height, width, labels, bboxes
    )
    return example.SerializeToString()


class ImageCoder:
    """Helper class that provides TensorFlow image coding utilities."""

    def __init__(self):
        # Create a single Session to run all image coding calls.
        self._sess = tf.Session()

        # Initializes function that decodes RGB JPEG data.
        self._decode_img_data = tf.placeholder(dtype=tf.string)
        self._decode_jpeg = tf.image.decode_jpeg(
            self._decode_img_data, channels=3
        )

    def decode_jpeg(self, image_data):
        image = self._sess.run(
            self._decode_jpeg, feed_dict={self._decode_img_data: image_data}
        )
        assert len(image.shape) == 3
        assert image.shape[2] == 3
        return image

    """ def decode_png(self, image_data):
        image = self._sess.run(
            self._decode_png, feed_dict={self._decode_img_data: image_data}
        )
        assert len(image.shape) == 3
        assert image.shape[2] == 3
        return image """

    def __del__(self):
        self._sess.close()


if __name__ == "__main__":
    main()
