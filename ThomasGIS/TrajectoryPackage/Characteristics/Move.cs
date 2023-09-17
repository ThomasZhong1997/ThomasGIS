using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public class Move : Trajectory, IMove
    {
        private double startTimestamp;
        private double endTimestamp;

        private int prevStayID;
        private int nextStayID;

        public int PrevStayID => this.prevStayID;
        public int NextStayID => this.nextStayID;

        public Move(string taxiID, CoordinateBase coordinate, List<GnssPoint> gnssPoints, int prevStayID, int nextStayID) : base(taxiID, coordinate, gnssPoints)
        {
            this.startTimestamp = this.GetPointByIndex(0).Timestamp;
            this.endTimestamp = this.GetPointByIndex(-1).Timestamp;
            this.prevStayID = prevStayID;
            this.nextStayID = nextStayID;
        }

        public double GetStartTime()
        {
            return startTimestamp;
        }

        public double GetEndTime()
        {
            return endTimestamp;
        }

        public double GetAverageSpeed()
        {
            double distance = this.GetLength();
            double timeInterval = this.GetEndTime() - this.GetStartTime();
            return distance / timeInterval;
        }

        public IEnumerable<IPoint> GetMovePoints()
        {
            return this.GetPointEnumerable();
        }
    }
}
