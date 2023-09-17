using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Grids.Basic;
using ThomasGIS.Helpers;
using ThomasGIS.TrajectoryPackage.QuickIO;
using ThomasGIS.Vector;

namespace ThomasGIS.TrajectoryPackage
{
    public class TrajectorySetStatistics
    {
        public double AvgDistance = -1;
        public double AvgTime = -1;
        public double AvgSpeed = -1;

        public double Time50Per = -1;
        public double Time75Per = -1;
        public double Time90Per = -1;
        public double Time95Per = -1;
        public double Time99Per = -1;
        public double Time999Per = -1;

        public double Distance50Per = -1;
        public double Distance75Per = -1;
        public double Distance90Per = -1;
        public double Distance95Per = -1;
        public double Distance99Per = -1;
        public double Distance999Per = -1;

        public string startDateTime = "未知";
        public string endDateTime = "未知";
    }

    public class TrajectorySet : ITrajectorySet
    {
        public List<ITrajectory> TrajectoryList { get; } = new List<ITrajectory>();

        public int TrajectoryNumber => TrajectoryList.Count;

        public TrajectorySet()
        {

        }

        public TrajectorySet(List<ITrajectory> trajectoryList)
        {
            this.TrajectoryList.AddRange(trajectoryList);
        }

        public IShapefile ExportToShapefile(CoordinateBase targetCoordinate)
        {
            IShapefile shapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);

            shapefile.AddField("ID", DBFFieldType.Char, 50, 0);
            shapefile.AddField("Length", DBFFieldType.Number, 20, 5);

            foreach (Trajectory trajectory in TrajectoryList)
            {
                ShpPolyline newPolyline = new ShpPolyline(trajectory.GetPointEnumerable());
                Dictionary<string, object> values = new Dictionary<string, object>();
                values.Add("ID", trajectory.TaxiID);
                values.Add("Length", trajectory.Length);
                shapefile.AddFeature(newPolyline, values);
            }

            // 如果轨迹坐标系与其不一致，需要坐标转换
            // 暂时还没做 TODO
            shapefile.SetCoordinateRef(targetCoordinate);

            return shapefile;
        }

        public TrajectorySet(string filePath, ITrajectorySetCreator creator, CoordinateBase coordinateSystem, bool preprocessFlag = false, bool trajectorySplitFlag = false, bool debug = false)
        {
            ConcurrentDictionary<string, List<GnssPoint>> originTrajectorySet = new ConcurrentDictionary<string, List<GnssPoint>>();

            StreamReader sr = new StreamReader(new FileStream(filePath, FileMode.Open));
            // 文件大小
            long fileSize = sr.BaseStream.Length;
            sr.Close();

            // 从配置文件中获取每个块的大小
            long eachCoreReadLength = Convert.ToInt64(Configuration.GetConfiguration("datareader.block.size"));

            long usedSystemCore = (fileSize / eachCoreReadLength) + 1;

            // 每个线程在文件中的理论初始偏移量
            long[] offsetIndex = new long[usedSystemCore];

            for (int i = 0; i < usedSystemCore; i++)
            {
                offsetIndex[i] = i * eachCoreReadLength;
            }

            Parallel.ForEach(offsetIndex, offset =>
            {
                FileStream baseStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                baseStream.Seek(offset, SeekOrigin.Begin);

                long readLength = 0;

                StringBuilder sb = new StringBuilder();
                // 一个一个试到 \n 出现，然后才能按行读（忽略首行同样重要）
                if (offset != 0 || (offset == 0 && creator.IsJumpTitleLine()))
                {
                    if (baseStream.Position >= baseStream.Length) return;

                    byte[] ch = new byte[1];
                    baseStream.Read(ch, 0, 1);
                    readLength++;
                    while (ch[0] != '\n' && baseStream.Position < baseStream.Length)
                    {
                        sb.Append((char)ch[0]);
                        baseStream.Read(ch, 0, 1);
                        readLength++;
                    }
                }

                // Console.WriteLine(string.Format("{0}: {1}", offset, sb.ToString()));

                // 读取32MB的数据块
                int chunkSize = (int)eachCoreReadLength - (int)readLength;

                if (offset + chunkSize > fileSize) chunkSize = (int)(fileSize - offset) - (int)readLength;

                byte[] chunk = new byte[chunkSize];
                baseStream.Read(chunk, 0, chunkSize);

                List<GnssPoint> tempPointList = new List<GnssPoint>();
                List<string> validateList = new List<string>();

                StringBuilder oneLineStringBuilder = new StringBuilder();
                for (int i = 0; i < chunkSize; i++)
                {
                    if (chunk[i] != '\n')
                    {
                        oneLineStringBuilder.Append((char)chunk[i]);
                    }
                    else
                    {
                        string line = oneLineStringBuilder.ToString();
                        validateList.Add(line);
                        GnssPoint newPoint = creator.Reader(line);
                        if (newPoint != null)
                        {
                            tempPointList.Add(newPoint);
                        }
                        oneLineStringBuilder.Clear();
                    }
                }

                if (oneLineStringBuilder.Length != 0)
                {
                    byte[] ch = new byte[1];
                    baseStream.Read(ch, 0, 1);
                    while (ch[0] != '\n' && baseStream.Position < baseStream.Length)
                    {
                        oneLineStringBuilder.Append((char)ch[0]);
                        baseStream.Read(ch, 0, 1);
                    }

                    validateList.Add(oneLineStringBuilder.ToString());
                    GnssPoint finalNewPoint = creator.Reader(oneLineStringBuilder.ToString());
                    if (finalNewPoint != null)
                    {
                        tempPointList.Add(finalNewPoint);
                        oneLineStringBuilder.Clear();
                    }
                }

                lock (originTrajectorySet)
                {
                    foreach (GnssPoint gnssPoint in tempPointList)
                    {
                        if (!originTrajectorySet.ContainsKey(gnssPoint.ID))
                        {
                            originTrajectorySet.TryAdd(gnssPoint.ID, new List<GnssPoint>());
                        }

                        originTrajectorySet[gnssPoint.ID].Add(gnssPoint);
                    }
                }
            });

            Console.WriteLine(originTrajectorySet.Count);
            int pointNumber = 0;
            foreach (string key in originTrajectorySet.Keys)
            {
                pointNumber += originTrajectorySet[key].Count;
            }
            Console.WriteLine(pointNumber);

            if (!debug)
            {
                Parallel.ForEach(originTrajectorySet, pair =>
                {
                    // 轨迹对象的轨迹点数量 < 2 则无法生成
                    if (pair.Value.Count < 2) return;

                    Trajectory newTrajectory = new Trajectory(pair.Key, coordinateSystem, pair.Value);
                    if (preprocessFlag)
                    {
                        creator.PointFilter(newTrajectory);
                    }

                    if (newTrajectory.PointNumber < 2) return;

                    if (trajectorySplitFlag)
                    {
                        IEnumerable<Trajectory> trajectories = creator.TrajectorySpliter(newTrajectory);
                        if (trajectories.Count() >= 1)
                        {
                            for (int i = 0; i < trajectories.Count(); i++)
                            {
                                if (trajectories.ElementAt(i).PointNumber < 2) continue;
                                if (trajectories.ElementAt(i).Length == 0) continue;
                                lock (this.TrajectoryList)
                                {
                                    this.TrajectoryList.Add(trajectories.ElementAt(i));
                                }
                            }
                        }
                    }
                    else
                    {
                        lock (this.TrajectoryList)
                        {
                            this.TrajectoryList.Add(newTrajectory);
                        }
                    }
                });
            }
            else
            {
                foreach (KeyValuePair<string, List<GnssPoint>> pair in originTrajectorySet)
                {
                    // 轨迹对象的轨迹点数量 < 2 则无法生成
                    if (pair.Value.Count < 2) continue;

                    Trajectory newTrajectory = new Trajectory(pair.Key, coordinateSystem, pair.Value);
                    if (preprocessFlag)
                    {
                        creator.PointFilter(newTrajectory);
                    }

                    if (newTrajectory.PointNumber < 2) continue;

                    if (trajectorySplitFlag)
                    {
                        IEnumerable<Trajectory> trajectories = creator.TrajectorySpliter(newTrajectory);
                        if (trajectories.Count() >= 1)
                        {
                            for (int i = 0; i < trajectories.Count(); i++)
                            {
                                if (trajectories.ElementAt(i).PointNumber < 2) continue;
                                if (trajectories.ElementAt(i).Length == 0) continue;
                                this.TrajectoryList.Add(trajectories.ElementAt(i));
                            }
                        }
                    }
                    else
                    {
                        this.TrajectoryList.Add(newTrajectory);                    
                    }
                }
            }
        }

        public TrajectorySet(FileInfo dictionary, ITrajectorySetCreator creator, CoordinateBase coordinateSystem, bool preprocessFlag = false, bool trajectorySplitFlag = false)
        {
            string[] innerFiles = Directory.GetFiles(dictionary.FullName);
            ConcurrentDictionary<string, List<GnssPoint>> originTrajectorySet = new ConcurrentDictionary<string, List<GnssPoint>>();

            // 1分41秒，批量读取再写入并行容器
            Parallel.ForEach(innerFiles, fileName =>
            {
                List<GnssPoint> oneFilePoints = new List<GnssPoint>();

                using (StreamReader sr = new StreamReader(new FileStream(fileName, FileMode.Open)))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        GnssPoint readerResult = creator.Reader(line);
                        if (readerResult == null) continue;
                        oneFilePoints.Add(readerResult);
                    }
                    sr.Close();
                }

                lock (originTrajectorySet)
                {
                    foreach (GnssPoint gnssPoint in oneFilePoints)
                    {
                        if (!originTrajectorySet.ContainsKey(gnssPoint.ID))
                        {
                            originTrajectorySet.TryAdd(gnssPoint.ID, new List<GnssPoint>());
                        }
                        originTrajectorySet[gnssPoint.ID].Add(gnssPoint);
                    }
                }

                oneFilePoints.Clear();
            });

            //// 3分12秒
            //Parallel.ForEach(innerFiles, fileName =>
            //{
            //    using (StreamReader sr = new StreamReader(new FileStream(fileName, FileMode.Open)))
            //    {
            //        Console.WriteLine(fileName + " opened");
            //        while (!sr.EndOfStream)
            //        {
            //            string line = sr.ReadLine();
            //            GnssPoint readerResult = creator.Reader(line);
            //            if (readerResult == null) continue;

            //            if (!originTrajectorySet.ContainsKey(readerResult.ID))
            //            {
            //                originTrajectorySet.TryAdd(readerResult.ID, new List<GnssPoint>());
            //            }
            //            originTrajectorySet[readerResult.ID].Add(readerResult);
            //        }
            //    }
            //});

            //// 大于3分12秒
            //foreach (string fileName in innerFiles) 
            //{
            //    using (StreamReader sr = new StreamReader(new FileStream(fileName, FileMode.Open)))
            //    {
            //        Console.WriteLine(fileName + " opened");
            //        while (!sr.EndOfStream)
            //        {
            //            string line = sr.ReadLine();
            //            GnssPoint readerResult = creator.Reader(line);
            //            if (readerResult == null) continue;

            //            if (!originTrajectorySet.ContainsKey(readerResult.ID))
            //            {
            //                originTrajectorySet.TryAdd(readerResult.ID, new List<GnssPoint>());
            //            }
            //            originTrajectorySet[readerResult.ID].Add(readerResult);
            //        }
            //    }
            //}


            Parallel.ForEach(originTrajectorySet.Keys, oneKey =>
            {
                if (originTrajectorySet[oneKey].Count < 2) return;

                Trajectory newTrajectory = new Trajectory(oneKey, coordinateSystem, originTrajectorySet[oneKey]);
                if (preprocessFlag)
                {
                    creator.PointFilter(newTrajectory);
                }

                if (newTrajectory.PointNumber < 2) return;

                if (trajectorySplitFlag)
                {
                    IEnumerable<Trajectory> splitedTrajectory = creator.TrajectorySpliter(newTrajectory);
                    lock (this.TrajectoryList)
                    {
                        foreach (Trajectory oneTrajectory in splitedTrajectory)
                        {
                            if (oneTrajectory.PointNumber < 2) continue;
                            if (oneTrajectory.Length == 0) continue;
                            this.TrajectoryList.Add(oneTrajectory);
                        }
                    }
                }
                else
                {
                    lock (this.TrajectoryList)
                    {
                        this.TrajectoryList.Add(newTrajectory);
                    }
                }
            });
        }

        public TrajectorySet(string filePath)
        {
            using (StreamReader sr = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string flag = sr.ReadLine();
                if (flag != "Stardard ThomasGIS TrajectorySet File") throw new Exception("非标准ThomasGIS轨迹数据集文本文件，解析失败");
                string[] baseInfo = sr.ReadLine().Split('\t');

                int trajectoryNumber = Convert.ToInt32(baseInfo[0]);
                string separator = baseInfo[1];

                string[] fields = sr.ReadLine().Trim('\n').Split('\t');

                for (int i = 0; i < trajectoryNumber; i++)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('\t');
                    int pointCount = Convert.ToInt32(items[1]);
                    string ID = items[0];
                    string coorWKT = sr.ReadLine();
                    CoordinateBase coordinateBase = CoordinateGenerator.ParseFromWkt(coorWKT.Trim('\n'));
                    List<GnssPoint> pointList = new List<GnssPoint>();
                    for (int j = 0; j < pointCount; j++)
                    {
                        string[] infos = sr.ReadLine().Trim('\n').Split(new string[] { separator }, StringSplitOptions.None);
                        double longitude = Convert.ToDouble(infos[1]);
                        double latitude = Convert.ToDouble(infos[2]);
                        double timestamp = Convert.ToDouble(infos[3]);
                        double speed = Convert.ToDouble(infos[4]);
                        double direction = Convert.ToDouble(infos[5]);
                        Dictionary<string, object> extraInfo = new Dictionary<string, object>();
                        for (int k = 6; k < infos.Length; k++)
                        {
                            extraInfo.Add(fields[k], infos[k]);
                        }
                        pointList.Add(new GnssPoint(ID, longitude, latitude, timestamp, direction, speed, extraInfo));
                    }
                    this.AddTrajectory(new Trajectory(ID, coordinateBase, pointList));
                }
            }
        }
        
        public bool AddTrajectory(ITrajectory trajectory)
        {
            this.TrajectoryList.Add(trajectory);
            return true;
        }

        public bool RemoveTrajectory(int index)
        {
            this.TrajectoryList.RemoveAt(index);
            return true;
        }

        public bool SortByLength()
        {
            this.TrajectoryList.Sort((t1, t2) =>
            {
                return t2.GetLength().CompareTo(t1.GetLength());
            });

            return true;
        }

        public IEnumerable<ITrajectory> LengthLongerThan(double length)
        {
            return this.TrajectoryList.Where(trajectory => trajectory.GetLength() >= length);
        }

        public IEnumerable<ITrajectory> LengthShorterThan(double length)
        {
            return this.TrajectoryList.Where(trajectory => trajectory.GetLength() <= length);
        }

        public IEnumerable<ITrajectory> TimeShorterThan(double timeInterval)
        {
            List<ITrajectory> result = new List<ITrajectory>();

            for (int i = 0; i < this.TrajectoryNumber; i++)
            {
                ITrajectory trajectory = this.GetTrajectoryByIndex(i);
                double dTime = trajectory.GetPointByIndex(-1).Timestamp - trajectory.GetPointByIndex(0).Timestamp;
                if (dTime < timeInterval)
                {
                    result.Add(trajectory);
                }
            }

            return result;
        }

        public IEnumerable<ITrajectory> TimeLongerThan(double timeInterval)
        {
            List<ITrajectory> result = new List<ITrajectory>();

            for (int i = 0; i < this.TrajectoryNumber; i++)
            {
                ITrajectory trajectory = this.GetTrajectoryByIndex(i);
                double dTime = trajectory.GetPointByIndex(-1).Timestamp - trajectory.GetPointByIndex(0).Timestamp;
                if (dTime >= timeInterval)
                {
                    result.Add(trajectory);
                }
            }

            return result;
        }

        // 轨迹数据集在timestamp处的轨迹点快照
        public IEnumerable<GnssPoint> SnapShotByTime(int timeStamp)
        {
            List<GnssPoint> snapShotList = new List<GnssPoint>();

            foreach (Trajectory trajectory in TrajectoryList)
            {
                IEnumerable<GnssPoint> trajectoryPoints = trajectory.GetPointEnumerable();
                List<GnssPoint> pointList = trajectoryPoints.ToList();
                for (int i = 0; i < pointList.Count - 1; i++)
                {
                    GnssPoint startPoint = pointList[i];
                    GnssPoint endPoint = pointList[i + 1];

                    if (startPoint.Timestamp == endPoint.Timestamp) continue;

                    if (timeStamp >= startPoint.Timestamp && timeStamp < endPoint.Timestamp)
                    {
                        double timePercent = (timeStamp - startPoint.Timestamp) / (endPoint.Timestamp - startPoint.Timestamp);
                        double snapPointX = startPoint.X + timePercent * (endPoint.X - startPoint.X);
                        double snapPointY = startPoint.Y + timePercent * (endPoint.Y - startPoint.Y);
                        double distance, angle;
                        if (trajectory.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                            angle = DistanceCalculator.DirectionAngleGeo(startPoint, endPoint);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                            angle = DistanceCalculator.DirectionAngle(startPoint, endPoint);
                        }

                        double speed = distance / (endPoint.Timestamp - startPoint.Timestamp);

                        snapShotList.Add(new GnssPoint(trajectory.TaxiID, snapPointX, snapPointY, timeStamp, angle, speed, startPoint.ExtraInformation));
                    }
                }
            }

            return snapShotList;
        }

        public IEnumerable<GnssPoint> SnapShotByTime2(int timeStamp, Raster vorinoi)
        {
            List<GnssPoint> snapShotList = new List<GnssPoint>();

            foreach (Trajectory trajectory in TrajectoryList)
            {
                IEnumerable<GnssPoint> trajectoryPoints = trajectory.GetPointEnumerable();
                List<GnssPoint> pointList = trajectoryPoints.ToList();
                for (int i = 0; i < pointList.Count - 1; i++)
                {
                    GnssPoint startPoint = pointList[i];
                    GnssPoint endPoint = pointList[i + 1];

                    if (startPoint.Timestamp == endPoint.Timestamp) continue;

                    if (timeStamp >= startPoint.Timestamp && timeStamp < endPoint.Timestamp)
                    {
                        double timePercent = (timeStamp - startPoint.Timestamp) / (endPoint.Timestamp - startPoint.Timestamp);
                        double snapPointX = startPoint.X + timePercent * (endPoint.X - startPoint.X);
                        double snapPointY = startPoint.Y + timePercent * (endPoint.Y - startPoint.Y);
                        double distance, angle;
                        if (trajectory.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                            angle = DistanceCalculator.DirectionAngleGeo(startPoint, endPoint);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                            angle = DistanceCalculator.DirectionAngle(startPoint, endPoint);
                        }

                        double speed = distance / (endPoint.Timestamp - startPoint.Timestamp);

                        Dictionary<string, object> newProperties = new Dictionary<string, object>();
                        newProperties.Add("Origin", startPoint.ExtraInformation["Origin"]);
                        newProperties.Add("Destination", startPoint.ExtraInformation["Destination"]);
                        newProperties.Add("L2", "-1");

                        int xloc = (int)((snapPointX - vorinoi.RasterXMin) / vorinoi.XScale);
                        int yloc = (int)((snapPointY - (vorinoi.RasterYMax - vorinoi.Rows * vorinoi.YScale)) / vorinoi.YScale);

                        int nowL2 = (int)vorinoi.GetRasterBand(0).At(vorinoi.Rows - 1 - yloc, xloc);
                        if (nowL2.ToString() == startPoint.ExtraInformation["L2"].ToString())
                        {
                            newProperties["L2"] = nowL2.ToString();
                        }

                        if (nowL2.ToString() == endPoint.ExtraInformation["L2"].ToString())
                        {
                            newProperties["L2"] = nowL2.ToString();
                        }

                        snapShotList.Add(new GnssPoint(trajectory.TaxiID, snapPointX, snapPointY, timeStamp, angle, speed, newProperties));
                    }
                }
            }

            return snapShotList;
        }

        public BoundaryBox CalculateBoundaryBox()
        {
            double XMin = Double.MaxValue;
            double XMax = Double.MinValue;
            double YMin = Double.MaxValue;
            double YMax = Double.MinValue;

            foreach (Trajectory oneTrajectory in TrajectoryList)
            {
                for (int i = 0; i < oneTrajectory.PointNumber; i++)
                {
                    GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                    if (gnssPoint.X < XMin)
                    {
                        XMin = gnssPoint.X;
                    }

                    if (gnssPoint.X > XMax)
                    {
                        XMax = gnssPoint.X;
                    }

                    if (gnssPoint.Y < YMin)
                    {
                        YMin = gnssPoint.Y;
                    }

                    if (gnssPoint.Y > YMax)
                    {
                        YMax = gnssPoint.Y;
                    }
                }
            }

            return new BoundaryBox(XMin, YMin, XMax, YMax);
        }

        public IEnumerable<ITrajectory> OriginRegionFilter(ShpPolygon polygon)
        {
            List<Trajectory> result = new List<Trajectory>();

            foreach (Trajectory trajectory in this.TrajectoryList)
            {
                IPoint originPoint = trajectory.GetPointByIndex(0);
                IPoint destPoint = trajectory.GetPointByIndex(-1);

                bool isContainOri = TopoCalculator.IsContain(polygon, new ShpPoint(originPoint.GetX(), originPoint.GetY()));

                bool isContainDest = TopoCalculator.IsContain(polygon, new ShpPoint(destPoint.GetX(), destPoint.GetY()));

                if (isContainOri && !isContainDest)
                {
                    result.Add(trajectory);
                }

            }

            return result;
        }

        public IEnumerable<ITrajectory> DestinationRegionFilter(ShpPolygon polygon)
        {
            List<Trajectory> result = new List<Trajectory>();

            foreach (Trajectory trajectory in this.TrajectoryList)
            {
                IPoint originPoint = trajectory.GetPointByIndex(0);
                IPoint destPoint = trajectory.GetPointByIndex(-1);

                bool isContainOri = TopoCalculator.IsContain(polygon, new ShpPoint(originPoint.GetX(), originPoint.GetY()));

                bool isContainDest = TopoCalculator.IsContain(polygon, new ShpPoint(destPoint.GetX(), destPoint.GetY()));

                if (!isContainOri && isContainDest)
                {
                    result.Add(trajectory);
                }
            }

            return result;
        }

        public IEnumerable<ITrajectory> ODRegionFilter(ShpPolygon polygon)
        {
            IEnumerable<ITrajectory> origin = OriginRegionFilter(polygon);

            IEnumerable<ITrajectory> destination = DestinationRegionFilter(polygon);

            return origin.Union(destination);
        }

        public IEnumerable<ITrajectory> IntersectFilter(ShpPolygon polygon)
        {
            List<Trajectory> result = new List<Trajectory>();

            foreach (Trajectory trajectory in this.TrajectoryList)
            {
                for (int i = 0; i < trajectory.PointNumber; i++)
                {
                    GnssPoint nowPoint = trajectory.GetPointByIndex(i);

                    bool isContain = TopoCalculator.IsContain(polygon, new ShpPoint(nowPoint.GetX(), nowPoint.GetY()));

                    if (isContain)
                    {
                        result.Add(trajectory);
                        break;
                    }
                }
            }

            return result;
        }

        public ITrajectory GetTrajectoryByIndex(int index)
        {
            if (index < -this.TrajectoryNumber || index >= this.TrajectoryNumber) throw new IndexOutOfRangeException();

            if (index < 0) index += this.TrajectoryNumber;

            return this.TrajectoryList[index];
        }

        private HashSet<string> CheckAllExistField()
        {
            HashSet<string> result = new HashSet<string>();
            foreach (Trajectory oneTrajectory in this.TrajectoryList)
            {
                foreach (GnssPoint gnssPoint in oneTrajectory.GetPointEnumerable())
                {
                    foreach (string key in gnssPoint.ExtraInformation.Keys)
                    {
                        result.Add(key);
                    }
                }
            }
            return result;
        }

        public bool ExportToText(string filepath, char separator = ',')
        {
            int trajectoryCount = this.TrajectoryNumber;
            using (StreamWriter sw = new StreamWriter(new FileStream(filepath, FileMode.Create)))
            {
                sw.WriteLine("Stardard ThomasGIS TrajectorySet File");
                sw.WriteLine(trajectoryCount + "\t" + separator.ToString());
                string fields = "ID\tLongitude\tLatitude\tTimestamp\tSpeed\tDirection";
                HashSet<string> otherFields = CheckAllExistField();
                foreach (string fieldName in otherFields)
                {
                    fields += $"\t{fieldName}";
                }
                sw.WriteLine(fields);

                foreach (Trajectory oneTrajectory in this.TrajectoryList)
                {
                    sw.WriteLine(oneTrajectory.GetTaxiID() + "\t" + oneTrajectory.PointNumber);
                    if (oneTrajectory.GetCoordinateSystem() == null)
                    {
                        sw.WriteLine("Unknown");
                    }
                    else
                    {
                        sw.WriteLine(oneTrajectory.GetCoordinateSystem().ExportToWkt());
                    }
                    foreach (GnssPoint gnssPoint in oneTrajectory.GetPointEnumerable())
                    {
                        string pointInfo = String.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", gnssPoint.ID, separator, gnssPoint.X, separator, gnssPoint.Y, separator, gnssPoint.Timestamp, separator, gnssPoint.Speed, separator, gnssPoint.Direction);
                        foreach (string key in otherFields)
                        {
                            if (gnssPoint.ExtraInformation.ContainsKey(key))
                            {
                                pointInfo += $"{separator}{ gnssPoint.ExtraInformation[key].ToString() }";
                            }
                            else
                            {
                                pointInfo += $"{separator}";
                            }
                        }
                        sw.WriteLine(pointInfo);
                    }
                }
                sw.Close();
            }
            return true;
        }

        public bool AllUnion(ITrajectorySet addTrajectorySet)
        {
            this.TrajectoryList.AddRange(addTrajectorySet.GetTrajectoryEnumerable());
            return true;
        }

        public IEnumerable<ITrajectory> GetTrajectoryEnumerable()
        {
            return this.TrajectoryList.AsEnumerable();
        }

        public bool ExtractInOutTrajectory(ShpPolygon polygon, out List<ITrajectory> inTrajectories, out List<ITrajectory> outTrajectories)
        {
            outTrajectories = new List<ITrajectory>();
            inTrajectories = new List<ITrajectory>();

            foreach (Trajectory trajectory in this.TrajectoryList)
            {
                List<GnssPoint> tempPointList = new List<GnssPoint>();
                int flag;

                // 判断初始点的状态，若第一个点在范围内则说明第一段是Out，反之第一段为In
                GnssPoint firstPoint = trajectory.GetPointByIndex(0);
                if (TopoCalculator.IsContain(polygon, new ShpPoint(firstPoint.X, firstPoint.Y)))
                {
                    // 第一个点在范围内，flag设为1
                    flag = 1;
                }
                else
                {
                    // 第一个点不在范围内，flag设为2
                    flag = 2;
                }

                tempPointList.Add(firstPoint);

                for (int i = 1; i < trajectory.GetPointNumber(); i++)
                {
                    GnssPoint nextPoint = trajectory.GetPointByIndex(i);
                    bool isContain = TopoCalculator.IsContain(polygon, new ShpPoint(nextPoint.X, nextPoint.Y));

                    // 上一点在范围内，这一点还在范围内
                    if (isContain && flag == 1)
                    {
                        tempPointList.Add(nextPoint);
                    }
                    // 上一点在范围外，这一点还在范围外
                    else if (!isContain && flag == 2)
                    {
                        tempPointList.Add(nextPoint);
                    }
                    // 上一点在范围外，这一点在范围内，切割成两条
                    else if (isContain && flag == 2)
                    {
                        int maxDistanceIndex = -1;
                        double maxDistance = -1;
                        for (int j = 0; j < tempPointList.Count; j++)
                        {
                            double distance;
                            if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                            {
                                distance = DistanceCalculator.SpatialDistanceGeo(polygon, tempPointList[j]);
                            }
                            else
                            {
                                distance = DistanceCalculator.SpatialDistance(polygon, tempPointList[j]);
                            }

                            if (distance > maxDistance)
                            {
                                maxDistance = distance;
                                maxDistanceIndex = j;
                            }
                        }

                        List<GnssPoint> outTrajectoryPoint = new List<GnssPoint>();
                        List<GnssPoint> inTrajectoryPoint = new List<GnssPoint>();

                        for (int j = 0; j < tempPointList.Count; j++)
                        {
                            if (j <= maxDistanceIndex)
                            {
                                outTrajectoryPoint.Add(tempPointList[j]);
                            }
                            else
                            {
                                inTrajectoryPoint.Add(tempPointList[j]);
                            }
                        }

                        if (outTrajectoryPoint.Count > 1)
                        {
                            outTrajectories.Add(new Trajectory(trajectory.TaxiID, trajectory.TrajectoryCoordinate, outTrajectoryPoint));
                        }

                        if (inTrajectoryPoint.Count > 1)
                        {
                            inTrajectories.Add(new Trajectory(trajectory.TaxiID, trajectory.TrajectoryCoordinate, inTrajectoryPoint));
                        }

                        tempPointList.Clear();
                        tempPointList.Add(nextPoint);
                        flag = 1;
                    }
                    else
                    {
                        tempPointList.Clear();
                        tempPointList.Add(nextPoint);
                        flag = 2;
                    }
                }

                if (tempPointList.Count > 0)
                {
                    int maxDistanceIndex = -1;
                    double maxDistance = -1;
                    for (int j = 0; j < tempPointList.Count; j++)
                    {
                        double distance;
                        if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(polygon, tempPointList[j]);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(polygon, tempPointList[j]);
                        }

                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            maxDistanceIndex = j;
                        }
                    }

                    List<GnssPoint> outTrajectoryPoint = new List<GnssPoint>();

                    for (int j = 0; j < tempPointList.Count; j++)
                    {
                        if (j <= maxDistanceIndex)
                        {
                            outTrajectoryPoint.Add(tempPointList[j]);
                            continue;
                        }
                        break;
                    }

                    if (outTrajectoryPoint.Count > 1)
                    {
                        outTrajectories.Add(new Trajectory(trajectory.TaxiID, trajectory.TrajectoryCoordinate, outTrajectoryPoint));
                    }

                    tempPointList.Clear();
                }
            }

            return true;
        }

        public TrajectorySetStatistics GetStatistics()
        {
            decimal sumTime = 0;
            decimal sumDistance = 0;
            decimal sumSpeed = 0;
            long count = 0;
            double maxTimeStamp = double.MinValue;
            double minTimeStamp = double.MaxValue;

            Dictionary<double, int> timeIntervalDict = new Dictionary<double, int>();
            List<double> distanceList = new List<double>();

            // 采样间隔分布
            foreach (Trajectory oneTrajectory in this.TrajectoryList)
            {
                for (int i = 0; i < oneTrajectory.GetPointNumber() - 1; i++)
                {
                    GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                    GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);
                    double timeInterval = endPoint.Timestamp - startPoint.Timestamp;
                    double distance;
                    if (oneTrajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                    }

                    if (!timeIntervalDict.ContainsKey(timeInterval))
                    {
                        timeIntervalDict.Add(timeInterval, 0);
                    }

                    if (timeInterval != 0)
                    {
                        sumSpeed += Decimal.Parse((distance / timeInterval).ToString("f3"));
                    }

                    maxTimeStamp = Math.Max(endPoint.Timestamp, maxTimeStamp);
                    minTimeStamp = Math.Min(startPoint.Timestamp, minTimeStamp);

                    timeIntervalDict[timeInterval] += 1;
                    distanceList.Add(distance);
                    sumDistance += Decimal.Parse(distance.ToString());
                    sumTime += Decimal.Parse(timeInterval.ToString());
                    count += 1;
                }
            }

            TrajectorySetStatistics result = new TrajectorySetStatistics();
            result.AvgDistance = Convert.ToDouble(Decimal.Divide(sumDistance, count).ToString());
            result.AvgSpeed = Convert.ToDouble(Decimal.Divide(sumSpeed, count).ToString());
            result.AvgTime = Convert.ToDouble(Decimal.Divide(sumTime, count).ToString());

            // 99.9分位数
            int _999PercentIndex = (int)(count * 0.999);
            // 99分位数
            int _99PercentIndex = (int)(count * 0.99);
            // 95分位数
            int _95PercentIndex = (int)(count * 0.95);
            // 90分位数
            int _90PercentIndex = (int)(count * 0.90);
            // 75分位数
            int _75PercentIndex = (int)(count * 0.75);
            // 50分位数
            int _50PercentIndex = (int)(count * 0.50);

            // 时间消耗及其恐怖，先这样吧
            distanceList.Sort();

            // 分位数
            double _999TimeInterval = -1;
            double _99TimeInterval = -1;
            double _95TimeInterval = -1;
            double _90TimeInterval = -1;
            double _75TimeInterval = -1;
            double _50TimeInterval = -1;

            int tempCount = 0;
            List<double> timeIntervalSet = timeIntervalDict.Keys.ToList();
            timeIntervalSet.Sort();
            foreach (double key in timeIntervalSet)
            {
                if (tempCount + timeIntervalDict[key] >= _50PercentIndex && _50TimeInterval < 0)
                {
                    _50TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _75PercentIndex && _75TimeInterval < 0)
                {
                    _75TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _90PercentIndex && _90TimeInterval < 0)
                {
                    _90TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _95PercentIndex && _95TimeInterval < 0)
                {
                    _95TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _99PercentIndex && _99TimeInterval < 0)
                {
                    _99TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _99PercentIndex && _99TimeInterval < 0)
                {
                    _99TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _999PercentIndex && _999TimeInterval < 0)
                {
                    _999TimeInterval = key;
                }

                tempCount += timeIntervalDict[key];
            }

            result.Time50Per = _50TimeInterval;
            result.Time75Per = _75TimeInterval;
            result.Time90Per = _90TimeInterval;
            result.Time95Per = _95TimeInterval;
            result.Time99Per = _99TimeInterval;
            result.Time999Per = _999TimeInterval;

            result.Distance50Per = distanceList[_50PercentIndex];
            result.Distance75Per = distanceList[_75PercentIndex];
            result.Distance90Per = distanceList[_90PercentIndex];
            result.Distance95Per = distanceList[_95PercentIndex];
            result.Distance99Per = distanceList[_99PercentIndex];
            result.Distance999Per = distanceList[_999PercentIndex];

            result.startDateTime = DatetimeCalculator.TimestampToDatetime(minTimeStamp, "yyyy-MM-dd HH:mm:ss");
            result.endDateTime = DatetimeCalculator.TimestampToDatetime(maxTimeStamp, "yyyy-MM-dd HH:mm:ss");

            return result;
        }

        // 统计轨迹的一般性指标分布
        public bool ExportTrajectoryStatistics(string outputFilePath)
        {
            // <=1 1-5 5-10 10-20 20-30 30-60 60-120 120-300 300-600 600-1800 1800-3600 3600-7200 7200-14400 14400-28800 >28800
            long[] timeDataLevel = new long[15];
            // <=1 1-10 10-50 50-100 100-200 200-500 500-1000 1000-2000 2000-5000 5000-10000 10000-20000 20000-50000 50000-100000 100000-500000 >5000000
            long[] distanceDataLevel = new long[15];

            decimal sumTime = 0;
            decimal sumDistance = 0;
            long count = 0;
            double maxTimeInterval = double.MinValue;
            double minTimeInterval = double.MaxValue;
            double maxDistanceInterval = double.MinValue;
            double minDistanceInterval = double.MaxValue;

            Dictionary<double, int> timeIntervalDict = new Dictionary<double, int>();
            List<double> distanceList = new List<double>();

            // 采样间隔分布
            foreach (Trajectory oneTrajectory in this.TrajectoryList)
            {
                for (int i = 0; i < oneTrajectory.GetPointNumber() - 1; i++)
                {
                    GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                    GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);
                    double timeInterval = endPoint.Timestamp - startPoint.Timestamp;
                    double distance;
                    if (oneTrajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                    }

                    if (!timeIntervalDict.ContainsKey(timeInterval))
                    {
                        timeIntervalDict.Add(timeInterval, 0);
                    }
                    timeIntervalDict[timeInterval] += 1;

                    distanceList.Add(distance);

                    maxTimeInterval = Math.Max(maxTimeInterval, timeInterval);
                    minTimeInterval = Math.Min(minTimeInterval, timeInterval);
                    maxDistanceInterval = Math.Max(maxDistanceInterval, distance);
                    minDistanceInterval = Math.Min(minDistanceInterval, distance);
                    sumDistance += Decimal.Parse(distance.ToString());
                    sumTime += Decimal.Parse(timeInterval.ToString());

                    if (timeInterval <= 1 && timeInterval >= 0)
                    {
                        timeDataLevel[0] += 1;
                    }
                    else if (timeInterval <= 5)
                    {
                        timeDataLevel[1] += 1;
                    }
                    else if (timeInterval <= 10)
                    {
                        timeDataLevel[2] += 1;
                    }
                    else if (timeInterval <= 20)
                    {
                        timeDataLevel[3] += 1;
                    }
                    else if (timeInterval <= 30)
                    {
                        timeDataLevel[4] += 1;
                    }
                    else if (timeInterval <= 60)
                    {
                        timeDataLevel[5] += 1;
                    }
                    else if (timeInterval <= 120)
                    {
                        timeDataLevel[6] += 1;
                    }
                    else if (timeInterval <= 300)
                    {
                        timeDataLevel[7] += 1;
                    }
                    else if (timeInterval <= 600)
                    {
                        timeDataLevel[8] += 1;
                    }
                    else if (timeInterval <= 1800)
                    {
                        timeDataLevel[9] += 1;
                    }
                    else if (timeInterval <= 3600)
                    {
                        timeDataLevel[10] += 1;
                    }
                    else if (timeInterval <= 7200)
                    {
                        timeDataLevel[11] += 1;
                    }
                    else if (timeInterval <= 14400)
                    {
                        timeDataLevel[12] += 1;
                    }
                    else if (timeInterval <= 28800)
                    {
                        timeDataLevel[13] += 1;
                    }
                    else if (timeInterval > 28800)
                    {
                        timeDataLevel[14] += 1;
                    }
                    else
                    {
                        throw new Exception("错误编号 TS000：错误的轨迹序列，存在时间逆序轨迹点序列!");
                    }

                    if (distance <= 1)
                    {
                        distanceDataLevel[0] += 1;
                    }
                    else if (distance <= 10)
                    {
                        distanceDataLevel[1] += 1;
                    }
                    else if (distance <= 50)
                    {
                        distanceDataLevel[2] += 1;
                    }
                    else if (distance <= 100)
                    {
                        distanceDataLevel[3] += 1;
                    }
                    else if (distance <= 200)
                    {
                        distanceDataLevel[4] += 1;
                    }
                    else if (distance <= 500)
                    {
                        distanceDataLevel[5] += 1;
                    }
                    else if (distance <= 1000)
                    {
                        distanceDataLevel[6] += 1;
                    }
                    else if (distance <= 2000)
                    {
                        distanceDataLevel[7] += 1;
                    }
                    else if (distance <= 5000)
                    {
                        distanceDataLevel[8] += 1;
                    }
                    else if (distance <= 10000)
                    {
                        distanceDataLevel[9] += 1;
                    }
                    else if (distance <= 20000)
                    {
                        distanceDataLevel[10] += 1;
                    }
                    else if (distance <= 50000)
                    {
                        distanceDataLevel[11] += 1;
                    }
                    else if (distance <= 100000)
                    {
                        distanceDataLevel[12] += 1;
                    }
                    else if (distance <= 500000)
                    {
                        distanceDataLevel[13] += 1;
                    }
                    else
                    {
                        distanceDataLevel[14] += 1;
                    }

                    count += 1;
                }
            }

            // 平均时间间隔和平均轨迹间隔
            decimal averageTimeInterval = Decimal.Divide(sumTime, count);
            decimal averageDistanceInterval = Decimal.Divide(sumDistance, count);

            // 99.9分位数
            int _999PercentIndex = (int)(count * 0.999);
            // 99分位数
            int _99PercentIndex = (int)(count * 0.99);
            // 95分位数
            int _95PercentIndex = (int)(count * 0.95);
            // 90分位数
            int _90PercentIndex = (int)(count * 0.90);
            // 75分位数
            int _75PercentIndex = (int)(count * 0.75);
            // 50分位数
            int _50PercentIndex = (int)(count * 0.50);

            // 时间消耗及其恐怖，先这样吧
            distanceList.Sort();

            // 分位数
            double _999TimeInterval = -1;
            double _99TimeInterval = -1;
            double _95TimeInterval = -1;
            double _90TimeInterval = -1;
            double _75TimeInterval = -1;
            double _50TimeInterval = -1;

            int tempCount = 0;
            List<double> timeIntervalSet = timeIntervalDict.Keys.ToList();
            timeIntervalSet.Sort();
            foreach (double key in timeIntervalSet)
            {
                if (tempCount + timeIntervalDict[key] >= _50PercentIndex && _50TimeInterval < 0)
                {
                    _50TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _75PercentIndex && _75TimeInterval < 0)
                {
                    _75TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _90PercentIndex && _90TimeInterval < 0)
                {
                    _90TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _95PercentIndex && _95TimeInterval < 0)
                {
                    _95TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _99PercentIndex && _99TimeInterval < 0)
                {
                    _99TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _99PercentIndex && _99TimeInterval < 0)
                {
                    _99TimeInterval = key;
                }

                if (tempCount + timeIntervalDict[key] >= _999PercentIndex && _999TimeInterval < 0)
                {
                    _999TimeInterval = key;
                }

                tempCount += timeIntervalDict[key];
            }

            string[] timeTitle = { "<=1", "1-5", "5-10", "10-20", "20-30", "30-60", "60-120", "120-300", "300-600", "600-1800", "1800-3600", "3600-7200", "7200-14400", "14400-28800", ">28800" };
            string[] distanceTitle = { "<=1", "1-10", "10-50", "50-100", "100-200", "200-500", "500-1000", "1000-2000", "2000-5000", "5000-10000", "10000-20000", "20000-50000", "50000-100000", "100000-500000", ">500000" };

            double maxTrajectoryLength = this.TrajectoryList.Max(item => item.GetLength());
            double minTrajectoryLength = this.TrajectoryList.Min(item => item.GetLength());


            using (StreamWriter sw = new StreamWriter(new FileStream(outputFilePath, FileMode.Create)))
            {
                StringBuilder timeDistributionSB = new StringBuilder();
                StringBuilder timeTitleSB = new StringBuilder();
                StringBuilder distanceDistributionSB = new StringBuilder();
                StringBuilder distanceTitleSB = new StringBuilder();

                for (int i = 0; i < 15; i++)
                {
                    timeDistributionSB.Append(timeDataLevel[i]);
                    timeDistributionSB.Append("\t");
                    distanceDistributionSB.Append(distanceDataLevel[i]);
                    distanceDistributionSB.Append("\t");
                    timeTitleSB.Append(timeTitle[i]);
                    timeTitleSB.Append("\t");
                    distanceTitleSB.Append(distanceTitle[i]);
                    distanceTitleSB.Append("\t");
                }

                sw.WriteLine("Gnss Points Statistics");
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("Max Time Interval: " + maxTimeInterval.ToString());
                sw.WriteLine("Min Time Interval: " + minTimeInterval.ToString());
                sw.WriteLine("Average Time Interval: " + averageTimeInterval.ToString());
                sw.WriteLine("Time 99.9% Quantile: " + _999TimeInterval.ToString());
                sw.WriteLine("Time 99% Quantile: " + _99TimeInterval.ToString());
                sw.WriteLine("Time 95% Quantile: " + _95TimeInterval.ToString());
                sw.WriteLine("Time 90% Quantile: " + _90TimeInterval.ToString());
                sw.WriteLine("Time 75% Quantile: " + _75TimeInterval.ToString());
                sw.WriteLine("Time 50% Quantile: " + _50TimeInterval.ToString());
                sw.WriteLine("\n");

                sw.WriteLine("Max Distance Interval: " + maxDistanceInterval.ToString());
                sw.WriteLine("Min Distance Interval: " + minDistanceInterval.ToString());
                sw.WriteLine("Average Distance Interval: " + averageDistanceInterval.ToString());
                sw.WriteLine("Distance 99.9% Quantile: " + distanceList[_999PercentIndex].ToString());
                sw.WriteLine("Distance 99% Quantile: " + distanceList[_99PercentIndex].ToString());
                sw.WriteLine("Distance 95% Quantile: " + distanceList[_95PercentIndex].ToString());
                sw.WriteLine("Distance 90% Quantile: " + distanceList[_90PercentIndex].ToString());
                sw.WriteLine("Distance 75% Quantile: " + distanceList[_75PercentIndex].ToString());
                sw.WriteLine("Distance 50% Quantile: " + distanceList[_50PercentIndex].ToString());
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("Time Interval Distribution Table");
                sw.WriteLine(timeTitleSB.ToString().Trim('\t'));
                sw.WriteLine(timeDistributionSB.ToString().Trim('\t'));
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("Distance Distribution Table");
                sw.WriteLine(distanceTitleSB.ToString().Trim('\t'));
                sw.WriteLine(distanceDistributionSB.ToString().Trim('\t'));
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("\n");

                sw.WriteLine("Trajectory Statistics");
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("Max Trajectory Length: " + maxTrajectoryLength.ToString());
                sw.WriteLine("Min Trajectory Length: " + minTrajectoryLength.ToString());
                sw.WriteLine("-------------------------------------------------");
                sw.WriteLine("\n");
            }

            return true;
        }

        public int GetTrajectoryNumber()
        {
            return this.TrajectoryNumber;
        }
    }
}
