using System;
using System.IO;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    public class CsvWriter
    {
        string m_Delimiter;
        StreamWriter m_File;
        ParameterConfiguration m_Config;
        Parameter[] m_SelectedParameters;

        public CsvWriter(
            string filePath,
            ParameterConfiguration config,
            bool overwriteData,
            Parameter[] selectedParameters = null,
            string[] additionalHeaders = null,
            string delimiter = "\t")
        {
            m_File = overwriteData
                ? new StreamWriter(filePath, false)
                : new StreamWriter(filePath, true);
            m_File.AutoFlush = true;
            m_Config = config;
            m_Delimiter = delimiter;
            m_SelectedParameters = selectedParameters ?? m_Config.parameters.ToArray();
            WriteHeaders(additionalHeaders);
        }

        ~CsvWriter()
        {
            Close();
        }

        public void Close()
        {
            m_File.Close();
        }

        void WriteHeaders(string[] additionalHeaders)
        {
            var output = $"Global Iteration{m_Delimiter}";
            foreach (var parameter in m_SelectedParameters)
            {
                output += $"{parameter.parameterName}{m_Delimiter}";
            }
            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                    output += $"{header}{m_Delimiter}";
            }
            output += "\n";
            m_File.Write(output);
        }

        public void WriteParameterIterationToFile(string[] additionalData = null)
        {
            // var output = $"{m_Config.GlobalIterationIndex}{m_Delimiter}";
            var output = "";
            // foreach (var parameter in m_SelectedParameters)
            // {
            //     output += $"{parameter.sampler.GetSampleString()}{m_Delimiter}";
            // }

            if (additionalData != null)
            {
                foreach (var item in additionalData)
                    output += $"{item}{m_Delimiter}";
            }
            output += "\n";
            m_File.Write(output);
        }
    }
}
