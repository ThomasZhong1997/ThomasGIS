using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ThomasGIS.Helpers
{
    static class SampleHelper
    {
        public static string[] ReservoirSampling(string inputFilePath, int sampleCount)
        {
            if (sampleCount <= 0) return new string[0];

            string[] dataContainer = new string[sampleCount];

            long nowLineNumber = 0;

            Random rd = new Random();

            using (StreamReader sr = new StreamReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    double randomNumber = (double)rd.NextDouble();
                    string line = sr.ReadLine();
                    nowLineNumber += 1;
                    double keepPercent = sampleCount / nowLineNumber;

                    if (nowLineNumber < sampleCount)
                    {
                        dataContainer[nowLineNumber - 1] = line;
                    }
                    else
                    {
                        if (randomNumber < keepPercent)
                        {
                            int replaceIndex = rd.Next(0, sampleCount - 1);
                            dataContainer[replaceIndex] = line;
                        }
                    }
                }
            }

            return dataContainer;
        }
    }
}
