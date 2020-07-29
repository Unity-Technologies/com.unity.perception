using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The control panel for labeler visualizers. This panel resides in the upper right hand corner of
    /// the display. Any UI element can be added to the panel, but it contains helper methods to quickly
    /// create some of the most common control panel display elements: toggles and sliders.
    /// </summary>
    public class ControlPanel : MonoBehaviour
    {
        HashSet<GameObject> m_Controls = new HashSet<GameObject>();

        /// <summary>
        ///  Retrieves a list of the current controls in the control panel.
        /// </summary>
        public List<GameObject> controls
        {
            get
            {
                return m_Controls.ToList<GameObject>();
            }
        }

        /// <summary>
        /// Adds a new UI control to the control panel. If the control cannot be added and the method will
        /// return false. Also, all UI elements must have a LayoutElement component and if they do not, 
        /// this method will reject the new element, and return false.
        /// </summary>
        public bool AddNewControl(GameObject uiControl)
        {
            if (uiControl.GetComponent<RectTransform>() == null)
            {
                Debug.LogError("Control panel UI control must have a rect transform component.");
                return false;
            }

            if (uiControl.GetComponent<LayoutElement>() == null)
            {
                Debug.LogError("Control panel UI control must contain a layout element component.");
                return false;
            }

            if (m_Controls.Add(uiControl))
            {
                uiControl.transform.SetParent(this.transform, false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the passed in component from the control panel. Returns
        /// false if the element does not exist. Returns true on a successful removal.
        /// The caller is responsible for destroying the control.
        /// </summary>
        public bool RemoveControl(GameObject uiControl)
        {
            if (m_Controls.Remove(uiControl))
            {
                uiControl.transform.SetParent(null);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new toogle control with passed in name. The passed in listener will be
        /// called on toggle clicks. If anything goes wrong this method will return null.
        /// Returns the control panel elemet upon a succssful add.
        /// </summary>
        public GameObject AddToggleControl(string name, UnityAction<bool> listener)
        {
            if (listener == null)
            {
                Debug.LogWarning("Adding toggle to control panel without a listener, nothing will respond to user's clicks");
            }

            var toggle = GameObject.Instantiate(Resources.Load<GameObject>("GenericToggle"));
            toggle.transform.SetParent(this.transform, false);
            toggle.GetComponentInChildren<Text>().text = name;
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(listener);

            m_Controls.Add(toggle);

            return toggle;
        }

        /// <summary>
        /// Creates a new slider control with the passed in name and default value. The passed in listener will be
        /// called on slider changes. If anything goes wrong this method will return null.
        /// Returns the control panel elemet upon a succssful add.
        /// </summary>
        public GameObject AddSliderControl(string name, float defaultValue, UnityAction<float> listener)
        {
            if (listener == null)
            {
                Debug.LogWarning("Adding slider to control panel without a listener, nothing will respond to user's interactions");
            }

            var gameObject = GameObject.Instantiate(Resources.Load<GameObject>("GenericSlider"));
            gameObject.transform.SetParent(this.transform, false);
            gameObject.GetComponentInChildren<Text>().text = name;
            var slider = gameObject.GetComponentInChildren<Slider>();
            slider.value = defaultValue;
            slider.onValueChanged.AddListener(listener);

            m_Controls.Add(gameObject);

            return gameObject;
        }
    }
}
