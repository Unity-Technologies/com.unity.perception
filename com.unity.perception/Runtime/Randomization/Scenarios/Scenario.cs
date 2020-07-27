namespace UnityEngine.Perception.Randomization.Scenarios
{
    public abstract class Scenario<T> : ScenarioBase
    {
        public T constants;

        public override string Serialize()
        {
            return JsonUtility.ToJson(constants, true);
        }

        public override void Deserialize(string json)
        {
            constants = JsonUtility.FromJson<T>(json);
        }
    }
}
