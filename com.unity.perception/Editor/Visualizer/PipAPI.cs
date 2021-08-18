#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace UnityEditor.Perception.Visualizer
{
    static class PipAPI
    {
        static readonly HttpClient k_HttpClient;
        const string k_PypiServer = "https://pypi.org";
        const string k_PackageName = "unity-cv-datasetvisualizer";

        static PipAPI()
        {
            k_HttpClient = new HttpClient();
        }

        internal static async Task<string> GetLatestVersionNumber()
        {
            var requestUri = new Uri($"{k_PypiServer}/pypi/{k_PackageName}/json?Accept=application/json");
			try
            {
            	var httpResponse = await k_HttpClient.GetAsync(requestUri);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = httpResponse.Content.ReadAsStringAsync().Result;
                    dynamic responseJson = JsonConvert.DeserializeObject(responseString);
                    return responseJson.info.version;
                }

                HandleApiErrors(httpResponse);
            }
            catch (HttpRequestException e)
            {
                Debug.LogException(e);
            }
			return null;
        }

        static void HandleApiErrors(HttpResponseMessage responseMessage)
        {
            Debug.LogError("A request to PyPI.org did not successfully complete: " + responseMessage.ReasonPhrase);
        }

        internal static int CompareVersions(string version1, string version2)
        {
            var split1 = version1.Split('.');
            var split2 = version2.Split('.');

            int i;
            for(i = 0; i < Math.Min(split1.Length, split2.Length); i++)
            {
                var compare = Int32.Parse(split1[i]) - Int32.Parse(split2[i]);
                if (compare != 0)
                {
                    return compare;
                }
            }

            if (i < split1.Length)
                return 1;

            if (i < split2.Length)
                return -1;

            return 0;
        }
    }
}
#endif
