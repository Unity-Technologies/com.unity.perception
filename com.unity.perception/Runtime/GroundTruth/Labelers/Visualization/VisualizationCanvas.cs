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
        public ControlPanel controlPanel;
        public HUDPanel hudPanel;
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
