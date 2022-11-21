using System;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    class DragToReorderManipulator : MouseManipulator
    {
        bool m_Active;
        VisualElement m_DragHandle;
        float m_Offset;
        VisualElement m_ParameterContainer;
        RandomizerElement m_RandomizerElement;
        VisualElement m_ReorderingIndicator;

        protected override void RegisterCallbacksOnTarget()
        {
            m_RandomizerElement = (RandomizerElement)target;
            m_DragHandle = m_RandomizerElement.Q<VisualElement>("drag-handle");
            m_DragHandle.RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_DragHandle.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            m_DragHandle.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            m_DragHandle.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            m_DragHandle.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            m_DragHandle.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            m_ParameterContainer = target.parent;
            m_ReorderingIndicator = new RandomizerReorderingIndicator();
            m_ReorderingIndicator.style.width = new StyleLength(m_ParameterContainer.resolvedStyle.width);
            target.parent.Add(m_ReorderingIndicator);

            m_Offset = m_DragHandle.worldBound.position.y - m_ParameterContainer.worldBound.position.y;
            m_ReorderingIndicator.style.top = evt.localMousePosition.y + m_Offset;

            m_Active = true;
            m_DragHandle.CaptureMouse();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Active || !m_DragHandle.HasMouseCapture())
                return;

            m_ReorderingIndicator.style.top = evt.localMousePosition.y + m_Offset;

            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Active || !m_DragHandle.HasMouseCapture() || !CanStopManipulation(evt))
                return;

            var dragBarY = evt.localMousePosition.y + m_Offset;
            m_ReorderingIndicator.RemoveFromHierarchy();

            m_Active = false;
            m_DragHandle.ReleaseMouse();
            evt.StopPropagation();

            var p = 0;
            var middlePoints = new float[m_ParameterContainer.childCount];
            foreach (var parameterElement in m_ParameterContainer.Children())
            {
                var middleHeight = parameterElement.worldBound.height / 2;
                var localY = parameterElement.worldBound.y - m_ParameterContainer.worldBound.position.y;
                middlePoints[p++] = middleHeight + localY;
            }

            var randomizerIndex = m_RandomizerElement.parent.IndexOf(m_RandomizerElement);
            for (var i = 0; i < middlePoints.Length; i++)
                if (dragBarY < middlePoints[i])
                {
                    ReorderParameter(randomizerIndex, i);
                    return;
                }

            ReorderParameter(randomizerIndex, middlePoints.Length);
        }

        void ReorderParameter(int currentIndex, int nextIndex)
        {
            m_RandomizerElement.randomizerList.ReorderRandomizer(currentIndex, nextIndex);
        }
    }
}
