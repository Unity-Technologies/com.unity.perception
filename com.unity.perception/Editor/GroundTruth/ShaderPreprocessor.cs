using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Perception.GroundTruth
{
    public class ShaderPreprocessor : IPreprocessShaders
    {
        private string[] shadersToPreprocess = new[]
        {
            "Perception/KeypointDepthCheck"
        };
        public int callbackOrder => 0;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!shadersToPreprocess.Contains(shader.name))
                return;

            var hdrpKeyword = new ShaderKeyword(shader, "HDRP_ENABLED");
#if HDRP_PRESENT || URP_PRESENT

#if HDRP_PRESENT
            bool isHdrp = true;
#else
            bool isHdrp = false;
#endif
            for (var i = data.Count - 1; i >= 0; --i)
            {
                if (data[i].shaderKeywordSet.IsEnabled(hdrpKeyword) == isHdrp)
                    continue;

                data.RemoveAt(i);
            }
#endif
        }
    }
}
