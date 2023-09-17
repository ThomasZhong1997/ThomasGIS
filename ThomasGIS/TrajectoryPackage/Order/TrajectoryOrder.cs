using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Order
{
    public enum ThomasGISTransportMode
    {
        FloatCar = 1,
        BUS = 2,
        Metro = 4,
        Bicycle = 8,
        Unknown = 0
    }

    public class TrajectoryOrder : ITrajectoryOrder
    {
        public string TrajectoryID;
        public IPoint StartPoint;
        public IPoint EndPoint;
        public double Distance;
        public double TimeInterval;
        public double Speed;
        public double StartTimestamp;
        public double EndTimestamp;
        public double Price;
        public ThomasGISTransportMode OrderType;
        public CoordinateBase CoordinateSystem;

        Dictionary<string, object> Properties;

        public TrajectoryOrder(string trajectoryID, IPoint startPoint, IPoint endPoint, double startTimestamp, double endTimestamp, CoordinateBase coordinateSystem,double distance = -1, double speed = -1, double price = -1, ThomasGISTransportMode transportMode = ThomasGISTransportMode.Unknown, Dictionary<string, object> properties = null)
        {
            this.TrajectoryID = trajectoryID;
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
            this.Distance = distance;
            this.TimeInterval = endTimestamp - startTimestamp;
            this.StartTimestamp = startTimestamp;
            this.EndTimestamp = endTimestamp;
            this.Speed = speed;
            this.OrderType = ThomasGISTransportMode.Unknown;
            this.Price = price;
            this.CoordinateSystem = coordinateSystem;

            this.Properties = new Dictionary<string, object>();
            if (properties != null)
            {
                foreach (string key in properties.Keys)
                {
                    this.Properties.Add(key, properties[key]);
                }
            }
        }

        public string ExportTitle(char separator)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ID,StartLongitude,StartLatitude,StartTimestamp,EndLongitude,EndLatitude,EndTimestamp,Speed,Price,");
            string outputString = sb.ToString().Replace(',', separator);
            sb.Clear();
            sb.Append(outputString);
            foreach (string key in this.Properties.Keys)
            {
                sb.Append(key);
                sb.Append(separator);
            }
            return sb.ToString().Trim(separator);
        }

        public string ExportString(char separator)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.TrajectoryID);
            sb.Append(separator);
            sb.Append(this.StartPoint.GetX());
            sb.Append(separator);
            sb.Append(this.StartPoint.GetY());
            sb.Append(separator);
            sb.Append(this.StartTimestamp);
            sb.Append(separator);
            sb.Append(this.EndPoint.GetX());
            sb.Append(separator);
            sb.Append(this.EndPoint.GetY());
            sb.Append(separator);
            sb.Append(this.EndTimestamp);
            sb.Append(separator);
            sb.Append(this.Speed);
            sb.Append(separator);
            sb.Append(this.Price);
            sb.Append(separator);
            foreach (string key in this.Properties.Keys)
            {
                sb.Append(this.Properties[key]);
                sb.Append(separator);
            }
            return sb.ToString().Trim(separator);
        }

        public CoordinateBase GetCoordinateSystem()
        {
            return this.CoordinateSystem;
        }

        public IPoint GetStartPoint()
        {
            return StartPoint;
        }

        public IPoint GetEndPoint()
        {
            return EndPoint;
        }

        public bool SetCoordinateSystem(CoordinateBase coordinateSystem)
        {
            this.CoordinateSystem = coordinateSystem;
            return true;
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xmin = Math.Min(this.StartPoint.GetX(), this.EndPoint.GetX());
            double xmax = Math.Max(this.StartPoint.GetX(), this.EndPoint.GetX());
            double ymin = Math.Min(this.StartPoint.GetY(), this.EndPoint.GetY());
            double ymax = Math.Max(this.StartPoint.GetY(), this.EndPoint.GetY());
            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }
    }
}
