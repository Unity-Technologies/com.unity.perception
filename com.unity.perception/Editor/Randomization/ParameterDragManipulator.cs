using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    public class ParameterDragManipulator : MouseManipulator
    {
        bool m_Active;
        float m_Offset;
        ParameterElement m_ParameterElement;
        VisualElement m_DragHandle;
        VisualElement m_DragBar;
        VisualElement m_ParameterContainer;

        protected override void RegisterCallbacksOnTarget()
        {
            m_DragHandle = target.Q<VisualElement>("drag-handle");
            m_ParameterElement = (ParameterElement)target;
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

            if (m_ParameterElement.ConfigEditor.FilterString != string.Empty)
                return;

            m_ParameterContainer = target.parent;
            m_DragBar = new ParameterDragBar();
            m_DragBar.style.width = new StyleLength(m_ParameterContainer.resolvedStyle.width);
            target.parent.Add(m_DragBar);

            m_Offset = m_DragHandle.worldBound.position.y - m_ParameterContainer.worldBound.position.y;
            m_DragBar.style.top = evt.localMousePosition.y + m_Offset;

            m_Active = true;
            m_DragHandle.CaptureMouse();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Active || !m_DragHandle.HasMouseCapture())
                return;

            m_DragBar.style.top = evt.localMousePosition.y + m_Offset;

            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Active || !m_DragHandle.HasMouseCapture() || !CanStopManipulation(evt))
                return;

            var dragBarY = evt.localMousePosition.y + m_Offset;
            m_DragBar.RemoveFromHierarchy();

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

            for (var i = 0; i < middlePoints.Length; i++)
            {
                if (dragBarY < middlePoints[i])
                {
                    ReorderParameter(m_ParameterElement.ParameterIndex, i);
                    return;
                }
            }
            ReorderParameter(m_ParameterElement.ParameterIndex, middlePoints.Length);
        }

        void ReorderParameter(int currentIndex, int nextIndex)
        {
            m_ParameterElement.ConfigEditor.ReorderParameter(currentIndex, nextIndex);
        }
    }
}
