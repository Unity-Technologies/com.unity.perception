/*
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Curriculum
{
    public class GridCurriculum : CurriculumBase
    {
        public int samplesPerCell = 1;
        public ExecutionRange executionRange;
        public List<GridSampler> gridSamplers;
        public List<RandomSamplerBase> randomSamplers;

        int m_SampleIndex;
        int m_GlobalIterationIndex;
        int m_TotalIterationCount;

        public override string Type => "grid";
        public int GlobalIterationIndex => m_GlobalIterationIndex;
        public override bool FinishedIterating => GlobalIterationIndex >= m_TotalIterationCount;

        public override void Initialize()
        {
            m_TotalIterationCount = samplesPerCell;
            foreach (var sampler in gridSamplers)
                m_TotalIterationCount *= sampler.binCount;
        }

        public override void Iterate()
        {
            if (gridSamplers.Count == 0)
                return;

            m_SampleIndex++;

            if (m_SampleIndex >= samplesPerCell)
            {
                m_SampleIndex = 0;
                var samplerIndex = gridSamplers.Count - 1;
                gridSamplers[samplerIndex].iterationIndex++;
                while (samplerIndex > 0 && gridSamplers[samplerIndex].iterationIndex == gridSamplers[samplerIndex].binCount)
                {
                    gridSamplers[samplerIndex].iterationIndex = 0;
                    gridSamplers[--samplerIndex].iterationIndex++;
                }
            }

            m_GlobalIterationIndex++;
            foreach (var randomSampler in randomSamplers)
                randomSampler.iterationIndex = m_GlobalIterationIndex;
        }

        public SamplerBase GetSampler(string parameterName)
        {
            foreach (var sampler in gridSamplers)
            {
                if (sampler.parameter.name == parameterName)
                    return sampler;
            }
            foreach (var sampler in randomSamplers)
            {
                if (sampler.parameter.name == parameterName)
                    return sampler;
            }
            throw new ParameterConfigurationException($"A sampler applied to a parameter with the name " +
                $"\"{parameterName}\" was not found");
        }
    }
}
*/
