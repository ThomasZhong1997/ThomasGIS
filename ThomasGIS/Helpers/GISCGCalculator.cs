using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;
using System.Linq;

namespace ThomasGIS.Helpers
{
    public class Location
    {
        public int X;
        public int Y;

        public Location(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class EdgeNode
    {
        public int maxY;
        public double downX;
        public double k;

        public EdgeNode(int maxY, double downX, double k)
        {
            this.maxY = maxY;
            this.downX = downX;
            this.k = k;
        }
    }

    public static class GISCGCalculator
    {
        public static IEnumerable<Location> DigitalDifferentialAnalyzer(int startX, int startY, int endX, int endY)
        {
            List<Location> locationList = new List<Location>();

            if (startX == endX)
            {
                if (startY < endY)
                {
                    for (int i = startY; i < endY + 1; i++)
                    {
                        locationList.Add(new Location(startX, i));
                    }
                }
                else
                {
                    for (int i = 0; i < startY - endY + 1; i++)
                    {
                        locationList.Add(new Location(startX, startY - i));
                    }
                }
            }
            else
            {
                double slope = (double)(endY - startY) / (double)(endX - startX);
                if (slope > -1 && slope < 1)
                {
                    if (startX < endX)
                    {
                        for (int i = 0; i < endX - startX + 1; i++)
                        {
                            locationList.Add(new Location(startX + i, (int)Math.Floor(startY + slope * i)));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < startX - endX + 1; i++)
                        {
                            locationList.Add(new Location(startX - i, (int)Math.Floor(startY - slope * i)));
                        }
                    }
                }
                else
                {
                    slope = 1.0 / slope;
                    if (startY < endY)
                    {
                        for (int i = 0; i < endY - startY + 1; i++)
                        {
                            locationList.Add(new Location((int)Math.Floor(startX + slope * i), startY + i));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < startY - endY + 1; i++)
                        {
                            locationList.Add(new Location((int)Math.Floor(startX - slope * i), startY - i));
                        }
                    }
                }
            }

            return locationList;
        }

        public static IEnumerable<Location> PolylineFillLocations(IEnumerable<Location> boundaryLocations)
        {
            if (boundaryLocations.Count() < 2) return new List<Location>();

            List<Location> result = new List<Location>();
            List<Location> locationList = boundaryLocations.ToList();
            for (int i = 0; i < locationList.Count - 1; i++)
            {
                Location sourceLocation = locationList[i];
                Location targetLocation = locationList[i + 1];
                IEnumerable<Location> onePart = DigitalDifferentialAnalyzer(sourceLocation.X, sourceLocation.Y, targetLocation.X, targetLocation.Y);
                result.AddRange(onePart);
            }
            return result;
        }

        public static IEnumerable<Location> PolygonFillLocations(IEnumerable<Location> boundaryLocations)
        {
            if (boundaryLocations.Count() < 4) new List<Location>();

            List<Location> result = new List<Location>();
            List<Location> locationList = boundaryLocations.ToList();
            Dictionary<int, List<EdgeNode>> edgeTable = new Dictionary<int, List<EdgeNode>>();

            // 构造边表
            for (int i = 0; i < locationList.Count - 1; i++)
            {
                int startX = locationList[i].X;
                int startY = locationList[i].Y;
                int endX = locationList[i + 1].X;
                int endY = locationList[i + 1].Y;

                if (startY == endY) continue;

                int maxY, minX, minY;
                if (startY > endY)
                {
                    maxY = startY;
                    minX = endX;
                    minY = endY;
                }
                else
                {
                    minX = startX;
                    minY = startY;
                    maxY = endY;
                }

                double k = (double)(endX - startX) / (double)(endY - startY);

                if (!edgeTable.ContainsKey(minY))
                {
                    edgeTable[minY] = new List<EdgeNode>();
                    
                }

                edgeTable[minY].Add(new EdgeNode(maxY, minX, k));
            }

            foreach (int key in edgeTable.Keys)
            {
                edgeTable[key].Sort((left, right) =>
                {
                    return left.downX.CompareTo(right.downX);
                });
            }

            if (edgeTable.Count == 0)
            {
                result.Add(boundaryLocations.First());
                return result;
            }

            int min = edgeTable.Keys.Min();
            int max = int.MinValue;
            foreach (int yKey in edgeTable.Keys)
            {
                foreach (EdgeNode oneEdge in edgeTable[yKey])
                {
                    max = Math.Max(max, oneEdge.maxY);
                }
            }

            List<EdgeNode> activeEdgeTable = new List<EdgeNode>();

            // 扫描线
            for (int nowY = min; nowY <= max; nowY++)
            {
                // 将当前Y中的边加入活性边表
                if (edgeTable.ContainsKey(nowY))
                {
                    activeEdgeTable.AddRange(edgeTable[nowY]);
                }


                // 将活性边表中的边按X排序
                activeEdgeTable.Sort((left, right) =>
                {
                    return left.downX.CompareTo(right.downX);
                });

                // 两两配对
                for (int j = 0; j < activeEdgeTable.Count / 2; j++)
                {
                    EdgeNode startEdge = activeEdgeTable[j * 2];
                    EdgeNode endEdge = activeEdgeTable[j * 2 + 1];

                    // 填充location
                    for (int fillX = (int)startEdge.downX; fillX <= endEdge.downX; fillX++)
                    {
                        result.Add(new Location(fillX, nowY));
                    }
                }

                // 更新X，顺便删除已经算完的边
                for (int j = 0; j < activeEdgeTable.Count; j++)
                {
                    activeEdgeTable[j].downX = activeEdgeTable[j].downX + activeEdgeTable[j].k;
                    if (activeEdgeTable[j].maxY <= nowY)
                    {
                        activeEdgeTable.RemoveAt(j);
                        j--;
                    }
                }
            }
            return result;
        }
    }
}
