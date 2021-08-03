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

#if HDRP_PRESENT || URP_PRESENT

#if HDRP_PRESENT
            var enabled = new ShaderKeyword(shader, "HDRP_ENABLED");
            var disabled = new ShaderKeyword(shader, "URP_ENABLED");
#else
            var enabled = new ShaderKeyword(shader, "URP_ENABLED");
            var disabled = new ShaderKeyword(shader, "HDRP_ENABLED");


#endif
            for (var i = data.Count - 1; i >= 0; --i)
            {
                var skeys = data[i].shaderKeywordSet.GetShaderKeywords();
                foreach (var s in skeys)
                {
                    var txt = ShaderKeyword.GetGlobalKeywordName(s);
                    txt = ShaderKeyword.GetKeywordName(shader, s);
                }

                data[i].shaderKeywordSet.Enable(enabled);
                data[i].shaderKeywordSet.Disable(disabled);
            }
#endif
        }
    }
}
