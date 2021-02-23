using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Scenarios;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Derive Randomizer to implement systems that randomize GameObjects and/or simulation properties.
    /// </summary>
    /// <remark>
    /// Known issue:
    /// https://issuetracker.unity3d.com/issues/serializereference-non-serialized-initialized-fields-lose-their-values-when-entering-play-mode
    /// </remark>
    [Serializable]
    public abstract class Randomizer
    {
        [SerializeField, HideInInspector] bool m_Enabled = true;
        [SerializeField, HideInInspector] internal bool collapsed;

        /// <summary>
        /// Enabled Randomizers are updated, disabled Randomizers are not.
        /// </summary>
        public bool enabled
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                if (value)
                    OnEnable();
                else
                    OnDisable();
            }
        }

        /// <summary>
        /// Returns the scenario containing this Randomizer
        /// </summary>
        public ScenarioBase scenario => ScenarioBase.activeScenario;

        /// <summary>
        /// Retrieves the RandomizerTagManager of the scenario containing this Randomizer
        /// </summary>
        public RandomizerTagManager tagManager => RandomizerTagManager.singleton;

        internal IEnumerable<Parameter> parameters
        {
            get
            {
                var fields = GetType().GetFields();
                foreach (var field in fields)
                {
                    if (!field.IsPublic || !field.FieldType.IsSubclassOf(typeof(Parameter)))
                        continue;
                    var parameter = (Parameter)field.GetValue(this);
                    if (parameter == null)
                    {
                        parameter = (Parameter)Activator.CreateInstance(field.FieldType);
                        field.SetValue(this, parameter);
                    }
                    yield return parameter;
                }
            }
        }

        /// <summary>
        /// OnCreate is called when the Randomizer is added or loaded to a scenario
        /// </summary>
        [Obsolete("Method OnCreate has been deprecated. Use OnAwake instead (UnityUpgradable)", true)]
        protected virtual void OnCreate() =>
            throw new NotSupportedException("OnCreate method has been deprecated");

        /// <summary>
        /// OnAwake is called when the Randomizer is added or loaded to a scenario
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// OnEnabled is called when the Randomizer becomes enabled and active
        /// </summary>
        protected virtual void OnEnable() { }

        /// <summary>
        /// OnDisable is called when the Randomizer becomes disabled
        /// </summary>
        protected virtual void OnDisable() { }

        /// <summary>
        /// OnScenarioStart is called on the frame the scenario begins iterating
        /// </summary>
        protected virtual void OnScenarioStart() { }

        /// <summary>
        /// OnScenarioComplete is called the after the entire Scenario has completed
        /// </summary>
        protected virtual void OnScenarioComplete() { }

        /// <summary>
        /// OnIterationStart is called at the start of a new Scenario iteration
        /// </summary>
        protected virtual void OnIterationStart() { }

        /// <summary>
        /// OnIterationEnd is called the after a Scenario iteration has completed
        /// </summary>
        protected virtual void OnIterationEnd() { }

        /// <summary>
        /// OnStartRunning is called on the first frame a Randomizer is enabled
        /// </summary>
        [Obsolete("Method OnStartRunning has been deprecated. Use OnEnabled instead (UnityUpgradable)", true)]
        protected virtual void OnStartRunning() =>
            throw new NotSupportedException("OnStartRunning method has been deprecated");

        /// <summary>
        /// OnStartRunning is called on the first frame a disabled Randomizer is updated
        /// </summary>
        [Obsolete("Method OnStopRunning has been deprecated. Use OnDisable instead (UnityUpgradable)", true)]
        protected virtual void OnStopRunning() =>
            throw new NotSupportedException("OnStopRunning method has been deprecated");

        /// <summary>
        /// OnUpdate is executed every frame for enabled Randomizers
        /// </summary>
        protected virtual void OnUpdate() { }

        #region InternalScenarioMethods
        internal void Awake() => OnAwake();

        internal void ScenarioStart() => OnScenarioStart();

        internal void ScenarioComplete() => OnScenarioComplete();

        internal void IterationStart() => OnIterationStart();

        internal void IterationEnd() => OnIterationEnd();

        internal void Update() => OnUpdate();
        #endregion
    }
}
