using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    struct SoloDefinitionHolder : IMessageProducer
    {
        string m_Label;
        Dictionary<string, DataModelElement> m_Defs;

        public SoloDefinitionHolder(string label)
        {
            m_Label = label;
            m_Defs = new Dictionary<string, DataModelElement>();
        }

        public void AddDefinition(DataModelElement def)
        {
            m_Defs[def.id] = def;
        }

        public void ToMessage(IMessageBuilder builder)
        {
            foreach (var def in  m_Defs.Values)
            {
                var nested = builder.AddNestedMessageToVector(m_Label);
                def.ToMessage(nested);
            }
        }
    }
}
