using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.DataManagement;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public class ShpPolyline3D : ShpPolyline
    {
        public double ZMin;
        public double ZMax;
        public List<double> ZList;
        public double MMin;
        public double MMax;
        public List<double> MList;

        public ShpPolyline3D() : base()
        {
            this.ZList = new List<double>();
            this.MList = new List<double>();
        }

        public ShpPolyline3D(ShpPolyline3D clone) : base()
        {
            this.ZList = new List<double>();
            this.MList = new List<double>();

            for (int i = 0; i < clone.PartNumber; i++)
            {
                this.PartList.Add(clone.PartList[i]);
            }

            this.ZMax = clone.ZMax;
            this.ZMin = clone.ZMin;
            this.MMax = clone.MMax;
            this.MMin = clone.MMin;

            for (int i = 0; i < clone.PointNumber; i++)
            {
                IPoint polylinePoint = clone.PointList[i];
                IPoint newPoint = new Point(polylinePoint);
                double z = clone.ZList[i];
                double m = clone.MList[i];

                this.PointList.Add(newPoint);
                this.ZList.Add(z);
                this.MList.Add(m);
            }

            GetBoundaryBox();
        }

        public ShpPolyline3D(byte[] wkb)
        {
            byte byteOrder = wkb[0];

            ByteArrayReader wkbReader;
            if ((!BitConverter.IsLittleEndian && byteOrder == 0) || (BitConverter.IsLittleEndian && byteOrder == 1))
            {
                wkbReader = new ByteArrayReader(wkb, false);
            }
            else
            {
                wkbReader = new ByteArrayReader(wkb, true);
            }

            byteOrder = wkbReader.ReadByte();
            uint geometryType = wkbReader.ReadUInt();
            uint geometryNumber = wkbReader.ReadUInt();

            if (geometryType == 0x80000002)
            {
                PartList.Add(0);
                for (int i = 0; i < geometryNumber; ++i)
                {
                    double X = wkbReader.ReadDouble();
                    double Y = wkbReader.ReadDouble();
                    double Z = wkbReader.ReadDouble();
                    PointList.Add(new Point(X, Y));
                    ZList.Add(Z);
                    MList.Add(0);
                }
            }

            if (geometryType == 0x80000005)
            {
                for (int i = 0; i < geometryNumber; ++i)
                {
                    byteOrder = wkbReader.ReadByte();
                    geometryType = wkbReader.ReadUInt();
                    uint pointNumber = wkbReader.ReadUInt();

                    if (geometryType != 0x80000002) throw new Exception("Error Occurred When Construct ShpPolyline3D: WKB Error!");

                    PartList.Add(PointNumber);
                    for (int j = 0; j < pointNumber; ++j)
                    {
                        double X = wkbReader.ReadDouble();
                        double Y = wkbReader.ReadDouble();
                        double Z = wkbReader.ReadDouble();
                        PointList.Add(new Point(X, Y));
                        ZList.Add(Z);
                        MList.Add(0);
                    }
                }
            }
        }

        public bool Import(ShpPolyline originPolyline, double startHeight, double endHeight)
        {
            this.AddPart(originPolyline.GetPointEnumerable());
            double length = originPolyline.GetLength()[0];
            ZMax = Math.Max(startHeight, endHeight);
            ZMin = Math.Min(startHeight, endHeight);
            ZList.Add(startHeight);
            MMax = 0;
            MMin = 0;
            MList.Add(0);
            double sumLength = 0;
            for (int i = 1; i < originPolyline.PointNumber - 1; i++)
            {
                sumLength += DistanceCalculator.SpatialDistance(originPolyline.GetPointByIndex(i - 1), originPolyline.GetPointByIndex(i));
                double middleHeight = startHeight + (sumLength / length) * (endHeight - startHeight);
                ZList.Add(middleHeight);
                MList.Add(0);
            }
            ZList.Add(endHeight);
            MList.Add(0);
            return true;
        }

        public override ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.PolylineZ;
        }

        public override string GetGeometryType()
        {
            return "ShpPolyline3D";
        }

        public override int ContentLength()
        {
            return 2 + 16 + 2 + 2 + 2 * PartNumber + 8 * PointNumber + 4 + 4 + 4 * PointNumber + 4 + 4 + 4 * PointNumber;
        }

        public bool AddPart(IEnumerable<IPoint> pointList, IEnumerable<double> zList, IEnumerable<double> mList = null)
        {
            if (pointList.Count() != zList.Count()) return false;

            if (mList != null && zList.Count() != mList.Count()) return false;

            this.PartList.Add(this.PointNumber);
            this.PointList.AddRange(pointList);
            this.ZList.AddRange(zList);
            if (mList != null)
            {
                this.MList.AddRange(mList);
            }
            else
            {
                for (int i = 0; i < pointList.Count(); i++)
                {
                    this.MList.Add(0);
                }
            }

            return true;
        }

        public override IShpGeometryBase Clone()
        {
            return new ShpPolyline3D(this);
        }
    }
}
