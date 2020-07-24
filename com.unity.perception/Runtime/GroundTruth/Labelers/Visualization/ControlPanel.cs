using System.Collections;
using System.Collections.Generic;
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
        Dictionary<string, GameObject> controlMap = new Dictionary<string, GameObject>();

        /// <summary>
        /// Adds a new UI control to the control panel. If the panel already contains an elment with the
        /// passed in name, the UI element will not be added and the method will return false. Also, all
        /// UI elements must have a LayoutElement component and if they do not, this method will reject
        /// the new element, and return false.
        /// </summary>
        public bool AddNewControl(string name, GameObject uiControl)
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

            if (controlMap.ContainsKey(name))
            {
                Debug.LogWarning("A control with the name: " + name + " has already been registered with the control panel.");
                return false;
            }

            controlMap[name] = uiControl;
            uiControl.transform.SetParent(this.transform, false);

            return true;
        }

        /// <summary>
        /// Removes the component with the passed in name from the control panel. Returns
        /// false if the element does not exist. Returns true on a successful removal.
        /// </summary>
        public bool RemoveControl(string name)
        {
            if (!controlMap.ContainsKey(name))
            {
                Debug.LogWarning(name + " does not exist in control panel, cannot remove.");
                return false;
            }

            var control = controlMap[name];
            controlMap.Remove(name);
            control.transform.SetParent(null);
            GameObject.Destroy(control.gameObject);

            return true;
        }

        /// <summary>
        /// Creates a new toogle control with passed in name. The passed in listener will be
        /// called on toggle clicks. If anything goes wrong this method will return null.
        /// Returns the control panel elemet upon a succssful add.
        /// </summary>
        public GameObject AddToggleControl(string name, UnityAction<bool> listener)
        {
            if (controlMap.ContainsKey(name))
            {
                Debug.LogWarning("A control with the name: " + name + " has already been registered with the control panel.");
                return null;
            }

            if (listener == null)
            {
                Debug.LogWarning("Adding toggle to control panel without a listener, nothing will respond to user's clicks");
            }

            var toggle = GameObject.Instantiate(Resources.Load<GameObject>("GenericToggle"));
            toggle.transform.SetParent(this.transform, false);
            toggle.GetComponentInChildren<Text>().text = name;
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(listener);

            controlMap[name] = toggle;

            return toggle;
        }

        /// <summary>
        /// Creates a new slider control with the passed in name and default value. The passed in listener will be
        /// called on slider changes. If anything goes wrong this method will return null.
        /// Returns the control panel elemet upon a succssful add.
        /// </summary>
        public GameObject AddSliderControl(string name, float defaultValue, UnityAction<float> listener)
        {
            if (controlMap.ContainsKey(name))
            {
                Debug.LogWarning("A control with the name: " + name + " has already been registered with the control panel.");
                return null;
            }

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

            controlMap[name] = gameObject;

            return gameObject;
        }
    }
}
