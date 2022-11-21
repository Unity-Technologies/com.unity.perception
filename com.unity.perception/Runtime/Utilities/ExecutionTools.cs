using System;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Utilities
{
    [MovedFrom("UnityEngine.Perception.Tools")]
    static class ExecutionTools
    {
        internal static bool GetCommandLineArgumentValue(string key, out string value)
        {
            value = null;
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (!string.Equals(arguments[i], key) || i > arguments.Length - 1)
                {
                    continue;
                }

                value = arguments[i + 1];
                return true;
            }

            return false;
        }

        internal static bool HasCommandLineArgumentValue(string key)
        {
            return Environment.GetCommandLineArgs().Contains(key);
        }
    }
}
