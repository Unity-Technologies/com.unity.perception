using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The visualization canvas. This canvas should contain all labeler visualization components.
    /// All visualizlation components should be added to this canvas via teh AddComponent method.
    /// </summary>
    public class VisualizationCanvas : MonoBehaviour
    {
        /// <summary>
        /// The control panel contains the UI control elements used to interact with the labelers.
        /// </summary>
        public ControlPanel controlPanel;
        /// <summary>
        /// The HUD panel displays realtime key/value pair data on a UI panel.
        /// </summary>
        public HUDPanel hudPanel;
        /// <summary>
        /// Game object which acts as the scene container for all of the dynamic labeler visuals
        /// </summary>
        public GameObject dynaicContentHolder;

        // Start is called before the first frame update
        void Start()
        {
            if (GameObject.Find("EventSystem") == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// Adds a new UI components to the visualization canvas. Pass in fullscreen if the component should take up
        /// the entire screen. Pass in setAsLowestElement if the component should be rendered behind all other components.
        /// This method will return false if the element could not be added, true if everything works properly.
        /// <param name="component">UI component that should be added to this UI canvas</param>
        /// <param name="fullScreen">Should this component's rect transform be set to fill the entire dimensions of the parent, defaults to true</param>
        /// <param name="setAsLowestElement">Should this UI component be rendered as the lowest UI component in the scene, defaults to false</param>
        /// <returns>True if the component was added properly, false if an error occurred.</returns>
        /// </summary>
        public bool AddComponent(GameObject component, bool fullScreen = true, bool setAsLowestElement = false)
        {
            if (component == null)
            {
                Debug.LogError("Trying to add a null component to VisualizationCanvas");
                return false;
            }

            RectTransform trans = component.GetComponent<RectTransform>();
            if (trans == null)
            {
                Debug.LogWarning("Adding UI element without a rect transform, adding one to it");
                trans = component.AddComponent<RectTransform>();
            }

            if (fullScreen)
            {
                trans.anchorMin = new Vector2(0, 0);
                trans.anchorMax = new Vector2(1, 1);
                trans.pivot = new Vector2(0.5f, 0.5f);

                trans.offsetMax = new Vector2(0, 0);
                trans.offsetMin = new Vector2(0, 0);
            }

            trans.SetParent(dynaicContentHolder.transform, false);

            if (setAsLowestElement) component.transform.SetAsFirstSibling();

            return true;
        }
    }
}
