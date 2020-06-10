using System;
// using System.Reflection;
// using System.Reflection.Emit;
using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Parameters.Abstractions
{
    public abstract class Parameter<T> : ParameterBase
    {
        Action<Component, T> m_ApplyParameterDelegate;

        public override Type SamplerType()
        {
            return typeof(Sampler<T>);
        }

        public override Type SampleType()
        {
            return typeof(T);
        }

        protected override void SetupFieldOrPropertySetters()
        {
            // if (!hasTarget)
            //     return;
            // var componentType = propertyTarget.targetComponent.GetType();
            // switch (propertyTarget.targetKind)
            // {
            //     case TargetKind.Field:
            //         var fieldInfo = componentType.GetField(propertyTarget.propertyName);
            //         m_ApplyParameterDelegate = CreateFieldSetter(fieldInfo, componentType);
            //         break;
            //     case TargetKind.Property:
            //         var propertyInfo = componentType.GetProperty(propertyTarget.propertyName);
            //         m_ApplyParameterDelegate = CreatePropertySetter(propertyInfo, componentType);
            //         break;
            // }
        }

        public override void Apply(IterationData data)
        {
            iterationData = data;
            UnreflectiveApply();

            // if (!hasTarget)
            //     return;
            // var value = ((Sampler<T>)sampler).NextSample();
            // m_ApplyParameterDelegate(propertyTarget.targetComponent, value);
        }

        void UnreflectiveApply()
        {
            if (!hasTarget)
                return;
            var value = ((Sampler<T>)sampler).NextSample();
            var componentType = propertyTarget.targetComponent.GetType();
            switch (propertyTarget.targetKind)
            {
                case TargetKind.Field:
                    var fieldInfo = componentType.GetField(propertyTarget.propertyName);
                    fieldInfo.SetValue(propertyTarget.targetComponent, value);
                    break;
                case TargetKind.Property:
                    var propertyInfo = componentType.GetProperty(propertyTarget.propertyName);
                    propertyInfo.SetValue(propertyTarget.targetComponent, value);
                    break;
            }
        }

        // static Action<Component, T> CreateFieldSetter(FieldInfo field, Type componentType)
        // {
        //     var methodName = field.ReflectedType.FullName + ".set_" + field.Name;
        //     var setterMethod = new DynamicMethod(
        //         methodName, null, new []{ typeof(Component), typeof(T) }, true);
        //     var gen = setterMethod.GetILGenerator();
        //     gen.Emit(OpCodes.Ldarg_0);
        //     gen.Emit(OpCodes.Castclass, componentType);
        //     gen.Emit(OpCodes.Ldarg_1);
        //     gen.Emit(OpCodes.Stfld, field);
        //     gen.Emit(OpCodes.Ret);
        //     var newDelegate = (Action<Component, T>)setterMethod.CreateDelegate(typeof(Action<Component, T>));
        //     return newDelegate;
        // }
        //
        // static Action<Component, T> CreatePropertySetter(PropertyInfo property, Type componentType)
        // {
        //     var methodName = property.ReflectedType.FullName + ".set_" + property.Name;
        //     var setMethod = property.GetSetMethod();
        //     var setterMethod = new DynamicMethod(
        //         methodName, null, new []{ typeof(Component), typeof(T) }, true);
        //     var gen = setterMethod.GetILGenerator();
        //     gen.Emit(OpCodes.Ldarg_0);
        //     gen.Emit(OpCodes.Castclass, componentType);
        //     gen.Emit(OpCodes.Ldarg_1);
        //     gen.Emit(OpCodes.Callvirt, setMethod);
        //     gen.Emit(OpCodes.Ret);
        //     var newDelegate = (Action<Component, T>)setterMethod.CreateDelegate(typeof(Action<Component, T>));
        //     return newDelegate;
        // }
    }
}
