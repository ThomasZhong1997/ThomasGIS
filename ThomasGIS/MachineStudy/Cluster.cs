using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using ThomasGIS.SpatialIndex;

namespace ThomasGIS.MachineStudy
{
    public class Cluster
    {
        public HashSet<int> IDList;
        public int ClusterID;

        public double centerX = 0;
        public double centerY = 0;
        public Dictionary<string, object> centerProperties = new Dictionary<string, object>();

        public Cluster(int clusterID)
        {
            this.IDList = new HashSet<int>();
            this.ClusterID = clusterID;
        }
    }

    public class SpatialClusterItem
    {
        public IGeometry Geometry;
        public Dictionary<string, object> Properties;

        public SpatialClusterItem(IGeometry geometry, Dictionary<string, object> properties)
        {
            this.Geometry = geometry;
            this.Properties = new Dictionary<string, object>();
            foreach (string key in properties.Keys)
            {
                this.Properties.Add(key, properties[key]);
            }
        }
    }

    public enum SpatialClusterPrecision
    {
        Simplification = 0,
        KeepOrigin = 1,
    }

    public class SpatialCluster
    {
        private List<SpatialClusterItem> geometryList;
        private CoordinateBase coordinateSystem;

        public SpatialCluster(CoordinateBase coordinate)
        {
            this.geometryList = new List<SpatialClusterItem>();
            this.coordinateSystem = coordinate;
        }

        public bool AddItem(SpatialClusterItem newItem)
        {
            geometryList.Add(newItem);
            return true;
        }

        private BoundaryBox GetBoundaryBox()
        {
            if (geometryList.Count == 0) return new BoundaryBox(0, 0, 0, 0);

            double xmin = double.MaxValue;
            double xmax = double.MinValue;
            double ymin = double.MaxValue;
            double ymax = double.MinValue;

            for (int i = 0; i < geometryList.Count; i++)
            {
                IGeometry geometry = geometryList[i].Geometry;
                BoundaryBox boundaryBox = geometry.GetBoundaryBox();
                xmin = Math.Min(xmin, boundaryBox.XMin);
                xmax = Math.Max(xmax, boundaryBox.XMax);
                ymin = Math.Min(ymin, boundaryBox.YMin);
                ymax = Math.Max(ymax, boundaryBox.YMax);
            }

            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }

        private double TrueSearchRadius(IGeometry geometry, double maxDistance, CoordinateBase coordinateBase)
        {
            BoundaryBox itemBoundary = geometry.GetBoundaryBox();
            double centerX = itemBoundary.XMax * 0.5 + itemBoundary.XMin * 0.5;
            double centerY = itemBoundary.YMax * 0.5 + itemBoundary.YMin * 0.5;
            if (coordinateBase.GetCoordinateType() == CoordinateType.Geographic)
            {
                return DistanceCalculator.SpatialDistanceGeo(centerX, centerY, itemBoundary.XMax, itemBoundary.YMax) + maxDistance;
            }
            else
            {
                return DistanceCalculator.SpatialDistance(centerX, centerY, itemBoundary.XMax, itemBoundary.YMax) + maxDistance;
            }
        }

        private double BaseCost(Dictionary<string, object> properties1, Dictionary<string, object> properties2, List<string> fieldNameList)
        {
            double cost = 0;
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                string fieldName = fieldNameList[i];
                if (properties1.ContainsKey(fieldName) && properties2.ContainsKey(fieldName))
                {
                    double v1 = Convert.ToDouble(properties1[fieldName].ToString());
                    double v2 = Convert.ToDouble(properties2[fieldName].ToString());
                    cost += Math.Abs(v1 - v2);
                }
            }
            return cost;
        }

        // Simplification 选项下，所有的 Geometry 会被抽象为 Point 类型进行计算
        // KeepOrigin 选项下，所有的 Geometry 会保留原始几何形态
        public IEnumerable<Cluster> DBScan(List<string> usingFeatureNameList, double distanceCostMultiple, double maxCost, int minItemNumber, SpatialClusterPrecision option = SpatialClusterPrecision.Simplification)
        {
            BoundaryBox boundaryBox = GetBoundaryBox();
            double scale = (boundaryBox.XMax - boundaryBox.XMin) / 1000.0;
            ISpatialIndex spatialIndex = new GridSpatialIndex(boundaryBox, scale, coordinateSystem);
            for (int i = 0; i < geometryList.Count; i++)
            {
                spatialIndex.AddItem(geometryList[i].Geometry);
            }
            spatialIndex.RefreshIndex();

            List<Cluster> result = new List<Cluster>();
            bool[] isVisited = new bool[geometryList.Count];
            for (int i = 0; i < geometryList.Count; i++)
            {
                if (isVisited[i]) continue;
                Stack<int> itemStack = new Stack<int>();
                itemStack.Push(i);
                isVisited[i] = true;
                Cluster newCluster = new Cluster(result.Count);

                while (itemStack.Count > 0)
                {
                    int nowItemIndex = itemStack.Pop();
                    newCluster.IDList.Add(nowItemIndex);
                    SpatialClusterItem nowItem = geometryList[nowItemIndex];
                    BoundaryBox itemBoundary = nowItem.Geometry.GetBoundaryBox();
                    double centerX = 0.5 * itemBoundary.XMax + 0.5 * itemBoundary.XMin;
                    double centerY = 0.5 * itemBoundary.YMax + 0.5 * itemBoundary.YMin;
                    if (option == SpatialClusterPrecision.KeepOrigin)
                    {
                        double searchRadius = TrueSearchRadius(nowItem.Geometry, maxCost, coordinateSystem);
                        List<int> candidateItemIndex = spatialIndex.SearchID(new Point(centerX, centerY), searchRadius).ToList();
                        foreach (int nextIndex in candidateItemIndex)
                        {
                            if (isVisited[nextIndex]) continue;
                            SpatialClusterItem nextItem = geometryList[nextIndex];
                            double cost = -1;
                            double baseCost = BaseCost(nowItem.Properties, nextItem.Properties, usingFeatureNameList);
                            if (coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                            {
                                cost = baseCost + distanceCostMultiple * DistanceCalculator.SpatialDistanceGeo(nowItem.Geometry, nextItem.Geometry);
                            }
                            else
                            {
                                cost = baseCost + distanceCostMultiple * DistanceCalculator.SpatialDistance(nowItem.Geometry, nextItem.Geometry);
                            }

                            if (cost < maxCost)
                            {
                                isVisited[nextIndex] = true;
                                itemStack.Push(nextIndex);
                            }
                        }
                    }
                    else
                    {
                        List<int> candidateItemIndex = spatialIndex.SearchID(new Point(centerX, centerY), maxCost).ToList();
                        foreach (int nextIndex in candidateItemIndex)
                        {
                            if (isVisited[nextIndex]) continue;
                            SpatialClusterItem nextItem = geometryList[nextIndex];
                            double cost = -1;
                            double baseCost = BaseCost(nowItem.Properties, nextItem.Properties, usingFeatureNameList);
                            BoundaryBox nextBoundaryBox = nextItem.Geometry.GetBoundaryBox();
                            double nextCenterX = 0.5 * nextBoundaryBox.XMax + 0.5 * nextBoundaryBox.XMin;
                            double nextCenterY = 0.5 * nextBoundaryBox.YMax + 0.5 * nextBoundaryBox.YMin;
                            if (coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                            {
                                cost = baseCost + distanceCostMultiple * DistanceCalculator.SpatialDistanceGeo(new Point(centerX, centerY), new Point(nextCenterX, nextCenterY));
                            }
                            else
                            {
                                cost = baseCost + distanceCostMultiple * DistanceCalculator.SpatialDistance(new Point(centerX, centerY), new Point(nextCenterX, nextCenterY));
                            }

                            if (cost < maxCost)
                            {
                                isVisited[nextIndex] = true;
                                itemStack.Push(nextIndex);
                            }
                        }
                    }
                }

                if (newCluster.IDList.Count >= minItemNumber)
                {
                    result.Add(newCluster);
                }
            }
            return result;
        }

        // 用户自定义代价的计算方法，稳定一些
        // @Parameters: costFunction--用户自定义的属性代价计算函数；distanceCostMultiple--距离代价和属性代价间的比例值；maxDistance--聚类时所能容忍的最大距离；minItemNumber--每个聚类簇的最小个体数量 
        public IEnumerable<Cluster> DBScan(Func<Dictionary<string, object>, Dictionary<string, object>, double> costFunction, double distanceCostMultiple, double maxCost, int minItemNumber, SpatialClusterPrecision option = SpatialClusterPrecision.Simplification)
        {
            BoundaryBox boundaryBox = GetBoundaryBox();
            double scale = (boundaryBox.XMax - boundaryBox.XMin) / 1000.0;
            ISpatialIndex spatialIndex = new GridSpatialIndex(boundaryBox, scale, coordinateSystem);
            for (int i = 0; i < geometryList.Count; i++)
            {
                spatialIndex.AddItem(geometryList[i].Geometry);
            }
            spatialIndex.RefreshIndex();

            List<Cluster> result = new List<Cluster>();
            bool[] isVisited = new bool[geometryList.Count];
            for (int i = 0; i < geometryList.Count; i++)
            {
                if (isVisited[i]) continue;
                Stack<int> itemStack = new Stack<int>();
                itemStack.Push(i);
                isVisited[i] = true;
                Cluster newCluster = new Cluster(result.Count);

                while (itemStack.Count > 0)
                {
                    int nowItemIndex = itemStack.Pop();
                    newCluster.IDList.Add(nowItemIndex);
                    SpatialClusterItem nowItem = geometryList[nowItemIndex];
                    BoundaryBox itemBoundary = nowItem.Geometry.GetBoundaryBox();
                    double centerX = 0.5 * itemBoundary.XMax + 0.5 * itemBoundary.XMin;
                    double centerY = 0.5 * itemBoundary.YMax + 0.5 * itemBoundary.YMin;
                    if (option == SpatialClusterPrecision.KeepOrigin)
                    {
                        double searchRadius = TrueSearchRadius(nowItem.Geometry, maxCost, coordinateSystem);
                        List<int> candidateItemIndex = spatialIndex.SearchID(new Point(centerX, centerY), searchRadius).ToList();
                        foreach (int nextIndex in candidateItemIndex)
                        {
                            if (isVisited[nextIndex]) continue;
                            SpatialClusterItem nextItem = geometryList[nextIndex];
                            double cost = -1;
                            if (coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                            {
                                cost = costFunction(nextItem.Properties, nowItem.Properties) + distanceCostMultiple * DistanceCalculator.SpatialDistanceGeo(nowItem.Geometry, nextItem.Geometry);
                            }
                            else
                            {
                                cost = costFunction(nextItem.Properties, nowItem.Properties) + distanceCostMultiple * DistanceCalculator.SpatialDistance(nowItem.Geometry, nextItem.Geometry);
                            }

                            if (cost < maxCost)
                            {
                                isVisited[nextIndex] = true;
                                itemStack.Push(nextIndex);
                            }
                        }
                    }
                    else
                    {
                        List<int> candidateItemIndex = spatialIndex.SearchID(new Point(centerX, centerY), maxCost).ToList();
                        foreach (int nextIndex in candidateItemIndex)
                        {
                            if (isVisited[nextIndex]) continue;
                            SpatialClusterItem nextItem = geometryList[nextIndex];
                            double cost = -1;
                            BoundaryBox nextBoundaryBox = nextItem.Geometry.GetBoundaryBox();
                            double nextCenterX = 0.5 * nextBoundaryBox.XMax + 0.5 * nextBoundaryBox.XMin;
                            double nextCenterY = 0.5 * nextBoundaryBox.YMax + 0.5 * nextBoundaryBox.YMin;
                            if (coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                            {
                                cost = costFunction(nextItem.Properties, nowItem.Properties) + distanceCostMultiple * DistanceCalculator.SpatialDistanceGeo(new Point(centerX, centerY), new Point(nextCenterX, nextCenterY));
                            }
                            else
                            {
                                cost = costFunction(nextItem.Properties, nowItem.Properties) + distanceCostMultiple * DistanceCalculator.SpatialDistance(new Point(centerX, centerY), new Point(nextCenterX, nextCenterY));
                            }

                            if (cost < maxCost)
                            {
                                isVisited[nextIndex] = true;
                                itemStack.Push(nextIndex);
                            }
                        }
                    }
                }

                if (newCluster.IDList.Count >= minItemNumber)
                {
                    result.Add(newCluster);
                }
            }
            return result;
        }

        public IEnumerable<Cluster> KMeans(Func<Dictionary<string, object>, Dictionary<string, object>, double> costFunction, Func<List<Dictionary<string, object>>, Dictionary<string, object>> propertyKeepFunction, double distanceCostMultiple, int clusterNumber, SpatialClusterPrecision option = SpatialClusterPrecision.Simplification)
        {
            if (geometryList.Count > 10000) throw new Exception("要素数量超过10000个暂不支持该处理方法");

            if (clusterNumber > this.geometryList.Count) clusterNumber = this.geometryList.Count;

            bool needNext = true;
            List<Cluster> baseCluster = new List<Cluster>();
            List<HashSet<int>> prevClusterResult = new List<HashSet<int>>();
            for (int i = 0; i < clusterNumber; i++)
            {
                Cluster newCluster = new Cluster(i);
                SpatialClusterItem baseItem = geometryList[i];
                BoundaryBox baseItemMBR = baseItem.Geometry.GetBoundaryBox();
                newCluster.centerX = baseItemMBR.XMin * 0.5 + baseItemMBR.XMax * 0.5;
                newCluster.centerY = baseItemMBR.YMin * 0.5 + baseItemMBR.YMax * 0.5;
                newCluster.centerProperties = baseItem.Properties;
                baseCluster.Add(newCluster);
                prevClusterResult.Add(new HashSet<int>());
            }

            while (needNext)
            {
                for (int i = 0; i < geometryList.Count; i++)
                {
                    SpatialClusterItem oneItem = geometryList[i];
                    List<double> score = new List<double>();
                    for (int j = 0; j < clusterNumber; j++)
                    {
                        Cluster oneCluster = baseCluster[j];
                        double baseCost = costFunction(oneCluster.centerProperties, oneItem.Properties);
                        baseCost += distanceCostMultiple * DistanceCalculator.SpatialDistance(new Point(oneCluster.centerX, oneCluster.centerY), oneItem.Geometry);
                        score.Add(baseCost);
                    }
                    int minScoreIndex = score.IndexOf(score.Min());
                    baseCluster[minScoreIndex].IDList.Add(i);
                }

                int count = 0;
                // 验证是否需要继续聚类
                for (int i = 0; i < baseCluster.Count; i++)
                {
                    int unionCount = baseCluster[i].IDList.Union(prevClusterResult[i]).Count();
                    int intersectCount = baseCluster[i].IDList.Intersect(prevClusterResult[i]).Count();
                    if (unionCount == intersectCount)
                    {
                        count += 1;
                    }
                }

                if (count == clusterNumber) break;

                for (int i = 0; i < clusterNumber; i++)
                {
                    prevClusterResult[i].Clear();
                    prevClusterResult[i].UnionWith(baseCluster[i].IDList);
                    List<Dictionary<string, object>> propertyList = new List<Dictionary<string, object>>();
                    double sumX = 0, sumY = 0;
                    foreach (int index in prevClusterResult[i])
                    {
                        BoundaryBox mbr = geometryList[index].Geometry.GetBoundaryBox();
                        double centerX = 0.5 * mbr.XMin + 0.5 * mbr.XMax;
                        double centerY = 0.5 * mbr.YMin + 0.5 * mbr.YMax;
                        sumX += centerX;
                        sumY += centerY;
                        propertyList.Add(geometryList[index].Properties);
                    }
                    baseCluster[i].IDList.Clear();
                    baseCluster[i].centerX = sumX / prevClusterResult[i].Count;
                    baseCluster[i].centerY = sumY / prevClusterResult[i].Count;
                    baseCluster[i].centerProperties = propertyKeepFunction(propertyList);
                }
            }

            return baseCluster;
        }

        public IEnumerable<Cluster> Level(List<string> usingFeatureNameList)
        {
            if (this.geometryList.Count > 10000)
            {
                
            }
            throw new NotImplementedException("大数据量下暂不支持，速度太慢！");
        }
    }
}
