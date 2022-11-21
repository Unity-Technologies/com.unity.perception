using System;
using System.Linq;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// General metric class used to record simulation metrics. This class can report values of many primitive types
    /// along with any struct/class implementing the <see cref="IMessageProducer"/> interface. For most use cases, using
    /// this class will be suitable for metric reporting.
    /// </summary>
    public class GenericMetric : Metric
    {
        Array m_Values;

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return base.IsValid() && m_Values != null;
        }

        /// <inheritdoc/>
        public override T[] GetValues<T>()
        {
            return m_Values.Cast<T>().ToArray();
        }

        /// <summary>
        /// Creates a new metric containing a boolean value.
        /// </summary>
        /// <param name="value">The boolean metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(bool value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric containing an array of booleans.
        /// </summary>
        /// <param name="values">The boolean metric values</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(bool[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <summary>
        /// Creates a new float array metric from a vector3 value. The array will store the values as [x, y, z] in the array.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(Vector3 value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] {value.x, value.y, value.z };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(float value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="values">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(float[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(int value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="values">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(int[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(string value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="values">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(string[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(uint value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="values">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(uint[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="value">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(IMessageProducer value, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = new[] { value };
        }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="values">The metric value</param>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID, set as default for a metric not associated with a sensor</param>
        /// <param name="annotationId">The annotation ID associated with this metric, set as default for a metric not associated to an annotation</param>
        public GenericMetric(IMessageProducer[] values, MetricDefinition definition, string sensorId = default, string annotationId = default)
            : base(definition, sensorId, annotationId)
        {
            m_Values = values;
        }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            switch (m_Values)
            {
                case bool[] bools:
                    if (bools.Length == 1)
                        builder.AddBool("value", bools[0]);
                    else builder.AddBoolArray("values", bools);
                    break;
                case float[] floats:
                    if (floats.Length == 1)
                        builder.AddFloat("value", floats[0]);
                    else
                        builder.AddFloatArray("values", floats);
                    break;
                case int[] ints:
                    if (ints.Length == 1)
                        builder.AddInt("value", ints[0]);
                    else
                        builder.AddIntArray("values", ints);
                    break;
                case string[] strings:
                    if (strings.Length == 1)
                    {
                        builder.AddString("value", strings[0] ?? string.Empty);
                    }
                    else
                        builder.AddStringArray("values", strings.Select(s => s ?? string.Empty).ToArray());
                    break;
                case uint[] uints:
                    if (uints.Length == 1)
                        builder.AddUInt("value", uints[0]);
                    else
                        builder.AddUIntArray("values", uints);
                    break;
                case IMessageProducer[] mps:
                    foreach (var mp in mps)
                    {
                        var nested = builder.AddNestedMessageToVector("values");
                        mp.ToMessage(nested);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Generic metrics do not support passed in type");
            }
        }
    }
}
