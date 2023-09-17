using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Grids.Basic;
using System.IO;
using ThomasGIS.Grids.GeoTiff;
using ThomasGIS.Coordinates;

namespace ThomasGIS.Grids
{
    public static class RasterFactory
    {
        public static IRaster OpenGeoTiff(string filepath)
        {
            List<TiffIFD> IFDList = new List<TiffIFD>();
            bool geoFlag = false;

            using (BinaryReader gtiffReader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
            {
                // 是否是GeoTiff格式，若不是GeoTiff格式就没有坐标系统


                // 读取IFH部分
                // 0-1字节为顺序标志位
                byte[] orderFlag = gtiffReader.ReadBytes(2);
                // 2-3字节为TIFF的标志位
                byte[] tiffFlag = gtiffReader.ReadBytes(2);
                // 4-7字节位第一个IFD的偏移量
                long IFDOffset = gtiffReader.ReadInt32();

                while (IFDOffset != 0)
                {
                    gtiffReader.BaseStream.Seek(IFDOffset, SeekOrigin.Begin);
                    // 读取IFD部分
                    // 表示此IFD中包含了多少DE
                    int DENumber = gtiffReader.ReadInt16();
                    TiffIFD tempIFD = new TiffIFD(DENumber);
                    for (int i = 0; i < DENumber; i++)
                    {
                        int tagFlag = gtiffReader.ReadUInt16();
                        int dataType = gtiffReader.ReadInt16();
                        int dataNumber = gtiffReader.ReadInt32();

                        // 每个Tag内数据的长度为dataType * dataNumber
                        int dataLength = 0;
                        // dataType共有12种
                        switch (dataType)
                        {
                            // BYTE
                            case 1:
                            // ASCII 8-bit
                            case 2:
                            // SBYTE
                            case 6:
                            // UNDEFINED
                            case 7:
                                dataLength = dataNumber * 1;
                                break;
                            // Short
                            case 3:
                            // SSHORT
                            case 8:
                                dataLength = dataNumber * 2;
                                break;
                            // Long
                            case 4:
                            // SLong
                            case 9:
                            // Float
                            case 11:
                                dataLength = dataNumber * 4;
                                break;
                            // RATIONAL Two Longs
                            case 5:
                            // SRATIONAL Two SLongs
                            case 10:
                            // Double
                            case 12:
                                dataLength = dataNumber * 8;
                                break;
                            default:
                                throw new Exception("无法识别的GeoTiff数据类型");
                        }

                        // 如果数据长度小于等于4则就地读取
                        if (dataLength <= 4)
                        {
                            byte[] innerData = gtiffReader.ReadBytes(4);
                            tempIFD.DEList.Add(tagFlag, new TiffDE(tagFlag, dataType, dataNumber, innerData));
                        }
                        // 大于等于4则是存储为偏移量
                        else
                        {
                            int pointer = gtiffReader.ReadInt32();
                            long nowLoc = gtiffReader.BaseStream.Position;
                            gtiffReader.BaseStream.Seek(pointer, SeekOrigin.Begin);
                            byte[] innerData = gtiffReader.ReadBytes(dataLength);
                            tempIFD.DEList.Add(tagFlag, new TiffDE(tagFlag, dataType, dataNumber, innerData));
                            gtiffReader.BaseStream.Seek(nowLoc, SeekOrigin.Begin);
                        }
                    }

                    IFDList.Add(tempIFD);
                    IFDOffset = gtiffReader.ReadInt32();
                }

                gtiffReader.Close();
            }

            // IFD读完之后，就要根据IFD的数量读图像了，反正只能读一个23333

            // 解析IFD里的每个DE对应的属性，很重要.jpg
            // 按照需要的属性去IFD的DE字典里找
            TiffIFD oneIFD = IFDList[0];

            RasterBandDesc oneBandDesc = new RasterBandDesc();

            // 图像宽
            int widthTag = 0x0100;
            if (!oneIFD.DEList.ContainsKey(widthTag)) throw new Exception("已损坏的GeoTiff格式文件");
            TiffDE widthDE = oneIFD.DEList[widthTag];
            oneBandDesc.width = BitConverter.ToUInt16(widthDE.Innerdata, 0);

            // 图像高
            int heightTag = 0x0101;
            if (!oneIFD.DEList.ContainsKey(heightTag)) throw new Exception("已损坏的GeoTiff格式文件");
            TiffDE heightDE = oneIFD.DEList[heightTag];
            oneBandDesc.height = BitConverter.ToUInt16(heightDE.Innerdata, 0);

            // 颜色深度
            int colorDepthTag = 0x0102;
            if (oneIFD.DEList.ContainsKey(colorDepthTag))
            {
                TiffDE colorDepthDE = oneIFD.DEList[colorDepthTag];
                for (int j = 0; j < colorDepthDE.DataNumber; j++)
                {
                    oneBandDesc.colorDepth.Add(BitConverter.ToUInt16(colorDepthDE.Innerdata, j * 2));
                }
            }

            // 是否压缩
            int zipTag = 0x0103;
            if (oneIFD.DEList.ContainsKey(zipTag))
            {
                TiffDE zipDE = oneIFD.DEList[zipTag];
                oneBandDesc.zipFlag = BitConverter.ToUInt16(zipDE.Innerdata, 0);
            }

            // 是否反色
            int reverseTag = 0x0106;
            if (oneIFD.DEList.ContainsKey(reverseTag))
            {
                TiffDE reverseDE = oneIFD.DEList[reverseTag];
                oneBandDesc.reverseFalg = BitConverter.ToUInt16(reverseDE.Innerdata, 0);
            }

            // 每个像素中样本的数量 = 波段的数量
            int samplesPerPixelTag = 0x0115;
            if (oneIFD.DEList.ContainsKey(samplesPerPixelTag))
            {
                TiffDE samplesPerPixelDE = oneIFD.DEList[samplesPerPixelTag];
                oneBandDesc.samplesPerPixel = BitConverter.ToUInt16(samplesPerPixelDE.Innerdata, 0);
            }

            // 这是读取用线存储的TIFF文件需要的三项参数
            // 线偏移量
            int lineOffsetTag = 0x0111;
            if (oneIFD.DEList.ContainsKey(lineOffsetTag))
            {
                TiffDE lineOffsetDE = oneIFD.DEList[lineOffsetTag];
                oneBandDesc.lineOffset = BitConverter.ToInt32(lineOffsetDE.Innerdata, 0);
            }
            // 线数量
            int lineNumberTag = 0x0116;
            if (oneIFD.DEList.ContainsKey(lineNumberTag))
            {
                TiffDE lineNumberDE = oneIFD.DEList[lineNumberTag];
                oneBandDesc.lineNumber = BitConverter.ToInt32(lineNumberDE.Innerdata, 0);
            }
            // 总字节数
            int imageByteNumberTag = 0x0117;
            if (oneIFD.DEList.ContainsKey(imageByteNumberTag))
            {
                TiffDE imageByteNumberDE = oneIFD.DEList[imageByteNumberTag];
                oneBandDesc.imageByteNumber = BitConverter.ToInt32(imageByteNumberDE.Innerdata, 0);
            }


            // 水平分辨率偏移量
            int horizontalOffsetTag = 0x011A;
            if (oneIFD.DEList.ContainsKey(horizontalOffsetTag))
            {
                TiffDE horizontalOffsetDE = oneIFD.DEList[horizontalOffsetTag];
                oneBandDesc.horizontalOffset_1 = BitConverter.ToInt32(horizontalOffsetDE.Innerdata, 0);
                oneBandDesc.horizontalOffset_2 = BitConverter.ToInt32(horizontalOffsetDE.Innerdata, 4);
            }

            // 垂直分辨率偏移量
            int verticalOffsetTag = 0x011B;
            if (oneIFD.DEList.ContainsKey(verticalOffsetTag))
            {
                TiffDE verticalOffsetDE = oneIFD.DEList[verticalOffsetTag];
                oneBandDesc.verticalOffset_1 = BitConverter.ToInt32(verticalOffsetDE.Innerdata, 0);
                oneBandDesc.verticalOffset_2 = BitConverter.ToInt32(verticalOffsetDE.Innerdata, 4);
            }

            // 软件名
            int softwareTag = 0x131;
            if (oneIFD.DEList.ContainsKey(softwareTag))
            {
                TiffDE softwareDE = oneIFD.DEList[softwareTag];
                oneBandDesc.software = Encoding.ASCII.GetString(softwareDE.Innerdata);
            }

            // 时间
            int timeTag = 0x132;
            if (oneIFD.DEList.ContainsKey(timeTag))
            {
                TiffDE timeDE = oneIFD.DEList[timeTag];
                oneBandDesc.time = Encoding.ASCII.GetString(timeDE.Innerdata);
            }

            // 调色板偏移量
            int colorBandOffsetTag = 0x140;
            if (oneIFD.DEList.ContainsKey(colorBandOffsetTag))
            {
                TiffDE colorBandOffsetDE = oneIFD.DEList[colorBandOffsetTag];
                oneBandDesc.colorBandOffset = BitConverter.ToUInt16(colorBandOffsetDE.Innerdata, 0);
            }

            // 这是读取用Tile分块存储的TIFF需要的4项参数 322 323 324 325
            // 每个Tile的width宽度
            int tileWidthTag = 0x0142;
            if (oneIFD.DEList.ContainsKey(tileWidthTag))
            {
                TiffDE tileWidthDE = oneIFD.DEList[tileWidthTag];
                oneBandDesc.tileWidth = BitConverter.ToUInt16(tileWidthDE.Innerdata, 0);
            }
            // 每个Tile的height高度
            int tileHeightTag = 0x0143;
            if (oneIFD.DEList.ContainsKey(tileHeightTag))
            {
                TiffDE tileHeightDE = oneIFD.DEList[tileHeightTag];
                oneBandDesc.tileHeight = BitConverter.ToUInt16(tileHeightDE.Innerdata, 0);
            }
            // 每个Tile的存储位置偏移量
            int tileOffsetsTag = 0x0144;
            if (oneIFD.DEList.ContainsKey(tileOffsetsTag))
            {
                TiffDE tileOffsetsDE = oneIFD.DEList[tileOffsetsTag];
                int tileOffsetCount = tileOffsetsDE.DataNumber;
                for (int j = 0; j < tileOffsetCount; j++)
                {
                    oneBandDesc.tileOffsets.Add(BitConverter.ToInt32(tileOffsetsDE.Innerdata, j * 4));
                }
            }
            // 每个Tile中的字节长度
            int tileByteCountsTag = 0x0145;
            if (oneIFD.DEList.ContainsKey(tileByteCountsTag))
            {
                TiffDE tileByteCountsDE = oneIFD.DEList[tileByteCountsTag];
                int tileByteCountsCount = tileByteCountsDE.DataNumber;
                for (int j = 0; j < tileByteCountsCount; j++)
                {
                    oneBandDesc.tileByteCounts.Add(BitConverter.ToInt32(tileByteCountsDE.Innerdata, j * 4));                    
                }
            }

            int sampleFormatTag = 339;
            if (oneIFD.DEList.ContainsKey(sampleFormatTag))
            {
                TiffDE tileByteCountsDE = oneIFD.DEList[sampleFormatTag];
                int sampleFormatCount = tileByteCountsDE.DataNumber;
                for (int j = 0; j < sampleFormatCount; j++)
                {
                    oneBandDesc.sampleFormat.Add(BitConverter.ToUInt16(tileByteCountsDE.Innerdata, j * 2));
                }
            }

            // ExtraSamples
            // tiePointsAttributes[3] = xmin, tiePointsAttributes[3] = ymax
            double[] tiePointsAttributes = null;
            int modelTiePointTag = 33922;
            if (oneIFD.DEList.ContainsKey(modelTiePointTag))
            {
                tiePointsAttributes = new double[6];
                TiffDE modelTiePointDE = oneIFD.DEList[modelTiePointTag];
                int modelTiePointNumber = modelTiePointDE.DataNumber;
                for (int j = 0; j < Math.Min(modelTiePointNumber, 6); j++)
                {
                    tiePointsAttributes[j] = BitConverter.ToDouble(modelTiePointDE.Innerdata, 8 * j);
                }
            }

            // scaleAttributes[0] = xScale, tiePointsAttributes[1] = yScale, tiePointsAttributes[2] = zScale
            double[] scaleAttributes = null;
            int modelPixelScaleTag = 33550;
            if (oneIFD.DEList.ContainsKey(modelPixelScaleTag))
            {
                scaleAttributes = new double[3];
                TiffDE modelPixelScaleDE = oneIFD.DEList[modelPixelScaleTag];
                int modelPixelScaleNumber = modelPixelScaleDE.DataNumber;
                for (int j = 0; j < Math.Min(modelPixelScaleNumber, 3); j++)
                {
                    scaleAttributes[j] = BitConverter.ToDouble(modelPixelScaleDE.Innerdata, 8 * j);
                }
            }

            Dictionary<int, byte[]> geoKeyValue = new Dictionary<int, byte[]>();

            // GEOKEYDICTIONARYTAG，GeoKey字典
            int geoKeyDictionaryTag = 34735;
            if (oneIFD.DEList.ContainsKey(geoKeyDictionaryTag))
            {
                // 是Geotiff
                geoFlag = true;
                // 存储有多少个Key以及版本
                TiffDE geoKeyDictionaryDE = oneIFD.DEList[geoKeyDictionaryTag];
                int KeyDirectoryVersion = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, 0);
                int KeyRevision = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, 2);
                int MinorRevision = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, 4);
                int NumberOfKeys = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, 6);

                // 循环读取Key的值并存储，用于后续解析
                for (int j = 0; j < NumberOfKeys; j++)
                {
                    int KeyID = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, ((j + 1) * 8));
                    int TIFFTagLocation = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, ((j + 1) * 8) + 2);
                    int Count = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, ((j + 1) * 8) + 4);
                    int Value_Offset = BitConverter.ToUInt16(geoKeyDictionaryDE.Innerdata, ((j + 1) * 8) + 6);

                    if (TIFFTagLocation == 0)
                    {
                        byte[] valueData = BitConverter.GetBytes(Value_Offset);
                        geoKeyValue.Add(KeyID, valueData);
                    }
                    else
                    {
                        if (oneIFD.DEList.ContainsKey(TIFFTagLocation))
                        {
                            if (TIFFTagLocation == 34736)
                            {
                                TiffDE tempDE = oneIFD.DEList[TIFFTagLocation];
                                byte[] valueData = new byte[Count * 8];
                                for (int k = 0; k < Count * 8; k++)
                                {
                                    valueData[k] = tempDE.Innerdata[Value_Offset + k];
                                }

                                geoKeyValue.Add(KeyID, valueData);
                            }

                            if (TIFFTagLocation == 34737)
                            {
                                TiffDE tempDE = oneIFD.DEList[TIFFTagLocation];
                                byte[] valueData = new byte[Count];
                                for (int k = 0; k < Count; k++)
                                {
                                    valueData[k] = tempDE.Innerdata[Value_Offset + k];
                                }

                                geoKeyValue.Add(KeyID, valueData);
                            }
                        }
                    }
                }
            }

            GeoKeysDesc geoKeysDesc = ParseGeoKey(geoKeyValue);

            // 从图像中读取数据
            List<double[,]> bandRawDataList = new List<double[,]>();
            for (int j = 0; j < oneBandDesc.samplesPerPixel; j++)
            {
                bandRawDataList.Add(new double[oneBandDesc.height, oneBandDesc.width]);
            }

            // 如果这个Tag没有数据 == -1 说明是用Tile存储的图像
            if (oneBandDesc.imageByteNumber == -1)
            {
                // 横向的瓦片数量
                int widthTileNumber = oneBandDesc.width / oneBandDesc.tileWidth + 1;
                // 纵向的瓦片数量
                int heightTileNumber = oneBandDesc.height / oneBandDesc.tileHeight + 1;

                // 依次处理每个瓦片
                for (int j = 0; j < oneBandDesc.tileOffsets.Count; j++)
                {
                    // 每个瓦片在图像文件中的起始位置以及瓦片中的数据长度
                    long tileDataOffset = oneBandDesc.tileOffsets[j];
                    long tileDataLength = oneBandDesc.tileByteCounts[j];

                    // 打开文件
                    using (BinaryReader imgReader = new BinaryReader(new FileStream(filepath, FileMode.Open)))
                    {
                        // 定位至数据所在位置并读取等长的数据
                        imgReader.BaseStream.Seek(tileDataOffset, SeekOrigin.Begin);
                        byte[] tileData = imgReader.ReadBytes((int)tileDataLength);
                        double[,,] decodeTileData = null;

                        // 按照压缩的格式将Tile的字节数据解析为double矩阵
                        switch (oneBandDesc.zipFlag)
                        {
                            case 1:
                                // 无压缩的解析函数
                                if (oneBandDesc.sampleFormat.Count == 0)
                                {
                                    decodeTileData = UnCompressionImage(tileData, oneBandDesc.samplesPerPixel, oneBandDesc.tileWidth, oneBandDesc.tileHeight, oneBandDesc.colorDepth[0] / 8);
                                }
                                else
                                {
                                    decodeTileData = UnCompressionImage(tileData, oneBandDesc.samplesPerPixel, oneBandDesc.tileWidth, oneBandDesc.tileHeight, oneBandDesc.colorDepth[0] / 8, oneBandDesc.sampleFormat[0]);
                                }
                                break;
                            case 5:
                                // LZW压缩方法
                                throw new Exception("当前尚未支持压缩下的GTIFF文件，后续会逐步支持");
                                // decodeTileData = LZWCompression(tileData, oneBandDesc.samplesPerPixel, oneBandDesc.tileWidth, oneBandDesc.tileHeight);
                                // break;
                            default:
                                throw new Exception("当前尚未支持压缩下的GTIFF文件，后续会逐步支持");
                        }

                        // 依据Decode结果将数据填充至整体的图像中
                        // 当前Tile位于全局的行数
                        int nowHeightOffset = j / widthTileNumber;
                        // 当前Tile位于全局的列数
                        int nowWidthOffset = j - (nowHeightOffset * widthTileNumber);

                        // 填充：先波段，后行再列
                        for (int p = 0; p < oneBandDesc.samplesPerPixel; p++)
                        {
                            for (int q = 0; q < oneBandDesc.tileHeight; q++)
                            {
                                for (int k = 0; k < oneBandDesc.tileWidth; k++)
                                {
                                    int locY = nowHeightOffset * oneBandDesc.tileHeight + q;
                                    int locX = nowWidthOffset * oneBandDesc.tileWidth + k;
                                    if (locX < 0 || locX >= oneBandDesc.width || locY < 0 || locY >= oneBandDesc.height) continue;
                                    bandRawDataList[p][nowHeightOffset * oneBandDesc.tileHeight + q, nowWidthOffset * oneBandDesc.tileWidth + k] = decodeTileData[p, q, k];
                                }
                            }
                        }
                    }
                }
            }
            // 有数据则是按行存储的图像
            else
            {
                int imgDataOffset = oneBandDesc.lineOffset;
                int imgDataSize = oneBandDesc.imageByteNumber;

                using (BinaryReader sr = new BinaryReader(new FileStream(filepath, FileMode.Open)))
                {
                    sr.BaseStream.Seek(imgDataOffset, SeekOrigin.Begin);
                    byte[] imageData = sr.ReadBytes(imgDataSize);
                    double[,,] originImageData = null;
                    // 按照压缩的格式将Tile的字节数据解析为double矩阵
                    switch (oneBandDesc.zipFlag)
                    {
                        case 1:
                            // 无压缩的解析函数
                            if (oneBandDesc.sampleFormat.Count == 0)
                            {
                                originImageData = UnCompressionImage(imageData, oneBandDesc.samplesPerPixel, oneBandDesc.width, oneBandDesc.height, oneBandDesc.colorDepth[0] / 8);
                            }
                            else
                            {
                                originImageData = UnCompressionImage(imageData, oneBandDesc.samplesPerPixel, oneBandDesc.width, oneBandDesc.height, oneBandDesc.colorDepth[0] / 8, oneBandDesc.sampleFormat[0]);
                            }
                            break;
                        case 5:
                            // LZW压缩方法
                            throw new Exception("当前尚未支持压缩下的GTIFF文件，后续会逐步支持");
                        // decodeTileData = LZWCompression(tileData, oneBandDesc.samplesPerPixel, oneBandDesc.tileWidth, oneBandDesc.tileHeight);
                        // break;
                        default:
                            throw new Exception("当前尚未支持压缩下的GTIFF文件，后续会逐步支持");
                    }

                    // 填充：先波段，后行再列
                    for (int p = 0; p < oneBandDesc.samplesPerPixel; p++)
                    {
                        for (int q = 0; q < oneBandDesc.height; q++)
                        {
                            for (int k = 0; k < oneBandDesc.width; k++)
                            {
                                bandRawDataList[p][q, k] = originImageData[p, q, k];
                            }
                        }
                    }
                }
            }

            // 依据读取的内容构建Raster对象
            Raster newRaster;
            if (oneBandDesc.sampleFormat.Count == 0)
            {
                switch (oneBandDesc.colorDepth[0])
                {
                    case 8:
                        newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.UINT8);
                        break;
                    case 16:
                        newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.UINT16);
                        break;
                    case 32:
                        newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.FLOAT32);
                        break;
                    case 64:
                        newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.DOUBLE);
                        break;
                    default:
                        throw new Exception("不支持的颜色深度");
                }
            }
            else
            {
                switch (oneBandDesc.colorDepth[0])
                {
                    case 8:
                        if (oneBandDesc.sampleFormat[0] == 1)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.UINT8);
                        }
                        else if (oneBandDesc.sampleFormat[0] == 2)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.INT8);
                        }
                        else
                        {
                            throw new Exception("冲突的数据类型！");
                        }
                        break;
                    case 16:
                        if (oneBandDesc.sampleFormat[0] == 1)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.UINT16);
                        }
                        else if (oneBandDesc.sampleFormat[0] == 2)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.INT16);
                        }
                        else
                        {
                            throw new Exception("冲突的数据类型！");
                        }
                        break;
                    case 32:
                        if (oneBandDesc.sampleFormat[0] == 1)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.UINT32);
                        }
                        else if (oneBandDesc.sampleFormat[0] == 2)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.INT32);
                        }
                        else if (oneBandDesc.sampleFormat[0] == 3)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.FLOAT32);
                        }
                        else
                        {
                            throw new Exception("冲突的数据类型！");
                        }
                        break;
                    case 64:
                        if (oneBandDesc.sampleFormat[0] == 2)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.ULONG);
                        }
                        if (oneBandDesc.sampleFormat[0] == 2)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.LONG);
                        }
                        else if (oneBandDesc.sampleFormat[0] == 3)
                        {
                            newRaster = new Raster(oneBandDesc.height, oneBandDesc.width, RasterDataType.DOUBLE);
                        }
                        else
                        {
                            throw new Exception("冲突的数据类型！");
                        }
                        break;
                    default:
                        throw new Exception("不支持的颜色深度");
                }
            }


            for (int j = 0; j < oneBandDesc.samplesPerPixel; j++)
            {
                RasterBand newBand = new RasterBand(oneBandDesc.height, oneBandDesc.width);
                newBand.WriteData(bandRawDataList[j]);
                newRaster.AddRasterBand(newBand);
            }

            // 设置地理变换参数
            double xmin = 0;
            double ymax = newRaster.Rows;
            double xScale = 1;
            double yScale = 1;

            if (tiePointsAttributes != null)
            {
                xmin = tiePointsAttributes[3];
                ymax = tiePointsAttributes[4];
            }

            if (scaleAttributes != null)
            {
                xScale = scaleAttributes[0];
                yScale = scaleAttributes[1];
            }

            newRaster.SetGeoTransform(xmin, ymax, xScale, yScale);

            // 从Geokey中解析坐标系统
            // 首先是根据 1024 确定是地理坐标系还是投影坐标系还是三维坐标系

            if (geoFlag)
            {
                CoordinateBase coordinateSystem = CoordinateGenerator.ParseFormTiffGeoDesc(geoKeysDesc);
                newRaster.SetCoordinateSystem(coordinateSystem);
            }
            else
            {
                newRaster.SetCoordinateSystem(null);
            }

            return newRaster;
        }

        private static double[,,] UnCompressionImage(byte[] inputData, int bands, int width, int height, int type, int readType = 1)
        {
            double[,,] resultData = new double[bands, height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < bands; k++)
                    {
                        if (type == 1 && readType == 1)
                        {
                            resultData[k, i, j] = inputData[(i * width * bands + j * bands + k) * type];
                        }
                        else if (type == 2 && readType == 1)
                        {
                            resultData[k, i, j] = BitConverter.ToUInt16(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 2 && readType == 2)
                        {
                            resultData[k, i, j] = BitConverter.ToInt16(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 4 && readType == 1)
                        {
                            resultData[k, i, j] = BitConverter.ToUInt32(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 4 && readType == 2)
                        {
                            resultData[k, i, j] = BitConverter.ToInt32(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 4 && readType == 3)
                        {
                            resultData[k, i, j] = BitConverter.ToSingle(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 8 && readType == 3)
                        {
                            resultData[k, i, j] = BitConverter.ToDouble(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 8 && readType == 2)
                        {
                            resultData[k, i, j] = BitConverter.ToInt64(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else if (type == 8 && readType == 1)
                        {
                            resultData[k, i, j] = BitConverter.ToUInt64(inputData, (i * width * bands + j * bands + k) * type);
                        }
                        else
                        {
                            throw new Exception("无法识别的数据类型！");
                        }
                    }
                }
            }

            return resultData;
        }

        private static double[,,] LZWCompression(byte[] inputData, int bands, int width, int height)
        {
            Dictionary<byte[], short> colorTable = new Dictionary<byte[], short>();
            for (int i = 0; i < 100; i++)
            {
                double value = BitConverter.ToDouble(inputData, i * 8 + 1);
            }

            return new double[bands, width, height];
        }

        private static GeoKeysDesc ParseGeoKey(Dictionary<int, byte[]> geoKeyValue)
        {
            GeoKeysDesc desc = new GeoKeysDesc();

            if (geoKeyValue.ContainsKey(1024))
            {
                desc.GTModelTypeGeoKey = BitConverter.ToInt16(geoKeyValue[1024], 0);
            }

            if (geoKeyValue.ContainsKey(1025))
            {
                desc.GTRasterTypeGeoKey = BitConverter.ToInt16(geoKeyValue[1025], 0);
            }

            if (geoKeyValue.ContainsKey(1026))
            {
                desc.GTCitationGeoKey = Encoding.ASCII.GetString(geoKeyValue[1026]);
            }

            if (geoKeyValue.ContainsKey(2048))
            {
                desc.GeogGeodeticCRSGeoKey = BitConverter.ToUInt16(geoKeyValue[2048], 0);
            }

            if (geoKeyValue.ContainsKey(2049))
            {
                desc.GeogCitationGeoKey = Encoding.ASCII.GetString(geoKeyValue[2049]);
            }

            if (geoKeyValue.ContainsKey(2050))
            {
                desc.GeogGeodeticDatumGeoKey = BitConverter.ToUInt16(geoKeyValue[2050], 0);
            }

            if (geoKeyValue.ContainsKey(2051))
            {
                desc.GeogPrimeMeridianGeoKey = BitConverter.ToUInt16(geoKeyValue[2051], 0);
            }

            if (geoKeyValue.ContainsKey(2052))
            {
                desc.GeogLinearUnitsGeoKey = BitConverter.ToUInt16(geoKeyValue[2052], 0);
            }

            if (geoKeyValue.ContainsKey(2053))
            {
                desc.GeogLinearUnitSizeGeoKey = BitConverter.ToDouble(geoKeyValue[2053], 0);
            }

            if (geoKeyValue.ContainsKey(2054))
            {
                desc.GeogAngularUnitsGeoKey = BitConverter.ToUInt16(geoKeyValue[2054], 0);
            }

            if (geoKeyValue.ContainsKey(2055))
            {
                desc.GeogAngularUnitSizeGeoKey = BitConverter.ToDouble(geoKeyValue[2055], 0);
            }

            if (geoKeyValue.ContainsKey(2056))
            {
                desc.GeogEllipsoidGeoKey = BitConverter.ToUInt16(geoKeyValue[2056], 0);
            }

            if (geoKeyValue.ContainsKey(2057))
            {
                desc.EllipsoidSemiMajorAxisGeoKey = BitConverter.ToDouble(geoKeyValue[2057], 0);
            }

            if (geoKeyValue.ContainsKey(2058))
            {
                desc.EllipsoidSemiMinorAxisGeoKey = BitConverter.ToDouble(geoKeyValue[2058], 0);
            }

            if (geoKeyValue.ContainsKey(2059))
            {
                desc.EllipsoidInvFlatteningGeoKey = BitConverter.ToDouble(geoKeyValue[2059], 0);
            }

            if (geoKeyValue.ContainsKey(2060))
            {
                desc.GeogAzimuthUnitGeoKey = BitConverter.ToUInt16(geoKeyValue[2060], 0);
            }

            if (geoKeyValue.ContainsKey(2061))
            {
                desc.GeogPrimeMeridianLongitudeGeoKey = BitConverter.ToDouble(geoKeyValue[2061], 0);
            }

            if (geoKeyValue.ContainsKey(3072))
            {
                desc.ProjectedCRSGeoKey = BitConverter.ToUInt16(geoKeyValue[3072], 0);
            }

            if (geoKeyValue.ContainsKey(3073))
            {
                desc.ProjectedCitationGeoKey = Encoding.ASCII.GetString(geoKeyValue[3073]);
            }

            if (geoKeyValue.ContainsKey(3074))
            {
                desc.ProjectionGeoKey = BitConverter.ToUInt16(geoKeyValue[3074], 0);
            }

            if (geoKeyValue.ContainsKey(3075))
            {
                desc.ProjectionMethodGeoKey = BitConverter.ToUInt16(geoKeyValue[3075], 0);
            }

            if (geoKeyValue.ContainsKey(3076))
            {
                desc.ProjLinearUnitGeoKey = BitConverter.ToUInt16(geoKeyValue[3076], 0);
            }

            if (geoKeyValue.ContainsKey(3077))
            {
                desc.ProjLinearUnitSizeGeoKey = BitConverter.ToDouble(geoKeyValue[3077], 0);
            }

            if (geoKeyValue.ContainsKey(3078))
            {
                desc.ProjStandardParallel_1 = BitConverter.ToDouble(geoKeyValue[3078], 0);
            }

            if (geoKeyValue.ContainsKey(3079))
            {
                desc.ProjStandardParallel_2 = BitConverter.ToDouble(geoKeyValue[3079], 0);
            }

            if (geoKeyValue.ContainsKey(3080))
            {
                desc.ProjNaturalOriginLongitude = BitConverter.ToDouble(geoKeyValue[3080], 0);
            }

            if (geoKeyValue.ContainsKey(3081))
            {
                desc.ProjNaturalOriginLatitude = BitConverter.ToDouble(geoKeyValue[3081], 0);
            }

            if (geoKeyValue.ContainsKey(3082))
            {
                desc.ProjFalseEasting = BitConverter.ToDouble(geoKeyValue[3082], 0);
            }

            if (geoKeyValue.ContainsKey(3083))
            {
                desc.ProjFalseNorthing = BitConverter.ToDouble(geoKeyValue[3083], 0);
            }

            if (geoKeyValue.ContainsKey(3084))
            {
                desc.ProjFalseOriginLongitude = BitConverter.ToDouble(geoKeyValue[3084], 0);
            }

            if (geoKeyValue.ContainsKey(3085))
            {
                desc.ProjFalseOriginLatitude = BitConverter.ToDouble(geoKeyValue[3085], 0);
            }

            if (geoKeyValue.ContainsKey(3086))
            {
                desc.ProjFalseOriginEasting = BitConverter.ToDouble(geoKeyValue[3086], 0);
            }

            if (geoKeyValue.ContainsKey(3087))
            {
                desc.ProjFalseOriginNorthing = BitConverter.ToDouble(geoKeyValue[3087], 0);
            }

            if (geoKeyValue.ContainsKey(3088))
            {
                desc.ProjCenterLongitude = BitConverter.ToDouble(geoKeyValue[3088], 0);
            }

            if (geoKeyValue.ContainsKey(3089))
            {
                desc.ProjCenterLatitude = BitConverter.ToDouble(geoKeyValue[3089], 0);
            }

            if (geoKeyValue.ContainsKey(3090))
            {
                desc.ProjCenterEasting = BitConverter.ToDouble(geoKeyValue[3090], 0);
            }

            if (geoKeyValue.ContainsKey(3091))
            {
                desc.ProjCenterNorthing = BitConverter.ToDouble(geoKeyValue[3091], 0);
            }

            if (geoKeyValue.ContainsKey(3092))
            {
                desc.ProjScaleAtNaturalOrigin = BitConverter.ToDouble(geoKeyValue[3092], 0);
            }

            if (geoKeyValue.ContainsKey(3093))
            {
                desc.ProjScaleAtCenter = BitConverter.ToDouble(geoKeyValue[3093], 0);
            }

            if (geoKeyValue.ContainsKey(3094))
            {
                desc.ProjProjAzimuthAngleGeoKey = BitConverter.ToDouble(geoKeyValue[3094], 0);
            }

            if (geoKeyValue.ContainsKey(3095))
            {
                desc.ProjStraightVerticalPole = BitConverter.ToDouble(geoKeyValue[3095], 0);
            }

            if (geoKeyValue.ContainsKey(4096))
            {
                desc.VerticalGeoKey = BitConverter.ToUInt16(geoKeyValue[4096], 0);
            }

            if (geoKeyValue.ContainsKey(4097))
            {
                desc.VerticalCitationGeoKey = Encoding.ASCII.GetString(geoKeyValue[4097]);
            }

            if (geoKeyValue.ContainsKey(4098))
            {
                desc.VerticalDatumGeoKey = BitConverter.ToUInt16(geoKeyValue[4098], 0);
            }

            if (geoKeyValue.ContainsKey(4099))
            {
                desc.VerticalUnitSizeGeoKey = BitConverter.ToUInt16(geoKeyValue[4099], 0);
            }

            return desc;
        }
    }
}
