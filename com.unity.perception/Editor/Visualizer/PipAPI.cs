using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Simulation.Client;
using UnityEngine;

namespace UnityEditor.Perception.Visualizer
{
    static class PipAPI
    {
        static readonly HttpClient k_HttpClient;
        static string s_PypiServer = "https://pypi.org";
        static string s_PackageName = "unity-cv-datasetvisualizer";

        static PipAPI()
        {
            k_HttpClient = new HttpClient();
        }

        static internal async Task<string> GetLatestVersionNumber()
        {
			var requestUri = new Uri($"{s_PypiServer}/pypi/{s_PackageName}/json?Accept=application/json");
			try
            {
            	var httpResponse = await k_HttpClient.GetAsync(requestUri);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = httpResponse.Content.ReadAsStringAsync().Result;
                    dynamic responseJson = JsonConvert.DeserializeObject(responseString);
                    return responseJson.info.version;
                }
                else
                {
                    HandleApiErrors(httpResponse);
                }
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

        static internal int compareVersions(string version1, string version2)
        {
            string[] split1 = version1.Split('.');
            string[] split2 = version2.Split('.');

            if(split1.Length != split2.Length)
            {
                throw new ArgumentException($"Can't compare two versions that do not have the same format: {version1} & {version2}");
            }

            for(int i = 0; i < split1.Length; i++)
            {
                int compare = Int32.Parse(split1[i]) - Int32.Parse(split2[i]);
                if (compare != 0)
                {
                    return compare;
                }
            }
            return 0;            
        }
    }
}
