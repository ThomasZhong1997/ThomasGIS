using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries
{
    public class SingleLine : ISingleLine
    {
        public IPoint StartPoint { get; set; }
        public IPoint EndPoint { get; set; }

        public SingleLine(double sx, double sy, double ex, double ey)
        {
            StartPoint = new Point(sx, sy);
            EndPoint = new Point(ex, ey);
        }

        public SingleLine(IPoint startPoint, IPoint endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("LINESTRING (");
            wktBuilder.Append(StartPoint.GetX());
            wktBuilder.Append(" ");
            wktBuilder.Append(StartPoint.GetY());
            wktBuilder.Append(",");
            wktBuilder.Append(EndPoint.GetX());
            wktBuilder.Append(" ");
            wktBuilder.Append(EndPoint.GetY());
            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public IPoint GetStartPoint()
        {
            return this.StartPoint;
        }

        public IPoint GetEndPoint()
        {
            return this.EndPoint;
        }

        public bool SetStartX(double x)
        {
            this.StartPoint.SetX(x);
            return true;
        }

        public bool SetStartY(double y)
        {
            this.StartPoint.SetY(y);
            return true;
        }

        public bool SetEndX(double x)
        {
            this.EndPoint.SetX(x);
            return true;
        }

        public bool SetEndY(double y)
        {
            this.EndPoint.SetY(y);
            return true;
        }

        public ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.SingleLine;
        }

        public string GetGeometryType()
        {
            return "SingleLine";
        }

        public string GetBaseGeometryType()
        {
            return "SingleLine";
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xMin = Math.Min(StartPoint.GetX(), EndPoint.GetX());
            double xMax = Math.Max(StartPoint.GetX(), EndPoint.GetX());
            double yMin = Math.Min(StartPoint.GetY(), EndPoint.GetY());
            double yMax = Math.Max(StartPoint.GetY(), EndPoint.GetY());
            return new BoundaryBox(xMin, yMin, xMax, yMax);
        }
    }
}
