using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ThomasGIS.Network
{
    public static class NetworkIndicators
    {
        // WL 网络结构相似度
        // 输入参数
        // network1: 第一个网络
        // network2: 第二个网络
        // nodeField：使用网络的节点的哪项属性作为标签进行计算，若为null或""，则默认均为1
        // 注意：被选择的属性应当可被转为字符串，用户自定义的类应当重写ToString()方法，Key生成按照字典序排序以保证一致性，内部使用MD5加密Key保证一致性
        public static double WeisfeilerLehmanNetworkSimilarity(Network network1, Network network2, string nodeField=null)
        {
            if (network1.NodeNumber == 0 || network2.NodeNumber == 0) return -1;

            if (nodeField == null) nodeField = "";
            network1.RefreshNeighborMatrix(nodeField);
            network2.RefreshNeighborMatrix(nodeField);

            List<string> network1Features = new List<string>();
            List<string> network2Features = new List<string>();

            for (int i = 0; i < network1.NodeNumber; i++)
            {
                INetworkNode nowNode = network1.GetNodeByIndex(i);
                List<string> stringList = new List<string>();

                object property = nowNode.GetProperty(nodeField);
                if (nodeField == "" || nodeField == null || property == null)
                {
                    stringList.Add("1");
                }
                else
                {
                    stringList.Add(property.ToString());
                }

                List<LinkValuePair> arcs = network1.neighborMatrix[i];
                foreach (LinkValuePair arc in arcs)
                {
                    if (nodeField == "" || nodeField == null)
                    {
                        stringList.Add("1");
                    }
                    else
                    {
                        stringList.Add(arc.Value.ToString());
                    }
                }
                stringList.Sort();

                StringBuilder sb = new StringBuilder();
                foreach (string oneString in stringList)
                {
                    sb.Append(oneString);
                }

                MD5 md5 = MD5.Create();
                byte[] hashKey = md5.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString()));
                network1Features.Add(BitConverter.ToString(hashKey).Replace("-", ""));
            }

            for (int i = 0; i < network2.NodeNumber; i++)
            {
                INetworkNode nowNode = network2.GetNodeByIndex(i);
                List<string> stringList = new List<string>();

                object property = nowNode.GetProperty(nodeField);
                if (nodeField == "" || nodeField == null || property == null)
                {
                    stringList.Add("1");
                }
                else
                {
                    stringList.Add(property.ToString());
                }

                List<LinkValuePair> arcs = network2.neighborMatrix[i];
                foreach (LinkValuePair arc in arcs)
                {
                    if (nodeField == "" || nodeField == null)
                    {
                        stringList.Add("1");
                    }
                    else
                    {
                        stringList.Add(arc.Value.ToString());
                    }
                }
                stringList.Sort();

                StringBuilder sb = new StringBuilder();
                foreach (string oneString in stringList)
                {
                    sb.Append(oneString);
                }

                MD5 md5 = MD5.Create();
                byte[] hashKey = md5.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString()));
                network2Features.Add(BitConverter.ToString(hashKey).Replace("-", ""));
            }

            network1Features.Sort();
            network2Features.Sort();

            int m = 0;
            int n = 0;
            double sameCount = 0;
            double totalCount = 0;

            while (m < network1Features.Count&& n < network2Features.Count)
            {
                if (network1Features[m].Equals(network2Features[n]))
                {
                    sameCount += 1;
                    totalCount += 1;
                    m += 1;
                    n += 1;
                    continue;
                }
                else
                {
                    if (network1Features[m].CompareTo(network2Features[n]) < 0)
                    {
                        totalCount += 1;
                        m += 1;
                        continue;
                    }
                    else
                    {
                        totalCount += 1;
                        n += 1;
                        continue;
                    }
                }
            }

            if (m < network1Features.Count)
            {
                totalCount += (network1Features.Count - m);
            }

            if (n < network2Features.Count)
            {
                totalCount += (network2Features.Count - n);
            }

            return sameCount / totalCount;   
        }
    }
}
