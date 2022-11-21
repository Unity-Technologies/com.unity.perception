using System;
using UnityEngine;

namespace UnityEngine.Perception.Settings
{
    [Serializable]
    public class AccumulationSettings
    {
        [Tooltip("Number of frames used to accumulate a converged image")]
        [Range(3, 16383)]
        public int accumulationSamples;

        [Tooltip("Controls the amount of motion blur. A value of 0 corresponds to no motion blur and a value of 1 corresponds to maximum motion blur. This only applies to motion caused by physics and animations, or Time.deltaTime based movement.")]
        [Range(0.0f, 1.0f)]
        public float shutterInterval;

        [Tooltip("The time during shutter interval when the shutter is fully open")]
        [Range(0.0f, 1.0f)]
        public float shutterFullyOpen;

        [Tooltip("The time during shutter interval when the shutter begins closing")]
        [Range(0.0f, 1.0f)]
        public float shutterBeginsClosing;

        [Tooltip("Controls whether the Fixed Length Scenario in the Scene (if any) should automatically adapt its number of frames per iteration to account for the number of frames (Accumulation Samples) set here.")]
        public bool adaptFixedLengthScenarioFrames;
    }
}
