using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Interface that should be defined by a class that wants to be able to provide a image to the overlay
    /// panel.
    /// </summary>
    public interface IOverlayPanelProvider
    {
        /// <summary>
        /// The image to the overlay panel.
        /// </summary>
        Texture overlayImage { get; }

        /// <summary>
        /// The label of the overlay panel.
        /// </summary>
        string label { get; }
    }

    /// <summary>
    /// Some labeler's result in a full screen image per frame. The overlay panel controls which of these labeler's image
    /// is currently shown.
    /// </summary>
    public class OverlayPanel : MonoBehaviour
    {
        internal PerceptionCamera perceptionCamera { get; set; }

        bool m_Enabled;

        GUIStyle m_LabelStyle;
        GUIStyle m_SliderStyle;
        GUIStyle m_SelectorToggleStyle;
        GUIStyle m_WindowStyle;

        float m_SegmentTransparency = 0.8f;
        float m_BackgroundTransparency;

        GameObject m_SegCanvas;
        GameObject m_SegVisual;
        RawImage m_OverlayImage;

        int m_CachedHeight;
        int m_CachedWidth;

        bool m_ShowPopup = false;
        Texture2D m_NormalDropDownTexture;
        Texture2D m_HoverDropDownTexture;

        void SetEnabled(bool isEnabled)
        {
            if (isEnabled == m_Enabled) return;

            m_Enabled = isEnabled;

            m_SegCanvas.SetActive(isEnabled);

            foreach (var p in perceptionCamera.labelers)
            {
                if (p is IOverlayPanelProvider)
                {
                    p.visualizationEnabled = isEnabled;
                }
            }

            // Clear out the handle to the cached overlay texture if we are not isEnabled
            if (!isEnabled)
                m_OverlayImage.texture = null;
        }

        void SetupVisualizationElements()
        {
            m_Enabled = true;

            m_SegmentTransparency = 0.8f;
            m_BackgroundTransparency = 0.0f;

            m_SegVisual = GameObject.Instantiate(Resources.Load<GameObject>("SegmentTexture"));

            m_OverlayImage = m_SegVisual.GetComponent<RawImage>();
            m_OverlayImage.material.SetFloat("_SegmentTransparency", m_SegmentTransparency);
            m_OverlayImage.material.SetFloat("_BackTransparency", m_BackgroundTransparency);

            if (m_SegVisual.transform is RectTransform rt)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
            }

            if (m_SegCanvas == null)
            {
                m_SegCanvas = new GameObject("overlay_canvas");
                m_SegCanvas.AddComponent<RectTransform>();
                var canvas = m_SegCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                m_SegCanvas.AddComponent<CanvasScaler>();

                m_SegVisual.transform.SetParent(m_SegCanvas.transform, false);
            }

            m_LabelStyle = new GUIStyle(GUI.skin.label) {padding = {left = 10}};
            m_SliderStyle = new GUIStyle(GUI.skin.horizontalSlider) {margin = {left = 12}};
            m_SelectorToggleStyle = new GUIStyle(GUI.skin.button);

            if (m_NormalDropDownTexture == null)
            {
                m_NormalDropDownTexture = Resources.Load<Texture2D>("drop_down2");
                m_HoverDropDownTexture = Resources.Load<Texture2D>("drop_down");
            }

            m_SelectorToggleStyle.normal.background = m_NormalDropDownTexture;
            m_SelectorToggleStyle.border = new RectOffset(7, 70, 6, 6);
            m_SelectorToggleStyle.alignment = TextAnchor.MiddleLeft;
            m_SelectorToggleStyle.clipping = TextClipping.Clip;
            m_SelectorToggleStyle.active.background = m_NormalDropDownTexture;
            m_SelectorToggleStyle.hover.background = m_HoverDropDownTexture;
            m_SelectorToggleStyle.focused.background = m_HoverDropDownTexture;

            m_WindowStyle = new GUIStyle(GUI.skin.window);
            var backTexture = Resources.Load<Texture2D>("instance_back");
            m_WindowStyle.normal.background = backTexture;
        }

        IOverlayPanelProvider m_ActiveProvider = null;

        // Make the contents of the window
        void OnSelectorPopup(int windowID)
        {
            foreach (var labeler in perceptionCamera.labelers.Where(l => l is IOverlayPanelProvider &&  l.enabled))
            {
                var panel = labeler as IOverlayPanelProvider;
                if (GUILayout.Button(panel.label))
                {
                    m_ActiveProvider = panel;
                    m_ShowPopup = false;
                }
            }
        }

        internal void OnDrawGUI(float x, float y, float width, float height)
        {
            var any = perceptionCamera.labelers.Any(l => l is IOverlayPanelProvider && l.enabled);

            // If there used to be active providers, but they have been turned off, remove
            // the active provider and return null. If one has come online, then set it to the active
            // provider
            if (!any)
            {
                m_ActiveProvider = null;
            }
            else
            {
                var findNewProvider = m_ActiveProvider == null;

                if (!findNewProvider)
                {
                    if (m_ActiveProvider is CameraLabeler l)
                    {
                        findNewProvider = !l.enabled;
                    }
                }

                if (findNewProvider)
                    m_ActiveProvider= perceptionCamera.labelers.First(l => l is IOverlayPanelProvider && l.enabled) as IOverlayPanelProvider;

            }

            if (m_ActiveProvider == null)
            {
                if (m_SegCanvas != null)
                    m_SegCanvas.SetActive(false);
                return;
            }

            if (m_OverlayImage == null)
            {
                SetupVisualizationElements();
            }

            // If all overlays were offline, but now one has come on line
            // we need to set the canvas back to active
            if (!m_SegCanvas.activeSelf)
                m_SegCanvas.SetActive(true);

            GUILayout.Label("Overlay");
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Enabled");
            GUILayout.FlexibleSpace();
            var isEnabled = GUILayout.Toggle(m_Enabled, "");
            GUILayout.EndHorizontal();

            SetEnabled(isEnabled);
            if (!isEnabled)
            {
                return;
            }

            // Create the overlay button
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            // Truncate the label if it overflows the button size
            var label = m_ActiveProvider?.label ?? "Not Selected";
            var trunc = new StringBuilder(label.Substring(0, Math.Min(label.Length, 10)));
            if (trunc.Length != label.Length)
                trunc.Append("...");
            GUILayout.Label("Overlay: ");
            if (GUILayout.Button(trunc.ToString(), m_SelectorToggleStyle))
            {
                // If bottom is clicked we need to show the popup window
                m_ShowPopup = true;
            }
            GUILayout.EndHorizontal();

            if (m_ShowPopup)
            {
                var windowRect = new Rect(x, y, width, height);
                GUILayout.Window(0, windowRect, OnSelectorPopup, "Choose Overlay", m_WindowStyle);
            }

            // Create the transparency sliders
            GUILayout.Space(4);
            GUILayout.Label("Object Alpha:", m_LabelStyle);
            m_SegmentTransparency = GUILayout.HorizontalSlider(m_SegmentTransparency, 0.0f, 1.0f, m_SliderStyle, GUI.skin.horizontalSliderThumb);
            GUILayout.Space(4);
            GUILayout.Label("Background Alpha:", m_LabelStyle);
            m_BackgroundTransparency = GUILayout.HorizontalSlider(m_BackgroundTransparency, 0.0f, 1.0f, m_SliderStyle, GUI.skin.horizontalSliderThumb);
            GUI.skin.label.padding.left = 0;

            // Grab the overlay image from the active provider
            m_OverlayImage.texture = m_ActiveProvider?.overlayImage;

            var rt = m_SegVisual.transform as RectTransform;
            if (rt != null && m_CachedHeight != Screen.height)
            {
                m_CachedHeight = Screen.height;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_CachedHeight);
            }

            if (rt != null && m_CachedWidth != Screen.width)
            {
                m_CachedWidth = Screen.width;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
            }

            if (!GUI.changed) return;
            m_OverlayImage.material.SetFloat("_SegmentTransparency", m_SegmentTransparency);
            m_OverlayImage.material.SetFloat("_BackTransparency", m_BackgroundTransparency);
        }
    }
}
