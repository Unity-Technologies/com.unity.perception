using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class SceneRandomizerInfo : MonoBehaviour
{
    Text m_TextMesh;
    ScenarioBase m_Scenario;

    void Start()
    {
        m_TextMesh = GetComponent<Text>();
        m_Scenario = ScenarioBase.activeScenario;
        if (m_Scenario == null)
        {
            Debug.LogError("An active scenario is not present in the scene.");
            enabled = false;
        }
    }

    void Update()
    {
        var sceneCount = SceneManager.sceneCount;
        var mostRecentLoadedScene = SceneManager.GetSceneAt(sceneCount - 1);
        m_TextMesh.text = $"Iteration: {m_Scenario.currentIteration}\nScene: {mostRecentLoadedScene.name}";
    }
}
