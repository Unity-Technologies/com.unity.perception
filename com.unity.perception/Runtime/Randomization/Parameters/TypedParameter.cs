using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class TypedParameter<T> : Parameter
    {
        public sealed override Type OutputType => typeof(T);

        public abstract T Sample(int seedOffset);

        public abstract T[] Samples(int seedOffset, int totalSamples);

        public sealed override void Apply(int seedOffset)
        {
            if (!hasTarget)
                return;
            var value = Sample(seedOffset);
            var componentType = target.component.GetType();
            switch (target.fieldOrProperty)
            {
                case FieldOrProperty.Field:
                    var fieldInfo = componentType.GetField(target.propertyName);
                    fieldInfo.SetValue(target.component, value);
                    break;
                case FieldOrProperty.Property:
                    var propertyInfo = componentType.GetProperty(target.propertyName);
                    propertyInfo.SetValue(target.component, value);
                    break;
            }
        }

        public override void Validate()
        {
            base.Validate();
            foreach (var sampler in Samplers)
                sampler.Validate();
        }
    }
}
