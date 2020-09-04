using System.IO;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class of scenarios with serializable constants
    /// </summary>
    /// <typeparam name="T">The type of constants to serialize</typeparam>
    public abstract class Scenario<T> : ScenarioBase where T : new()
    {
        /// <summary>
        /// A construct containing serializable constants that control the execution of this scenario
        /// </summary>
        public T constants = new T();

        /// <summary>
        /// Serializes this scenario's constants to a json file in the Unity StreamingAssets folder
        /// </summary>
        public sealed override void Serialize()
        {
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets/");
            using (var writer = new StreamWriter(serializedConstantsFilePath, false))
                writer.Write(JsonUtility.ToJson(constants, true));
        }

        /// <summary>
        /// Deserializes this scenario's constants from a json file in the Unity StreamingAssets folder
        /// </summary>
        /// <exception cref="ScenarioException"></exception>
        public sealed override void Deserialize()
        {
            if (!File.Exists(serializedConstantsFilePath))
                throw new ScenarioException($"JSON scenario constants file does not exist at path {serializedConstantsFilePath}");
            var jsonText = File.ReadAllText(serializedConstantsFilePath);
            constants = JsonUtility.FromJson<T>(jsonText);
        }
    }
}
