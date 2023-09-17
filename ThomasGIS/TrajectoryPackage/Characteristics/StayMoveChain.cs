using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public class StayMoveChain : IStayMoveChain
    {
        private List<IMove> moveList = new List<IMove>();
        private List<IStay> stayList = new List<IStay>();
        private string trajectoryID;
        private CoordinateBase coordiante;

        public int MoveNumber => this.moveList.Count;
        public int StayNumber => this.stayList.Count;

        public StayMoveChain(ITrajectory inputTrajectory, double distanceTolerate, int minNeighborNumber, int minTimeInterval)
        {
            this.trajectoryID = inputTrajectory.GetTaxiID();

            this.coordiante = inputTrajectory.GetCoordinateSystem();

            List<GnssPoint> tempGnssPointList = new List<GnssPoint>();

            tempGnssPointList.Add(inputTrajectory.GetPointByIndex(0));

            int prevStayEndIndex = 0;

            for (int i = 1; i < inputTrajectory.GetPointNumber(); i++)
            {
                GnssPoint nowPoint = inputTrajectory.GetPointByIndex(i);

                int count = 0;
                for (int j = tempGnssPointList.Count - 1; j >= 0; j--)
                {
                    GnssPoint prevPoint = tempGnssPointList[j];

                    double distance;
                    if (inputTrajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(prevPoint, nowPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(prevPoint, nowPoint);
                    }

                    if (distance < distanceTolerate)
                    {
                        count += 1;
                    }

                    if (count >= minNeighborNumber)
                    {
                        break;
                    }
                }

                if (count >= Math.Min(minNeighborNumber, tempGnssPointList.Count))
                {
                    tempGnssPointList.Add(nowPoint);
                }
                else
                {
                    // 否则检测是否为有效停留点
                    double timeInterval = tempGnssPointList.Last().Timestamp - tempGnssPointList.First().Timestamp;
                    if (timeInterval > minTimeInterval)
                    {
                        List<GnssPoint> movePoints = new List<GnssPoint>();

                        for (int j = prevStayEndIndex; j < i - tempGnssPointList.Count + 1; j++)
                        {
                            movePoints.Add(inputTrajectory.GetPointByIndex(j));
                        }

                        if (movePoints.Count >= 2)
                        {
                            this.moveList.Add(new Move(inputTrajectory.GetTaxiID(), inputTrajectory.GetCoordinateSystem(), movePoints, this.StayNumber - 1, this.StayNumber));
                        }

                        // tempGnssPoint里的点是停留
                        this.stayList.Add(new Stay(inputTrajectory.GetTaxiID(), inputTrajectory.GetCoordinateSystem(), tempGnssPointList, this.MoveNumber - 1, this.MoveNumber, StayAreaType.Rectangle));

                        prevStayEndIndex = i - 1;
                    }

                    tempGnssPointList.Clear();
                    tempGnssPointList.Add(nowPoint);
                }
            }

            // 一遍循环结束后对剩余的保存在tempGnssPointList中的轨迹点进行判断，若由停留则构建一组停留/转移
            if (tempGnssPointList.Count > 1)
            {
                // 否则检测是否为有效停留点
                double timeInterval = tempGnssPointList.Last().Timestamp - tempGnssPointList.First().Timestamp;
                if (timeInterval > minTimeInterval)
                {
                    List<GnssPoint> movePoints = new List<GnssPoint>();

                    for (int j = prevStayEndIndex; j < inputTrajectory.GetPointNumber() - tempGnssPointList.Count + 1; j++)
                    {
                        movePoints.Add(inputTrajectory.GetPointByIndex(j));
                    }

                    this.moveList.Add(new Move(inputTrajectory.GetTaxiID(), inputTrajectory.GetCoordinateSystem(), movePoints, this.StayNumber - 1, this.StayNumber));

                    // tempGnssPoint里的点是停留
                    this.stayList.Add(new Stay(inputTrajectory.GetTaxiID(), inputTrajectory.GetCoordinateSystem(), tempGnssPointList, this.MoveNumber - 1, this.MoveNumber, StayAreaType.Detail));


                    prevStayEndIndex = inputTrajectory.GetPointNumber();
                }

                tempGnssPointList.Clear();
            }

            // 若最终的结尾部分不是停留，则表现为prevStayEndIndex与输入trajectory中的点数不一致
            if (prevStayEndIndex != inputTrajectory.GetPointNumber())
            {
                // 将前一个停留的最后一点作为起点，构建一条转移
                List<GnssPoint> movePoints = new List<GnssPoint>();

                for (int j = prevStayEndIndex; j < inputTrajectory.GetPointNumber(); j++)
                {
                    movePoints.Add(inputTrajectory.GetPointByIndex(j));
                }

                this.moveList.Add(new Move(inputTrajectory.GetTaxiID(), inputTrajectory.GetCoordinateSystem(), movePoints, this.StayNumber - 1, this.StayNumber));
            }

            // 至此全部构建完毕
        }

        public IStay GetStayByIndex(int index)
        {
            if (index < -this.stayList.Count || index >= this.stayList.Count) throw new IndexOutOfRangeException();

            if (index < 0) index += this.stayList.Count;

            return this.stayList[index];
        }

        public IMove GetMoveByIndex(int index)
        {
            if (index < -this.moveList.Count || index >= this.moveList.Count) throw new IndexOutOfRangeException();

            if (index < 0) index += this.moveList.Count;

            return this.moveList[index];
        }

        public IShapefile ExportMoveToShapefile()
        {
            Shapefile newShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);

            newShapefile.AddField("TaxiID", DBFFieldType.Char, 32, 0);
            newShapefile.AddField("StartTime", DBFFieldType.Number, 20, 4);
            newShapefile.AddField("EndTime", DBFFieldType.Number, 20, 4);
            newShapefile.AddField("Length", DBFFieldType.Number, 20, 4);

            foreach (IMove move in this.moveList)
            {
                IShpPolyline newPolyline = new ShpPolyline(move.GetMovePoints());
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add("TaxiID", trajectoryID);
                properties.Add("StartTime", move.GetStartTime());
                properties.Add("EndTime", move.GetEndTime());
                properties.Add("Length", move.GetLength());
                newShapefile.AddFeature(newPolyline, properties);
            }

            newShapefile.SetCoordinateRef(coordiante);

            return newShapefile;
        }

        public IShapefile ExportStayToShapefile()
        {
            Shapefile newShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);

            newShapefile.AddField("TaxiID", DBFFieldType.Char, 32, 0);
            newShapefile.AddField("StartTime", DBFFieldType.Number, 20, 4);
            newShapefile.AddField("EndTime", DBFFieldType.Number, 20, 4);

            foreach (IStay stay in this.stayList)
            {
                IShpPolygon newPolygon = new ShpPolygon(stay.GetPointEnumerable());
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add("TaxiID", trajectoryID);
                properties.Add("StartTime", stay.GetStartTime());
                properties.Add("EndTime", stay.GetEndTime());
                newShapefile.AddFeature(newPolygon, properties);
            }

            newShapefile.SetCoordinateRef(coordiante);

            return newShapefile;
        }
    }
}
