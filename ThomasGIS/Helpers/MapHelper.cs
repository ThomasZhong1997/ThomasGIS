using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using ThomasGIS.BaseConfiguration;
using System.Threading.Tasks;

namespace ThomasGIS.Helpers
{
    public static class MapHelper
    {
        // hash function based on MD5
        private static int HashFunction(string inputData, int mapNumber)
        {
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(Encoding.GetEncoding(Configuration.GetConfiguration("trajectory.file.encoding.type")).GetBytes(inputData));
            long sum = 0;
            for (int i = 0; i < result.Length; i++)
            {
                sum += result[i];
            }
            return (int)(sum % mapNumber);
        }

        // hash function based on MD5
        private static int HashFunction2(string inputData, int mapNumber)
        {
            int sum = 0;
            for (int i = 0; i < inputData.Length; i++)
            {
                sum += inputData[i];
            }
            return (int)(sum % mapNumber);
        }

        private static int BKDRHashFunction(string str, int mapNumber)
        {
            int seed = 131; // 31 131 1313 13131 131313 etc..
            int hash = 0;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            while (count > 0)
            {
                hash = hash * seed + (bitarray[bitarray.Length - count]);
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        private static int APHashFunction(string str, int mapNumber)
        {
            int hash = 0;
            int i;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            for (i = 0; i < count; i++)
            {
                if ((i & 1) == 0)
                {
                    hash ^= ((hash << 7) ^ (bitarray[i]) ^ (hash >> 3));
                }
                else
                {
                    hash ^= (~((hash << 11) ^ (bitarray[i]) ^ (hash >> 5)));
                }
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;

        }

        private static int SDBMHashFunction(string str, int mapNumber)
        {
            int hash = 0;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;

            while (count > 0)
            {
                // equivalent to: hash = 65599*hash + (*str++);
                hash = (bitarray[bitarray.Length - count]) + (hash << 6) + (hash << 16) - hash;
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;

        }

        private static int RSHashFunction(string str, int mapNumber)
        {
            int b = 378551;
            int a = 63689;
            int hash = 0;

            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            while (count > 0)
            {
                hash = hash * a + (bitarray[bitarray.Length - count]);
                a *= b;
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        private static int JSHashFunction(string str, int mapNumber)
        {
            int hash = 1315423911;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            while (count > 0)
            {
                hash ^= ((hash << 5) + (bitarray[bitarray.Length - count]) + (hash >> 2));
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        private static int PJWHashFunction(string str, int mapNumber)
        {
            int BitsInUnignedInt = (int)(sizeof(int) * 8);
            int ThreeQuarters = (int)((BitsInUnignedInt * 3) / 4);
            int OneEighth = (int)(BitsInUnignedInt / 8);
            int hash = 0;
            unchecked
            {
                int HighBits = (int)(0xFFFFFFFF) << (BitsInUnignedInt - OneEighth);
                int test = 0;
                int count;
                char[] bitarray = str.ToCharArray();
                count = bitarray.Length;
                while (count > 0)
                {
                    hash = (hash << OneEighth) + (bitarray[bitarray.Length - count]);
                    if ((test = hash & HighBits) != 0)
                    {
                        hash = ((hash ^ (test >> ThreeQuarters)) & (~HighBits));
                    }
                    count--;
                }
            }
            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        private static int ELFHashFunction(string str, int mapNumber)
        {
            int hash = 0;
            int x = 0;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            unchecked
            {
                while (count > 0)
                {
                    hash = (hash << 4) + (bitarray[bitarray.Length - count]);
                    if ((x = hash & (int)0xF0000000) != 0)
                    {
                        hash ^= (x >> 24);
                        hash &= ~x;
                    }
                    count--;
                }
            }
            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        private static int DJBHashFunction(string str, int mapNumber)
        {
            int hash = 5381;
            int count;
            char[] bitarray = str.ToCharArray();
            count = bitarray.Length;
            while (count > 0)
            {
                hash += (hash << 5) + (bitarray[bitarray.Length - count]);
                count--;
            }

            return (hash & 0x7FFFFFFF) % mapNumber;
        }

        // type == 0 create, type == 1 append
        public static bool MapFiles(string inputFolderPath, string outputFolderPath, int mapNumber, string seperator, int keyIndex, int type=0)
        {
            string[] files = Directory.GetFiles(inputFolderPath);

            StreamWriter[] streamWriterList = new StreamWriter[mapNumber];
            for (int i = 0; i < mapNumber; i++)
            {
                if (type == 0)
                {
                    streamWriterList[i] = new StreamWriter(new FileStream(string.Format(outputFolderPath + "/{0}", i), FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                }
                else
                {
                    streamWriterList[i] = new StreamWriter(new FileStream(string.Format(outputFolderPath + "/{0}", i), FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                }
            }

            Parallel.ForEach(files, file =>
            {
                List<string>[] tempContainer = new List<string>[mapNumber];
                for (int i = 0; i < mapNumber; i++)
                {
                    tempContainer[i] = new List<string>();
                }

                StreamReader reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                int count = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] items = line.Split(new string[] { seperator }, StringSplitOptions.None);

                    string hashFunctionName = Configuration.GetConfiguration("map.hash.function.name");
                    int hashCode = 0;

                    switch (hashFunctionName)
                    {
                        case "BKDR":
                            hashCode = BKDRHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "AP":
                            hashCode = APHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "SDBM":
                            hashCode = SDBMHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "RS":
                            hashCode = RSHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "JS":
                            hashCode = JSHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "PJW":
                            hashCode = PJWHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "ELF":
                            hashCode = ELFHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "DJB":
                            hashCode = DJBHashFunction(items[keyIndex], mapNumber);
                            break;
                        case "MD5":
                            hashCode = HashFunction(items[keyIndex], mapNumber);
                            break;
                        default:
                            hashCode = HashFunction2(items[keyIndex], mapNumber);
                            break;
                    }

                    tempContainer[hashCode].Add(line);
                    count += 1;
                    if (count % 100000 == 0)
                    {
                        for (int i = 0; i < mapNumber; i++)
                        {
                            lock (streamWriterList[i])
                            {
                                foreach (string outputLine in tempContainer[i])
                                {
                                    streamWriterList[i].WriteLine(outputLine);
                                }
                            }
                            tempContainer[i].Clear();
                        }
                        count = 0;
                    }
                }

                for (int i = 0; i < mapNumber; i++)
                {
                    lock (streamWriterList[i])
                    {
                        foreach (string outputLine in tempContainer[i])
                        {
                            streamWriterList[i].WriteLine(outputLine);
                        }
                    }
                    tempContainer[i].Clear();
                }
            });

            for (int i = 0; i < mapNumber; i++)
            {
                streamWriterList[i].Close();
            }

            return true;
        }
    }
}
