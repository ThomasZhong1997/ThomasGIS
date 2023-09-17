using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using ThomasGIS.Projections;
using ThomasGIS.TrajectoryPackage.Preprocess;

namespace ThomasGIS.TrajectoryPackage.QuickIO
{
    public class AirplaneTrajectorySetCreator : TrajectorySetCreator
    {
        public AirplaneTrajectorySetCreator(BoundaryBox mbr = null) : base(mbr)
        {

        }

        public override void PointFilter(Trajectory oneTrajectory)
        {
            int i = 0;
            while (i < oneTrajectory.PointNumber - 1)
            {
                GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);

                double distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);

                if (distance <= 0.0001)
                {
                    oneTrajectory.RemovePoint(i + 1);
                    continue;
                }

                i++;
            }
        }

        public override GnssPoint Reader(string line)
        {
            string[] items = line.Split(',');
            double X = Convert.ToDouble(items[0]);
            double Y = Convert.ToDouble(items[1]);
            string ID = items[2];
            int timestamp = DatetimeCalculator.DatetimeToTimestamp(items[16], "yyyy-MM-dd HH:mm:ss");
            Dictionary<string, object> extraInfo = new Dictionary<string, object>();
            extraInfo.Add("Origin", items[9]);
            extraInfo.Add("Destination", items[11]);

            GnssPoint newGnssPoint = new GnssPoint(ID, X, Y, timestamp, -1, -1, extraInfo);

            IProjection projection = ProjectionGenerator.MecatorProjection("MyProjection", 0, 0);
            projection.Toward(newGnssPoint);

            if (readerBoundary == null) return newGnssPoint;

            if (newGnssPoint.X < readerBoundary.XMax && newGnssPoint.X > readerBoundary.XMin && newGnssPoint.Y < readerBoundary.YMax && newGnssPoint.Y > readerBoundary.YMin)
            {
                return newGnssPoint;
            }

            return null;
        }

        public override IEnumerable<Trajectory> TrajectorySpliter(Trajectory oneTrajectory)
        {
            return null;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
        }
    }
}
