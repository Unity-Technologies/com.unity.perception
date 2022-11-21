using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Data structure to hold Tensor values.
    /// </summary>
    public struct Tensor
    {
        /// <summary>
        /// Creates a tensor of passed in type and shape. It will
        /// allocated the memory for the byte buffer, but all bytes will be 0.
        /// </summary>
        /// <param name="elementType">The element type of the tensor</param>
        /// <param name="shape">The shape of the tensor</param>
        public Tensor(ElementType elementType, int[] shape)
        {
            this.elementType = elementType;
            this.shape = shape;
            count = 1;
            size = 0;

            foreach (var t in shape)
                count *= t;

            size = CalculateSize(this.elementType, count);

            buffer = new byte[size];
        }

        static int CalculateSize(ElementType elementType, int count)
        {
            switch (elementType)
            {
                case ElementType.Byte:
                    return count * sizeof(byte);
                case ElementType.Char:
                    return count * sizeof(char);
                case ElementType.Bool:
                    return count * sizeof(bool);
                case ElementType.Int:
                    return count * sizeof(int);
                case ElementType.Uint:
                    return count * sizeof(uint);
                case ElementType.Float:
                    return count * sizeof(float);
                case ElementType.Double:
                    return count * sizeof(double);
                case ElementType.Long:
                    return count * sizeof(long);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Creates a tensor of the passed in type, shape, and with the passed in buffer
        /// </summary>
        /// <param name="elementType">The type of tensor</param>
        /// <param name="shape">The shape of the tensor</param>
        /// <param name="buffer">The byte buffer</param>
        public Tensor(ElementType elementType, int[] shape, byte[] buffer)
        {
            this.elementType = elementType;
            this.shape = shape;

            count = 1;
            foreach (var t in shape)
                count *= t;

            size = CalculateSize(this.elementType, count);

            this.buffer = buffer;

            // TODO verify that buffer is the correct size
        }

        /// <summary>
        /// Data type of the tensor value
        /// </summary>
        public enum ElementType
        {
            /// <summary>
            /// Byte element type
            /// </summary>
            Byte,
            /// <summary>
            /// Char element type
            /// </summary>
            Char,
            /// <summary>
            /// Bool element type
            /// </summary>
            Bool,
            /// <summary>
            /// Int element type
            /// </summary>
            Int,
            /// <summary>
            /// Uint element type
            /// </summary>
            Uint,
            /// <summary>
            /// Float element type
            /// </summary>
            Float,
            /// <summary>
            /// Double element type
            /// </summary>
            Double,
            /// <summary>
            /// Long element type
            /// </summary>
            Long
        }

        /// <summary>
        /// The shape of the Tensor. Each value is its length in a dimension. For example a single dimension array
        /// of 7 values would have a shape of new [] { 7 }. A three dimension array of width 5, height 6, and depth 8,
        /// would have a shape of new [] {5, 6, 8}
        /// </summary>
        public int[] shape { get; private set; }

        /// <summary>
        /// The total number of elements stored in the tensor. This value is equal to
        /// the product of all sizes.
        /// </summary>
        public int count { get; private set; }

        /// <summary>
        /// The total memory size of the tensor buffer. This value is equal to the product of <see cref="count"/>
        /// multiplied by the memory size of the element type.
        /// </summary>
        public int size { get; private set; }

        /// <summary>
        /// The element type of the tensor. The tensor class only supports tensors of the enumerated element types.
        /// </summary>
        public ElementType elementType { get; private set; }

        /// <summary>
        /// The raw buffer of the tensor. The buffer is the size of shape * element byte size.
        /// </summary>
        public byte[] buffer { get; private set; }

        int GetArrayIndex(int[] index)
        {
            var result = 0;
            var runningMultiplier = 1;

            for (var i = shape.Length - 1; i >= 0; i--)
            {
                result += index[i] * runningMultiplier;
                runningMultiplier *= shape[i];
            }

            return result;
        }

        bool ValidType(Type requestedType)
        {
            switch (elementType)
            {
                case ElementType.Byte:
                    return requestedType == typeof(byte);
                case ElementType.Char:
                    return requestedType == typeof(char);
                case ElementType.Bool:
                    return requestedType == typeof(bool);
                case ElementType.Int:
                    return requestedType == typeof(int);
                case ElementType.Uint:
                    return requestedType == typeof(uint);
                case ElementType.Float:
                    return requestedType == typeof(float);
                case ElementType.Double:
                    return requestedType == typeof(double);
                case ElementType.Long:
                    return requestedType == typeof(long);
                default:
                    return false;
            }
        }

        bool ValidIndex(int[] index)
        {
            if (index.Length != shape.Length) return false;

            for (var i = 0; i < index.Length; i++)
            {
                if (index[i] < 0 || index[i] >= shape[i]) return false;
            }

            return true;
        }

        #region Get Element

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out byte value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = buffer[calIndex];
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out bool value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToBoolean(buffer, calIndex * sizeof(bool));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out int value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToInt32(buffer, calIndex * sizeof(int));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out uint value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToUInt32(buffer, calIndex * sizeof(uint));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out char value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToChar(buffer, calIndex * sizeof(char));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out float value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToSingle(buffer, calIndex * sizeof(float));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out double value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToDouble(buffer, calIndex * sizeof(double));
        }

        /// <summary>
        /// Retrieve the casted element at the index in the tensor.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The retrieved value</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// requested element is the wrong type for the tensor</exception>
        public void GetElementAt(int[] index, out long value)
        {
            value = default;
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            value = BitConverter.ToInt64(buffer, calIndex * sizeof(long));
        }

        #endregion

        #region Set Element

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, byte value)
        {
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            buffer[calIndex] = value;
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, bool value)
        {
            const int memSize = sizeof(bool);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, int value)
        {
            const int memSize = sizeof(int);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, uint value)
        {
            const int memSize = sizeof(uint);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, char value)
        {
            const int memSize = sizeof(char);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, float value)
        {
            const int memSize = sizeof(float);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, double value)
        {
            const int memSize = sizeof(double);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        /// <summary>
        /// Sets the value int the tensor to the passed in value.
        /// </summary>
        /// <param name="index">The index of the element. The index array needs to be the size of the shape,
        /// with each element in the range of the shape's dimension</param>
        /// <param name="value">The value to set</param>
        /// <exception cref="ArgumentException">Thrown if either the index is not the proper size or the
        /// passed in element is the wrong type for the tensor</exception>
        public void SetElementAt(int[] index, long value)
        {
            const int memSize = sizeof(long);
            if (!ValidIndex(index)) throw new ArgumentException($"Passed in an invalid index: {index}");
            if (!ValidType(value.GetType())) throw new ArgumentException($"Requested the wrong data type");
            var calIndex = GetArrayIndex(index);
            var tmp = BitConverter.GetBytes(value);
            Buffer.BlockCopy(tmp, 0, buffer, calIndex * memSize, memSize);
        }

        #endregion
    }

    /// <summary>
    /// Utility class to build tensors out of some common data types
    /// </summary>
    public static class TensorBuilder
    {
        /// <summary>
        /// Converts from a tensor into a color32 object
        /// </summary>
        /// <param name="tensor">The tensor to convert</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentException">Thrown if the object does not match the tensor type</exception>
        public static Color32 ToColor32(Tensor tensor)
        {
            const int shape = 4;

            if (tensor.shape.Length != 1 && tensor.shape[0] != shape)
                throw new ArgumentException($"Passed in tensor cannot be converted into a Color32");
            if (tensor.elementType != Tensor.ElementType.Byte)
                throw new ArgumentException("Passed in tensor cannot be converted into a Color32");

            var bArray = new byte[shape];
            Buffer.BlockCopy(tensor.buffer, 0, bArray, 0, shape * sizeof(byte));

            return new Color32(bArray[0], bArray[1], bArray[2], bArray[3]);
        }

        /// <summary>
        /// Converts a Color32 object into a tensor
        /// </summary>
        /// <param name="c">The data to convert into a tensor</param>
        /// <returns>The tensor form of the data</returns>
        public static Tensor ToTensor(Color32 c)
        {
            const int shape = 4;
            var cArray = new[] { c.r, c.g, c.b, c.a };
            var buffer = new byte[shape * sizeof(byte)];
            Buffer.BlockCopy(cArray, 0, buffer, 0, shape * sizeof(byte));

            return new Tensor(Tensor.ElementType.Byte, new[] { shape }, buffer);
        }

        /// <summary>
        /// Converts from a tensor into a Vector3 object
        /// </summary>
        /// <param name="tensor">The tensor to convert</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentException">Thrown if the object does not match the tensor type</exception>
        public static Vector3 ToVector3(Tensor tensor)
        {
            const int shape = 3;

            if (tensor.shape.Length != 1 && tensor.shape[0] != shape)
                throw new ArgumentException("Passed in tensor cannot be converted into a Vector3");
            if (tensor.elementType != Tensor.ElementType.Float)
                throw new ArgumentException("Passed in tensor cannot be converted into a Vector3");

            var fArray = new float[shape];
            Buffer.BlockCopy(tensor.buffer, 0, fArray, 0, shape * sizeof(float));

            return new Vector3(fArray[0], fArray[1], fArray[2]);
        }

        /// <summary>
        /// Converts a Vector3 object into a tensor
        /// </summary>
        /// <param name="v">The data to convert into a tensor</param>
        /// <returns>The tensor form of the data</returns>
        public static Tensor ToTensor(Vector3 v)
        {
            const int shape = 3;
            var fArray = new[] { v.x, v.y, v.z };
            var buffer = new byte[shape * sizeof(float)];
            Buffer.BlockCopy(fArray, 0, buffer, 0, shape * sizeof(float));

            return new Tensor(Tensor.ElementType.Float, new[] { shape }, buffer);
        }

        /// <summary>
        /// Converts from a tensor into a Vector2 object
        /// </summary>
        /// <param name="tensor">The tensor to convert</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentException">Thrown if the object does not match the tensor type</exception>
        public static Vector2 ToVector2(Tensor tensor)
        {
            const int shape = 2;

            if (tensor.shape.Length != 1 && tensor.shape[0] != shape)
                throw new ArgumentException("Passed in tensor cannot be converted into a Vector2");
            if (tensor.elementType != Tensor.ElementType.Float)
                throw new ArgumentException("Passed in tensor cannot be converted into a Vector2");

            var fArray = new float[shape];
            Buffer.BlockCopy(tensor.buffer, 0, fArray, 0, shape * sizeof(float));

            return new Vector2(fArray[0], fArray[1]);
        }

        /// <summary>
        /// Converts a Vector2 object into a tensor
        /// </summary>
        /// <param name="v">The data to convert into a tensor</param>
        /// <returns>The tensor form of the data</returns>
        public static Tensor ToTensor(Vector2 v)
        {
            const int shape = 2;
            var fArray = new[] { v.x, v.y };
            var buffer = new byte[shape * sizeof(float)];
            Buffer.BlockCopy(fArray, 0, buffer, 0, shape * sizeof(float));

            return new Tensor(Tensor.ElementType.Float, new[] { shape }, buffer);
        }

        /// <summary>
        /// Converts from a tensor into a float3x3 object
        /// </summary>
        /// <param name="tensor">The tensor to convert</param>
        /// <returns>The converted object</returns>
        /// <exception cref="ArgumentException">Thrown if the object does not match the tensor type</exception>
        public static float3x3 ToFloat3X3(Tensor tensor)
        {
            if (tensor.shape.Length != 2 && tensor.shape[0] != 3 && tensor.shape[1] != 3)
                throw new ArgumentException("Passed in tensor cannot be converted into a float3x3");
            if (tensor.elementType != Tensor.ElementType.Float)
                throw new ArgumentException("Passed in tensor cannot be converted into a float3x3");

            var fArray = new float[9];
            Buffer.BlockCopy(tensor.buffer, 0, fArray, 0, 9 * sizeof(float));

            var i = 0;
            var matrix = new float3x3();
            matrix.c0.x = fArray[i++];
            matrix.c0.y = fArray[i++];
            matrix.c0.z = fArray[i++];

            matrix.c1.x = fArray[i++];
            matrix.c1.y = fArray[i++];
            matrix.c1.z = fArray[i++];

            matrix.c2.x = fArray[i++];
            matrix.c2.y = fArray[i++];
            matrix.c2.z = fArray[i];

            return matrix;
        }

        /// <summary>
        /// Converts a float3x3 matrix into a tensor
        /// </summary>
        /// <param name="m">The matrix to convert into a tensor</param>
        /// <returns>The tensor form of the matrix</returns>
        public static Tensor ToTensor(float3x3 m)
        {
            var shape = new[] { 3, 3 };
            const int count = 9;
            var fArray = new[]
            {
                m.c0.x, m.c0.y, m.c0.z,
                m.c1.x, m.c1.y, m.c1.z,
                m.c2.x, m.c2.y, m.c2.z
            };
            var buffer = new byte[count * sizeof(float)];
            Buffer.BlockCopy(fArray, 0, buffer, 0, count * sizeof(float));

            return new Tensor(Tensor.ElementType.Float, shape, buffer);
        }
    }

    /// <summary>
    /// Helper class that converts from common data types into message builder types.
    /// </summary>
    public static class MessageBuilderUtils
    {
        /// <summary>
        /// Converts a color32 into an int vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static int[] ToIntVector(Color32 inData)
        {
            return new[] { inData.r, inData.g, inData.b, (int)inData.a };
        }

        /// <summary>
        /// Converts a Vector2 into an int vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static float[] ToFloatVector(Vector2 inData)
        {
            return new[] { inData.x, inData.y };
        }

        /// <summary>
        /// Converts a Vector3 into an int vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static float[] ToFloatVector(Vector3 inData)
        {
            return new[] { inData.x, inData.y, inData.z };
        }

        /// <summary>
        /// Converts a Quaternion into a float vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static float[] ToFloatVector(Quaternion inData)
        {
            return new[] { inData.x, inData.y, inData.z, inData.w };
        }

        /// <summary>
        /// Converts a float3 into an int vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static float[] ToFloatVector(float3 inData)
        {
            return new[] { inData.x, inData.y, inData.z };
        }

        /// <summary>
        /// Converts a float3x3 into an int vector
        /// </summary>
        /// <param name="inData">The data to convert</param>
        /// <returns>The converted data type</returns>
        public static float[][] ToFloatVector2(float3x3 inData)
        {
            return new[]
            {
                ToFloatVector(inData.c0),
                ToFloatVector(inData.c1),
                ToFloatVector(inData.c2)
            };
        }
    }

    /// <summary>
    /// Creates messageQueue immediately in memory. Later it can be converted to any message with IMessageBuilder
    /// </summary>
    public class InMemoryMessageBuilder : IMessageBuilder, IMessageProducer
    {
        class EncodedImage
        {
            internal EncodedImage(string ext, byte[] value)
            {
                Ext = ext;
                Value = value;
            }

            internal readonly string Ext;
            internal readonly byte[] Value;
        }

        readonly List<(string, object)> kvps = new();

        public void AddByte(string key, byte value)
        {
            kvps.Add((key, value));
        }

        public void AddChar(string key, char value)
        {
            kvps.Add((key, value));
        }

        public void AddInt(string key, int value)
        {
            kvps.Add((key, value));
        }

        public void AddUInt(string key, uint value)
        {
            kvps.Add((key, value));
        }

        public void AddLong(string key, long value)
        {
            kvps.Add((key, value));
        }

        public void AddFloat(string key, float value)
        {
            kvps.Add((key, value));
        }

        public void AddDouble(string key, double value)
        {
            kvps.Add((key, value));
        }

        public void AddString(string key, string value)
        {
            kvps.Add((key, value));
        }

        public void AddBool(string key, bool value)
        {
            kvps.Add((key, value));
        }

        public void AddEncodedImage(string key, string extension, byte[] value)
        {
            kvps.Add((key, new EncodedImage(extension, value)));
        }

        public void AddByteArray(string key, IEnumerable<byte> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddIntArray(string key, IEnumerable<int> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddUIntArray(string key, IEnumerable<uint> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddLongArray(string key, IEnumerable<long> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddFloatArray(string key, IEnumerable<float> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddDoubleArray(string key, IEnumerable<double> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddStringArray(string key, IEnumerable<string> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddBoolArray(string key, IEnumerable<bool> value)
        {
            kvps.Add((key, value.ToArray()));
        }

        public void AddTensor(string key, Tensor tensor)
        {
            kvps.Add((key, tensor));
        }

        public IMessageBuilder AddNestedMessage(string key)
        {
            var message = new InMemoryMessageBuilder();
            kvps.Add((key, message));
            return message;
        }

        public IMessageBuilder AddNestedMessageToVector(string arrayKey)
        {
            var message = new InMemoryMessageBuilder();
            kvps.Add((arrayKey, new List<InMemoryMessageBuilder> { message }));
            return message;
        }

        public void ToMessage(IMessageBuilder builder)
        {
            foreach (var kvp in kvps)
            {
                switch (kvp.Item2)
                {
                    case byte b:
                        builder.AddByte(kvp.Item1, b);
                        break;
                    case char c:
                        builder.AddChar(kvp.Item1, c);
                        break;
                    case int i:
                        builder.AddInt(kvp.Item1, i);
                        break;
                    case uint i:
                        builder.AddUInt(kvp.Item1, i);
                        break;
                    case long l:
                        builder.AddLong(kvp.Item1, l);
                        break;
                    case float f:
                        builder.AddFloat(kvp.Item1, f);
                        break;
                    case double d:
                        builder.AddDouble(kvp.Item1, d);
                        break;
                    case string s:
                        builder.AddString(kvp.Item1, s);
                        break;
                    case bool b:
                        builder.AddBool(kvp.Item1, b);
                        break;
                    case EncodedImage encoded:
                        builder.AddEncodedImage(kvp.Item1, encoded.Ext, encoded.Value);
                        break;
                    case IEnumerable<int> ints:
                        builder.AddIntArray(kvp.Item1, ints);
                        break;
                    case IEnumerable<uint> uints:
                        builder.AddUIntArray(kvp.Item1, uints);
                        break;
                    case IEnumerable<long> longs:
                        builder.AddLongArray(kvp.Item1, longs);
                        break;
                    case IEnumerable<double> ds:
                        builder.AddDoubleArray(kvp.Item1, ds);
                        break;
                    case IEnumerable<float> fs:
                        builder.AddFloatArray(kvp.Item1, fs);
                        break;
                    case IEnumerable<string> strings:
                        builder.AddStringArray(kvp.Item1, strings);
                        break;
                    case IEnumerable<bool> bs:
                        builder.AddBoolArray(kvp.Item1, bs);
                        break;
                    case Tensor t:
                        builder.AddTensor(kvp.Item1, t);
                        break;
                    case InMemoryMessageBuilder m:
                        var messageBuilderChild = builder.AddNestedMessage(kvp.Item1);
                        m.ToMessage(messageBuilderChild);
                        break;
                    case List<InMemoryMessageBuilder> m:
                        var nestedMessageToVector = builder.AddNestedMessageToVector(kvp.Item1);
                        m[0].ToMessage(nestedMessageToVector);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Interface for a message builder class. A message builder is used to convert data
    /// inherited from <see cref="IMessageProducer"/> and convert it to a message.
    /// </summary>
    public interface IMessageBuilder
    {
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddByte(string key, byte value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddChar(string key, char value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddInt(string key, int value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddUInt(string key, uint value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddLong(string key, long value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddFloat(string key, float value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddDouble(string key, double value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddString(string key, string value);
        /// <summary>
        /// Add a key/value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        void AddBool(string key, bool value);
        /// <summary>
        /// Add an encoded byte array image to the message as a key/value pair.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="extension">Image extension for the image type, for example a PNG image would be "png"</param>
        /// <param name="value">The array of values to add</param>
        void AddEncodedImage(string key, string extension, byte[] value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        /// <remarks>This method is obsolete, using <see cref="AddEncodedImage"/> instead</remarks>
        [Obsolete("AddByteArray has been deprecated, Use AddEncodedImage instead", true)]
        void AddByteArray(string key, IEnumerable<byte> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddIntArray(string key, IEnumerable<int> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddUIntArray(string key, IEnumerable<uint> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddLongArray(string key, IEnumerable<long> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddFloatArray(string key, IEnumerable<float> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddDoubleArray(string key, IEnumerable<double> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddStringArray(string key, IEnumerable<string> value);

        /// <summary>
        /// Add a key/value array pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="value">The array of values to add</param>
        void AddBoolArray(string key, IEnumerable<bool> value);
        /// <summary>
        /// Add a key/tensor value pair to the message.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="tensor">The tensor value to add</param>
        void AddTensor(string key, Tensor tensor);
        /// <summary>
        /// Adds a nested message to the message. A nested message is a new nested hierarchy inside
        /// of the message. This method returns a new builder set to write at the new level.
        /// </summary>
        /// <param name="key">The key of the new nested message element</param>
        /// <returns>The builder used to build the nested messages</returns>
        IMessageBuilder AddNestedMessage(string key);
        /// <summary>
        /// Retrieves the builder for a nested message that takes in more than one value.
        /// </summary>
        /// <param name="arrayKey">The key of the nested element</param>
        /// <returns>The builder of the nested elment</returns>
        IMessageBuilder AddNestedMessageToVector(string arrayKey);
    }
}
