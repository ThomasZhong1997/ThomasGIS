using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Grids.GeoTiff;
using ThomasGIS.Grids;
using System.IO;

namespace ThomasGIS.Grids.Basic
{
    public class Raster : IRaster
    {
        public int Rows { get; }
        public int Cols { get; }

        private RasterDataType rasterDataType;

        private Point LeftTopPoint = new Point(0, 0);

        private double xScale = 1;

        private double yScale = 1;

        public double RasterXMin => LeftTopPoint.X;

        public double RasterYMax => LeftTopPoint.Y;

        public double XScale => xScale;

        public double YScale => yScale;

        public BoundaryBox GetBoundaryBox()
        {
            double xmin = this.RasterXMin;
            double ymax = this.RasterYMax;
            double xScale = this.XScale;
            double yScale = this.YScale;
            int rows = this.Rows;
            int cols = this.Cols;
            double xmax = xmin + cols * XScale;
            double ymin = ymax - rows * YScale;
            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }

        // 设置地理变换参数
        public bool SetGeoTransform(double xMin, double yMax, double xScale, double yScale)
        {
            this.LeftTopPoint.X = xMin;
            this.LeftTopPoint.Y = yMax;
            this.xScale = xScale;
            this.yScale = yScale;
            return true;
        }

        public double[] GetGeoTransform()
        {
            double[] result = new double[6];
            result[0] = this.LeftTopPoint.X;
            result[1] = this.xScale;
            result[2] = 0;
            result[3] = this.LeftTopPoint.Y;
            result[4] = 0;
            result[5] = this.YScale;

            return result;
        }

        public int RasterBandNumber => bandList.Count;

        private List<IRasterBand> bandList;

        private CoordinateBase rasterCoordinate;

        public CoordinateBase GetCoordinateSystem()
        {
            return rasterCoordinate;
        }

        public bool SetCoordinateSystem(CoordinateBase coordinateSystem)
        {
            this.rasterCoordinate = coordinateSystem;
            return true;
        }

        public Raster(int rows, int cols, RasterDataType type, CoordinateBase coordinateBase = null)
        {
            this.Rows = rows;
            this.Cols = cols;
            this.rasterDataType = type;
            this.bandList = new List<IRasterBand>();
            this.rasterCoordinate = coordinateBase;
        }

        public bool AddRasterBand(IRasterBand rasterBand)
        {
            if (rasterBand.GetRows() == this.Rows && rasterBand.GetCols() == this.Cols)
            {
                bandList.Add(rasterBand);
                return true;
            }

            return false;
        }

        public bool RemoveRasterBand(int index)
        {
            if (index >= RasterBandNumber || index < 0)
            {
                return false;
            }

            bandList.RemoveAt(index);
            return true;
        }

        public IRasterBand GetRasterBand(int index)
        {
            if (index >= RasterBandNumber || index < 0)
            {
                return null;
            }

            return bandList[index];
        }

        private Dictionary<int, TiffDE> ParseDEList(RasterBandDesc rasterBandDesc, GeoKeysDesc geoKeysDesc, double[] tiePointsAttributes, double[] scaleAttributes)
        {
            Dictionary<int, TiffDE> result = new Dictionary<int, TiffDE>();

            // 第一个Ox0100 图像宽度
            if (rasterBandDesc.width != -1)
            {
                byte[] innerData = new byte[4];
                byte[] widthBytes = BitConverter.GetBytes((short)rasterBandDesc.width);
                innerData[0] = widthBytes[0];
                innerData[1] = widthBytes[1];
                result.Add(0x0100, new TiffDE(0x0100, 3, 1, innerData));
            }

            // 第二个Ox0101 图像宽度
            if (rasterBandDesc.height != -1)
            {
                byte[] innerData = new byte[4];
                byte[] heightBytes = BitConverter.GetBytes((short)rasterBandDesc.height);
                innerData[0] = heightBytes[0];
                innerData[1] = heightBytes[1];
                result.Add(0x0101, new TiffDE(0x0101, 3, 1, innerData));
            }

            // 第三个Ox0102
            if (rasterBandDesc.colorDepth != null)
            {
                byte[] innerData;

                if (rasterBandDesc.colorDepth.Count < 2)
                {
                    innerData = new byte[4];
                }
                else
                {
                    innerData = new byte[2 * rasterBandDesc.colorDepth.Count];
                }

                for (int i = 0; i < rasterBandDesc.colorDepth.Count; i++)
                {
                    byte[] colorDepthBytes = BitConverter.GetBytes((short)rasterBandDesc.colorDepth[i]);
                    innerData[i * 2 + 0] = colorDepthBytes[0];
                    innerData[i * 2 + 1] = colorDepthBytes[1];
                }

                result.Add(0x0102, new TiffDE(0x0102, 3, rasterBandDesc.colorDepth.Count, innerData));
            }

            if (rasterBandDesc.zipFlag != -1)
            {
                byte[] innerData = new byte[4];
                byte[] zipBytes = BitConverter.GetBytes((short)rasterBandDesc.zipFlag);
                innerData[0] = zipBytes[0];
                innerData[1] = zipBytes[1];
                result.Add(0x0103, new TiffDE(0x0103, 3, 1, innerData));
            }

            if (rasterBandDesc.reverseFalg != -1)
            {
                byte[] innerData = new byte[4];
                byte[] reverseBytes = BitConverter.GetBytes((short)rasterBandDesc.reverseFalg);
                innerData[0] = reverseBytes[0];
                innerData[1] = reverseBytes[1];
                result.Add(0x0106, new TiffDE(0x0106, 3, 1, innerData));
            }

            if (rasterBandDesc.samplesPerPixel != -1)
            {
                byte[] innerData = new byte[4];
                byte[] bandsBytes = BitConverter.GetBytes((short)rasterBandDesc.samplesPerPixel);
                innerData[0] = bandsBytes[0];
                innerData[1] = bandsBytes[1];
                result.Add(0x0115, new TiffDE(0x0115, 3, 1, innerData));
            }

            if (rasterBandDesc.horizontalOffset_1 != -1 && rasterBandDesc.horizontalOffset_2 != -1)
            {
                byte[] innerData = new byte[8];
                byte[] part1 = BitConverter.GetBytes(rasterBandDesc.horizontalOffset_1);
                byte[] part2 = BitConverter.GetBytes(rasterBandDesc.horizontalOffset_2);
                for (int i = 0; i < 4; i++)
                {
                    innerData[i] = part1[i];
                    innerData[4 + i] = part2[i];
                }
                result.Add(0x011A, new TiffDE(0x011A, 5, 1, innerData));
            }

            if (rasterBandDesc.verticalOffset_1 != -1 && rasterBandDesc.verticalOffset_2 != -1)
            {
                byte[] innerData = new byte[8];
                byte[] part1 = BitConverter.GetBytes(rasterBandDesc.verticalOffset_1);
                byte[] part2 = BitConverter.GetBytes(rasterBandDesc.verticalOffset_2);
                for (int i = 0; i < 4; i++)
                {
                    innerData[i] = part1[i];
                    innerData[4 + i] = part2[i];
                }
                result.Add(0x011B, new TiffDE(0x011B, 5, 1, innerData));
            }

            if (rasterBandDesc.RGB != -1)
            {
                byte[] innerData = new byte[4];
                byte[] tileHeightBytes = BitConverter.GetBytes((short)rasterBandDesc.RGB);
                innerData[0] = tileHeightBytes[0];
                innerData[1] = tileHeightBytes[1];
                result.Add(284, new TiffDE(284, 3, 1, innerData));
            }

            if (rasterBandDesc.software != "")
            {
                byte[] innerData = Encoding.ASCII.GetBytes(rasterBandDesc.software);
                result.Add(0x0131, new TiffDE(0x0131, 2, innerData.Length, innerData));
            }

            if (rasterBandDesc.time != "")
            {
                byte[] innerData = Encoding.ASCII.GetBytes(rasterBandDesc.time);
                result.Add(0x0132, new TiffDE(0x0132, 2, innerData.Length, innerData));
            }

            if (rasterBandDesc.tileWidth != -1)
            {
                byte[] innerData = new byte[4];
                byte[] tileWidthBytes = BitConverter.GetBytes((short)rasterBandDesc.tileWidth);
                innerData[0] = tileWidthBytes[0];
                innerData[1] = tileWidthBytes[1];
                result.Add(0x0142, new TiffDE(0x0142, 3, 1, innerData));
            }

            if (rasterBandDesc.tileHeight != -1)
            {
                byte[] innerData = new byte[4];
                byte[] tileHeightBytes = BitConverter.GetBytes((short)rasterBandDesc.tileHeight);
                innerData[0] = tileHeightBytes[0];
                innerData[1] = tileHeightBytes[1];
                result.Add(0x0143, new TiffDE(0x0143, 3, 1, innerData));
            }


            // 偏移量现在还不知道怎么安排，先不着急，后面再填
            if (rasterBandDesc.tileOffsets.Count != 0)
            {
                byte[] innerData = new byte[rasterBandDesc.tileOffsets.Count * 4];
                result.Add(0x0144, new TiffDE(0x0144, 4, rasterBandDesc.tileOffsets.Count, innerData));
            }

            if (rasterBandDesc.tileByteCounts.Count != 0)
            {
                byte[] innerData = new byte[rasterBandDesc.tileByteCounts.Count * 4];
                result.Add(0x0145, new TiffDE(0x0145, 4, rasterBandDesc.tileByteCounts.Count, innerData));
            }

            if (rasterBandDesc.samplesPerPixel > 3)
            {
                int extraNumber = rasterBandDesc.samplesPerPixel - 1;
                if (extraNumber > 2)
                {
                    byte[] innerData = new byte[2 * extraNumber];
                    result.Add(338, new TiffDE(338, 3, extraNumber, innerData));
                }
                else
                {
                    result.Add(338, new TiffDE(338, 3, extraNumber, new byte[4]));
                }
            }

            // 第N个 339
            if (rasterBandDesc.sampleFormat != null)
            {
                byte[] innerData;
                if (rasterBandDesc.sampleFormat.Count < 2)
                {
                    innerData = new byte[4];
                }
                else
                {
                    innerData = new byte[2 * rasterBandDesc.sampleFormat.Count];
                }

                for (int i = 0; i < rasterBandDesc.sampleFormat.Count; i++)
                {
                    byte[] colorDepthBytes = BitConverter.GetBytes((short)rasterBandDesc.sampleFormat[i]);
                    innerData[i * 2 + 0] = colorDepthBytes[0];
                    innerData[i * 2 + 1] = colorDepthBytes[1];
                }

                result.Add(339, new TiffDE(339, 3, rasterBandDesc.sampleFormat.Count, innerData));
            }

            if (scaleAttributes != null)
            {
                byte[] innerData = new byte[scaleAttributes.Length * 8];
                for (int i = 0; i < 3; i++)
                {
                    byte[] valueBytes = BitConverter.GetBytes(scaleAttributes[i]);
                    for (int j = 0; j < 8; j++)
                    {
                        innerData[i * 8 + j] = valueBytes[j];
                    }
                }
                result.Add(33550, new TiffDE(33550, 12, scaleAttributes.Length, innerData));
            }
            else
            {
                byte[] innerData = new byte[scaleAttributes.Length * 8];
                for (int i = 0; i < 2; i++)
                {
                    byte[] valueBytes = BitConverter.GetBytes((double)1);
                    for (int j = 0; j < 8; j++)
                    {
                        innerData[i * 8 + j] = valueBytes[j];
                    }
                }
                result.Add(33550, new TiffDE(33550, 12, scaleAttributes.Length, innerData));
            }

            if (tiePointsAttributes != null)
            {
                byte[] innerData = new byte[tiePointsAttributes.Length * 8];
                for (int i = 0; i < 6; i++)
                {
                    byte[] valueBytes = BitConverter.GetBytes(tiePointsAttributes[i]);
                    for (int j = 0; j < 8; j++)
                    {
                        innerData[i * 8 + j] = valueBytes[j];
                    }
                }
                result.Add(33922, new TiffDE(33922, 12, tiePointsAttributes.Length, innerData));
            }
            else
            {
                byte[] innerData = new byte[tiePointsAttributes.Length * 8];
                result.Add(33922, new TiffDE(33922, 12, tiePointsAttributes.Length, innerData));
            }



            if (geoKeysDesc != null)
            {
                int usefulGeoKeyCount = 0;

                List<byte> geoDoubleParamsInnerData = new List<byte>();
                List<byte> geoASCIIParamsInnerData = new List<byte>();
                List<byte> geoKeyDictionaryInnerData = new List<byte>();

                if (geoKeysDesc.GTModelTypeGeoKey != -1)
                {
                    UInt16 geoKeyID = 1024;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GTModelTypeGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));
                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GTRasterTypeGeoKey != -1)
                {
                    UInt16 geoKeyID = 1025;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GTRasterTypeGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));
                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GTCitationGeoKey != "")
                {
                    UInt16 geoKeyID = 1026;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34737;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = (ushort)geoKeysDesc.GTCitationGeoKey.Length;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoASCIIParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoASCIIParamsInnerData.AddRange(Encoding.ASCII.GetBytes(geoKeysDesc.GTCitationGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogGeodeticCRSGeoKey != -1)
                {
                    UInt16 geoKeyID = 2048;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogGeodeticCRSGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogCitationGeoKey != "")
                {
                    UInt16 geoKeyID = 2049;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34737;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = (ushort)geoKeysDesc.GeogCitationGeoKey.Length;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoASCIIParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoASCIIParamsInnerData.AddRange(Encoding.ASCII.GetBytes(geoKeysDesc.GeogCitationGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogGeodeticDatumGeoKey != -1)
                {
                    UInt16 geoKeyID = 2050;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogGeodeticDatumGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogPrimeMeridianGeoKey != -1)
                {
                    UInt16 geoKeyID = 2051;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogPrimeMeridianGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogLinearUnitsGeoKey != -1)
                {
                    UInt16 geoKeyID = 2052;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogLinearUnitsGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogLinearUnitSizeGeoKey != -1)
                {
                    UInt16 geoKeyID = 2053;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.GeogLinearUnitSizeGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogAngularUnitsGeoKey != -1)
                {
                    UInt16 geoKeyID = 2054;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogAngularUnitsGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogAngularUnitSizeGeoKey != -1)
                {
                    UInt16 geoKeyID = 2055;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.GeogAngularUnitSizeGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogEllipsoidGeoKey != -1)
                {
                    UInt16 geoKeyID = 2056;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogEllipsoidGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.EllipsoidSemiMajorAxisGeoKey != -1)
                {
                    UInt16 geoKeyID = 2057;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.EllipsoidSemiMajorAxisGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.EllipsoidSemiMinorAxisGeoKey != -1)
                {
                    UInt16 geoKeyID = 2058;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.EllipsoidSemiMinorAxisGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.EllipsoidInvFlatteningGeoKey != -1)
                {
                    UInt16 geoKeyID = 2059;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.EllipsoidInvFlatteningGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogAzimuthUnitGeoKey != -1)
                {
                    UInt16 geoKeyID = 2060;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.GeogAzimuthUnitGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.GeogPrimeMeridianLongitudeGeoKey != -1)
                {
                    UInt16 geoKeyID = 2061;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.GeogPrimeMeridianLongitudeGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjectedCRSGeoKey != -1)
                {
                    UInt16 geoKeyID = 3072;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.ProjectedCRSGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjectedCitationGeoKey != "")
                {
                    UInt16 geoKeyID = 3073;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34737;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = (ushort)geoKeysDesc.ProjectedCitationGeoKey.Length;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoASCIIParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoASCIIParamsInnerData.AddRange(Encoding.ASCII.GetBytes(geoKeysDesc.ProjectedCitationGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjectionGeoKey != -1)
                {
                    UInt16 geoKeyID = 3074;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.ProjectionGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjectionMethodGeoKey != -1)
                {
                    UInt16 geoKeyID = 3075;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.ProjectionMethodGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjLinearUnitGeoKey != -1)
                {
                    UInt16 geoKeyID = 3076;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.ProjLinearUnitGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjLinearUnitSizeGeoKey != -1)
                {
                    UInt16 geoKeyID = 3077;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjLinearUnitSizeGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjStandardParallel_1 != -1)
                {
                    UInt16 geoKeyID = 3078;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjStandardParallel_1));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjStandardParallel_2 != -1)
                {
                    UInt16 geoKeyID = 3079;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjStandardParallel_2));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjNaturalOriginLongitude != -1)
                {
                    UInt16 geoKeyID = 3080;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjNaturalOriginLongitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjNaturalOriginLatitude != -1)
                {
                    UInt16 geoKeyID = 3081;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjNaturalOriginLatitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseEasting != -1)
                {
                    UInt16 geoKeyID = 3082;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseEasting));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseNorthing != -1)
                {
                    UInt16 geoKeyID = 3083;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseNorthing));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseOriginLongitude != -1)
                {
                    UInt16 geoKeyID = 3084;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseOriginLongitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseOriginLatitude != -1)
                {
                    UInt16 geoKeyID = 3085;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseOriginLatitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseOriginEasting != -1)
                {
                    UInt16 geoKeyID = 3086;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseOriginEasting));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjFalseOriginNorthing != -1)
                {
                    UInt16 geoKeyID = 3087;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjFalseOriginNorthing));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjCenterLongitude != -1)
                {
                    UInt16 geoKeyID = 3088;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjCenterLongitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjCenterLatitude != -1)
                {
                    UInt16 geoKeyID = 3089;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjCenterLatitude));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjCenterEasting != -1)
                {
                    UInt16 geoKeyID = 3090;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjCenterEasting));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjCenterNorthing != -1)
                {
                    UInt16 geoKeyID = 3091;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjCenterNorthing));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjScaleAtNaturalOrigin != -1)
                {
                    UInt16 geoKeyID = 3092;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjScaleAtNaturalOrigin));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjScaleAtCenter != -1)
                {
                    UInt16 geoKeyID = 3093;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjScaleAtCenter));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjProjAzimuthAngleGeoKey != -1)
                {
                    UInt16 geoKeyID = 3094;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjProjAzimuthAngleGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.ProjProjAzimuthAngleGeoKey != -1)
                {
                    UInt16 geoKeyID = 3095;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34736;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoDoubleParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoDoubleParamsInnerData.AddRange(BitConverter.GetBytes(geoKeysDesc.ProjStraightVerticalPole));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.VerticalGeoKey != -1)
                {
                    UInt16 geoKeyID = 4096;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.VerticalGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.VerticalCitationGeoKey != "")
                {
                    UInt16 geoKeyID = 4097;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 34737;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = (ushort)geoKeysDesc.VerticalCitationGeoKey.Length;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoASCIIParamsInnerData.Count;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    geoASCIIParamsInnerData.AddRange(Encoding.ASCII.GetBytes(geoKeysDesc.VerticalCitationGeoKey));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.VerticalDatumGeoKey != -1)
                {
                    UInt16 geoKeyID = 4098;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.VerticalDatumGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                if (geoKeysDesc.VerticalUnitSizeGeoKey != -1)
                {
                    UInt16 geoKeyID = 4099;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(geoKeyID));
                    UInt16 tiffTagLocation = 0;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(tiffTagLocation));
                    UInt16 count = 1;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(count));
                    UInt16 keyValue = (ushort)geoKeysDesc.VerticalUnitSizeGeoKey;
                    geoKeyDictionaryInnerData.AddRange(BitConverter.GetBytes(keyValue));

                    usefulGeoKeyCount++;
                }

                // 加个头
                UInt16 version = 1;
                UInt16 revision = 1;
                UInt16 minor = 0;
                UInt16 numberOfKeys = (ushort)usefulGeoKeyCount;

                List<byte> header = new List<byte>();
                header.AddRange(BitConverter.GetBytes(version));
                header.AddRange(BitConverter.GetBytes(revision));
                header.AddRange(BitConverter.GetBytes(minor));
                header.AddRange(BitConverter.GetBytes(numberOfKeys));

                header.AddRange(geoKeyDictionaryInnerData);

                result.Add(34735, new TiffDE(34735, 3, 4 * (usefulGeoKeyCount + 1), header.ToArray()));
                if (geoDoubleParamsInnerData.Count != 0)
                {
                    result.Add(34736, new TiffDE(34736, 12, geoDoubleParamsInnerData.Count / 8, geoDoubleParamsInnerData.ToArray()));
                }
                if (geoASCIIParamsInnerData.Count != 0)
                {
                    result.Add(34737, new TiffDE(34737, 2, geoASCIIParamsInnerData.Count, geoASCIIParamsInnerData.ToArray()));
                }
            }

            return result;
        }

        // 只默认输出为不压缩格式的，以Tile存储的GeoTiff格式
        public bool ExportToGTiff(string filepath)
        {
            RasterBandDesc desc = new RasterBandDesc();

            // 输出默认不压缩
            desc.zipFlag = 1;

            // 图像是否反色
            desc.reverseFalg = 1;

            // 图像长度为图像长度
            desc.width = this.Cols;

            // 图像宽度为图像宽度
            desc.height = this.Rows;

            // 图像深度
            desc.colorDepth = new List<int>();
            switch (this.rasterDataType)
            {
                case RasterDataType.INT8:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(8);
                        desc.sampleFormat.Add(2);
                    }
                    break;
                case RasterDataType.UINT8:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(8);
                        desc.sampleFormat.Add(1);
                    }
                    break;
                case RasterDataType.INT16:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(16);
                        desc.sampleFormat.Add(2);
                    }
                    break;
                case RasterDataType.UINT16:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(16);
                        desc.sampleFormat.Add(1);
                    }
                    break;
                case RasterDataType.INT32:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(32);
                        desc.sampleFormat.Add(2);
                    }
                    break;
                case RasterDataType.UINT32:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(32);
                        desc.sampleFormat.Add(1);
                    }
                    break;
                case RasterDataType.FLOAT32:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(32);
                        desc.sampleFormat.Add(3);
                    }
                    break;
                case RasterDataType.DOUBLE:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(64);
                        desc.sampleFormat.Add(3);
                    }
                    break;
                case RasterDataType.LONG:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(64);
                        desc.sampleFormat.Add(2);
                    }
                    break;
                case RasterDataType.ULONG:
                    for (int i = 0; i < this.RasterBandNumber; i++)
                    {
                        desc.colorDepth.Add(64);
                        desc.sampleFormat.Add(1);
                    }
                    break;
                default:
                    throw new Exception("无法识别的数据类型！");
            }

            // 图像的水平分辨率
            desc.horizontalOffset_1 = 3000000;
            desc.horizontalOffset_2 = 10000;

            // 图像的垂直分辨率
            desc.verticalOffset_1 = 3000000;
            desc.verticalOffset_2 = 10000;

            // 图像的波段
            desc.samplesPerPixel = this.RasterBandNumber;

            // 每个图像块的高度与宽度
            desc.tileHeight = Convert.ToInt32(Configuration.GetConfiguration("grid.geotiff.output.tileheight"));
            desc.tileWidth = Convert.ToInt32(Configuration.GetConfiguration("grid.geotiff.output.tilewidth"));

            // 生成图像的软件
            desc.software = "ThomasGIS";

            desc.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // tile的横向数量与纵向数量
            int tileWidthCount = (int)(desc.width / desc.tileWidth) + 1;
            int tileHeightCount = (int)(desc.height / desc.tileHeight) + 1;

            // tile的总数量
            int tileNumber = tileHeightCount * tileWidthCount;

            // tile的偏移量与数据量
            desc.tileOffsets = new List<int>();
            desc.tileByteCounts = new List<int>();
            for (int i = 0; i < tileNumber; i++)
            {
                desc.tileOffsets.Add(0);
                desc.tileByteCounts.Add(0);
            }

            double[] tiePointsAttributes = new double[6] { 0, 0, 0, 0, 0, 0 };
            tiePointsAttributes[3] = this.RasterXMin;
            tiePointsAttributes[4] = this.RasterYMax;

            double[] scaleAttributes = new double[3] { 0, 0, 0 };
            scaleAttributes[0] = this.XScale;
            scaleAttributes[1] = this.YScale;

            GeoKeysDesc geoKeysDesc = null;
            // 构成GeoKeyDesc
            if (rasterCoordinate != null)
            {
                geoKeysDesc = ParseCoordinateToGeoKeysDesc(rasterCoordinate);
            }

            Dictionary<int, TiffDE> DEList = ParseDEList(desc, geoKeysDesc, tiePointsAttributes, scaleAttributes);

            List<byte> imageDataOffset = new List<byte>();
            List<byte> imageDataByteSize = new List<byte>();

            List<byte>[] tilesByteList = new List<byte>[tileNumber];

            for (int i = 0; i < tileHeightCount; i++)
            {
                for (int j = 0; j < tileWidthCount; j++)
                {
                    int startHeightIndex = i * desc.tileHeight;
                    int startWidthIndex = j * desc.tileWidth;
                    List<byte> oneTileByteList = new List<byte>();

                    for (int m = startHeightIndex; m < startHeightIndex + desc.tileHeight; m++)
                    {
                        for (int n = startWidthIndex; n < startWidthIndex + desc.tileWidth; n++)
                        {
                            for (int q = 0; q < desc.samplesPerPixel; q++)
                            {
                                double value = this.GetRasterBand(q).At(m, n);

                                switch (this.rasterDataType)
                                {
                                    case RasterDataType.UINT8:
                                        oneTileByteList.Add((byte)value);
                                        break;
                                    case RasterDataType.UINT16:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((ushort)value));
                                        break;
                                    case RasterDataType.UINT32:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((uint)value));
                                        break;
                                    case RasterDataType.INT8:
                                        oneTileByteList.Add((byte)(sbyte)value);
                                        break;
                                    case RasterDataType.INT16:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((short)value));
                                        break;
                                    case RasterDataType.INT32:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((int)value));
                                        break;
                                    case RasterDataType.FLOAT32:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((float)value));
                                        break;
                                    case RasterDataType.DOUBLE:
                                        oneTileByteList.AddRange(BitConverter.GetBytes(value));
                                        break;
                                    case RasterDataType.LONG:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((long)value));
                                        break;
                                    case RasterDataType.ULONG:
                                        oneTileByteList.AddRange(BitConverter.GetBytes((ulong)value));
                                        break;
                                }
                            }
                        }
                    }

                    switch (desc.zipFlag)
                    {
                        case 1:
                            break;
                        case 5:
                            throw new Exception("暂不支持LZW方法的压缩，请等待后续更新！");
                        default:
                            throw new Exception("当前仅支持不压缩的GeoTiff输出！");
                    }

                    tilesByteList[i * tileWidthCount + j] = oneTileByteList;
                }
            }

            int headerOffset = 8 + 2 + DEList.Count * 12 + 4;

            // 设定每个Tile的写入位置与长度
            for (int i = 0; i < tileNumber; i++)
            {
                imageDataOffset.AddRange(BitConverter.GetBytes(headerOffset));
                imageDataByteSize.AddRange(BitConverter.GetBytes(tilesByteList[i].Count));
                headerOffset += tilesByteList[i].Count;
            }

            // 更新到DEList中去
            DEList[0x0144].Innerdata = imageDataOffset.ToArray();
            DEList[0x0145].Innerdata = imageDataByteSize.ToArray();
            // GC掉
            imageDataOffset = null;
            imageDataByteSize = null;

            // 然后开始更新keyList中的溢出的指针的位置们
            for (int i = 0; i < 65535; i++)
            {
                if (DEList.ContainsKey(i))
                {
                    if (DEList[i].Innerdata.Length > 4)
                    {
                        DEList[i].Pointer = headerOffset;
                        headerOffset += DEList[i].Innerdata.Length;
                    }
                }
            }

            using (BinaryWriter bw = new BinaryWriter(new FileStream(filepath, FileMode.Create)))
            {
                // 顺序标志位
                bw.Write((byte)73);
                bw.Write((byte)73);

                // TIFF标志位
                bw.Write((byte)42);
                bw.Write((byte)0);

                // 第一个IFH的偏移
                bw.Write((int)8);

                // 第一个IFH中包含的DE的数量
                bw.Write((ushort)DEList.Count);

                // 所有的DE
                foreach (int key in DEList.Keys)
                {
                    TiffDE tempDE = DEList[key];
                    bw.Write((ushort)tempDE.ID);
                    bw.Write((ushort)tempDE.DataType);
                    bw.Write((int)tempDE.DataNumber);
                    if (tempDE.Pointer == -1)
                    {
                        bw.Write(tempDE.Innerdata);
                    }
                    else
                    {
                        bw.Write((int)tempDE.Pointer);
                    }
                }

                bw.Write((int)0);

                // 所有的Tile
                for (int i = 0; i < tileNumber; i++)
                {
                    bw.Write(tilesByteList[i].ToArray());
                }

                // 所有的DE中的数据
                for (int i = 0; i < 65535; i++)
                {
                    if (DEList.ContainsKey(i))
                    {
                        if (DEList[i].Pointer != -1)
                        {
                            bw.Write(DEList[i].Innerdata);
                        }
                    }
                }

                bw.Close();
            }

            return true;
        }


        private GeoKeysDesc ParseCoordinateToGeoKeysDesc(CoordinateBase coordinate)
        {
            // 都能支持，唯独不能支持三维空间坐标系
            if (coordinate.GetCoordinateType() == CoordinateType.Geographic)
            {
                return new GeoKeysDesc(coordinate as GeographicCoordinate);  
            }
            else if (coordinate.GetCoordinateType() == CoordinateType.Projected)
            {
                return new GeoKeysDesc(coordinate as ProjectedCoordinate);
            }
            else
            {
                throw new Exception("无法解析的未知的坐标系统");
            }

        }


        public bool ExportToENVIStandard(string filepath, ENVIEncodingType type)
        {
            return true;
        }

        public int GetRows()
        {
            return this.Rows;
        }

        public int GetCols()
        {
            return this.Cols;
        }

        public int GetRasterBandNumber()
        {
            return this.RasterBandNumber;
        }
    }
}
