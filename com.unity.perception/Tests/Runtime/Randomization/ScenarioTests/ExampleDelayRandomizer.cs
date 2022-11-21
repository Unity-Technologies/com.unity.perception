using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

namespace RandomizationTests.ScenarioTests
{
    /// <summary>
    /// Delays the scenario every Nth iteration where N is given by <see cref="m_IterationDelay" />.
    /// Does not delay the very first iteration i.e. iteration 0.
    /// </summary>
    /// <remarks>
    /// With <see cref="m_IterationDelay" /> set to 2, the iterations 2, 4, 6, ..., etc. will be delayed once.
    /// </remarks>
    [Serializable]
    [AddRandomizerMenu("")]
    public class ExampleDelayRandomizer : Randomizer
    {
        int m_IterationDelay = 2;
        bool m_DelayedThisIteration = false;

        public ExampleDelayRandomizer(int iterationDelay = 2)
        {
            m_IterationDelay = Math.Max(2, iterationDelay);
        }

        protected override void OnIterationStart()
        {
            if (m_DelayedThisIteration)
            {
                m_DelayedThisIteration = false;
                return;
            }

            var currentIteration = scenario.currentIteration;
            if (currentIteration > 0 && ((currentIteration) % m_IterationDelay) == 0)
            {
                Debug.Log($"Delaying iteration {currentIteration} once.");
                m_DelayedThisIteration = true;
                scenario.DelayIteration();
            }
        }
    }
}
