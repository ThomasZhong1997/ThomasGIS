using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.DataManagement;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public class ShpPolygon3D : ShpPolygon
    {
        public double ZMin;
        public double ZMax;
        public List<double> ZList;
        public double MMin;
        public double MMax;
        public List<double> MList;

        public ShpPolygon3D() : base()
        {
            this.ZList = new List<double>();
            this.MList = new List<double>();
        }

        public ShpPolygon3D(byte[] wkb) : base()
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

            if (geometryType == 0x80000003)
            {
                for (int i = 0; i < geometryNumber; ++i)
                {
                    PartList.Add(PointNumber);
                    uint pointNumber = wkbReader.ReadUInt();
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

            if (geometryType == 0x80000006)
            {
                for (int i = 0; i < geometryNumber; ++i)
                {
                    byteOrder = wkbReader.ReadByte();
                    geometryType = wkbReader.ReadUInt();
                    uint ringNumber = wkbReader.ReadUInt();

                    if (geometryType != 0x80000003) throw new Exception("Error Occurred When Construct ShpPolygon3D: WKB Error!");

                    for (int j = 0; j < ringNumber; ++j)
                    {
                        PartList.Add(PointNumber);
                        uint pointNumber = wkbReader.ReadUInt();
                        for (int k = 0; k < pointNumber; ++k)
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
        }

        public override ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.PolygonZ;
        }

        public override string GetGeometryType()
        {
            return "ShpPolygon3D";
        }

        public override int ContentLength()
        {
            return 2 + 16 + 2 + 2 + 2 * PartNumber + 8 * PointNumber + 4 + 4 + 4 * PointNumber + 4 + 4 + 4 * PointNumber;
        }

        public ShpPolygon3D(ShpPolygon3D clone) : base()
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
                IPoint polygonPoint = clone.PointList[i];
                IPoint newPoint = new Point(polygonPoint);
                double z = clone.ZList[i];
                double m = clone.MList[i];

                this.PointList.Add(newPoint);
                this.ZList.Add(z);
                this.MList.Add(m);
            }

            GetBoundaryBox();
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
            return new ShpPolygon3D(this);
        }
    }
}
