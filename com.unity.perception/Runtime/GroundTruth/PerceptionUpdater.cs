namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// PerceptionUpdater is automatically spawned when the player starts and is used to coordinate and maintain
    /// static perception lifecycle behaviours.
    /// </summary>
    class PerceptionUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var updaterObject = new GameObject("PerceptionUpdater");
            updaterObject.AddComponent<PerceptionUpdater>();
            updaterObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(updaterObject);
        }

        void LateUpdate()
        {
            LabeledObjectsManager.singleton.RegisterPendingLabels();
            DatasetCapture.SimulationState?.Update();
        }
    }
}
