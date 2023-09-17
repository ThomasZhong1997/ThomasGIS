using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Vector;
using ThomasGIS.TrajectoryPackage;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.TrajectoryPackage.Order;
using ThomasGIS.Grids.Basic;

namespace ThomasGIS.Projections
{
    public static class ProjectionProcessor
    {
        public static bool DeProject(IShapefile shapefile)
        {
            CoordinateBase shpOriCoorSys = shapefile.GetCoordinateRef();
            // 如果默认坐标系为空，则自动生成一个WGS84坐标系统
            if (shpOriCoorSys == null || shpOriCoorSys.GetCoordinateType() == CoordinateType.Unknown || shpOriCoorSys.GetCoordinateType() == CoordinateType.Geographic)
            {
                return false;
            }

            ProjectedCoordinate nowCoordinate = shpOriCoorSys as ProjectedCoordinate;
            GeographicCoordinate baseCoordinate = nowCoordinate.GeoCoordinate;
            IProjection nowProjection = ProjectionGenerator.GenerateProjection(shpOriCoorSys);
            // 获取基准椭球体的参数
            // 长半轴
            double a = baseCoordinate.Datum.Spheroid.SemiMajorAxis;
            // 扁率
            double flatten = baseCoordinate.Datum.Spheroid.InverseFlattening;

            // 并行反算每个Geometry
            Parallel.For(0, shapefile.GetFeatureNumber(), i =>
            {
                IShpGeometryBase nowGeometry = shapefile.GetFeature(i);
                switch (nowGeometry.GetFeatureType())
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        ShpPoint nowPoint = nowGeometry as ShpPoint;
                        nowProjection.Backward(nowPoint, a, flatten);
                        shapefile.SetFeature(i, nowPoint);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        ShpPolyline nowLine = nowGeometry as ShpPolyline;
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            nowProjection.Backward(nowLine.PointList[j], a, flatten);
                        }
                        shapefile.SetFeature(i, nowLine);
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        ShpPolygon nowGon = nowGeometry as ShpPolygon;
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            nowProjection.Backward(nowGon.PointList[j], a, flatten);
                        }
                        shapefile.SetFeature(i, nowGon);
                        break;
                    default:
                        throw new Exception("无法识别的类型！");
                }
            });

            // 还原为地理坐标
            shapefile.SetCoordinateRef(baseCoordinate);

            return true;
        }

        public static bool DeProject(ITrajectorySet trajectorySet)
        {
            Parallel.ForEach(trajectorySet.GetTrajectoryEnumerable(), trajectory =>
            {
                DeProject(trajectory);
            });

            return true;
        }

        public static bool DeProject(ITrajectory trajectory)
        {
            CoordinateBase nowCoordinate = trajectory.GetCoordinateSystem();

            if (nowCoordinate == null || nowCoordinate.GetCoordinateType() == CoordinateType.Geographic || nowCoordinate.GetCoordinateType() == CoordinateType.Unknown)
            {
                return false;
            }

            ProjectedCoordinate trueCoordinate = nowCoordinate as ProjectedCoordinate;

            GeographicCoordinate baseCoordinate = trueCoordinate.GeoCoordinate;

            IProjection nowProjection = ProjectionGenerator.GenerateProjection(trueCoordinate);

            Parallel.For(0, trajectory.GetPointNumber(), i =>
            {
                nowProjection.Backward(trajectory.GetPointByIndex(i), baseCoordinate.Datum.Spheroid.SemiMajorAxis, baseCoordinate.Datum.Spheroid.InverseFlattening);
            });

            trajectory.SetCoordinateSystem(baseCoordinate);

            return true;
        }

        public static bool DoProject(ITrajectorySet trajectorySet, IProjection towardProjection)
        {
            Parallel.ForEach(trajectorySet.GetTrajectoryEnumerable(), trajectory =>
            {
                DoProject(trajectory, towardProjection);
            });

            return true;
        }

        public static bool DoProject(ITrajectory trajectory, IProjection towardProjection)
        {
            CoordinateBase nowCoordinate = trajectory.GetCoordinateSystem();

            // 如果默认坐标系为空，则自动生成一个WGS84坐标系统
            if (nowCoordinate == null || nowCoordinate.GetCoordinateType() == CoordinateType.Unknown)
            {
                nowCoordinate = CoordinateGenerator.CRSParseFromEPSG(4326);
            }

            // 如果当前已经是投影坐标系，则需要先把当前投影解除
            else if (nowCoordinate.GetCoordinateType() == CoordinateType.Projected)
            {
                DeProject(trajectory);
                nowCoordinate = trajectory.GetCoordinateSystem();
            }

            GeographicCoordinate baseCoordinate = nowCoordinate as GeographicCoordinate;

            // 获取基准椭球体的参数
            // 长半轴
            double a = baseCoordinate.Datum.Spheroid.SemiMajorAxis;
            // 扁率
            double flatten = baseCoordinate.Datum.Spheroid.InverseFlattening;

            Parallel.ForEach(trajectory.GetPointEnumerable(), onePoint =>
            {
                towardProjection.Toward(onePoint, a, flatten);
            });

            // 投影为投影坐标
            ProjectedCoordinate projectedCoordinate = new ProjectedCoordinate(baseCoordinate, towardProjection.GetProjectionName());
            Dictionary<string, double> writtenParameters = towardProjection.GetWrittenParameters();
            projectedCoordinate.Projection = towardProjection.GetProjectionType();
            foreach (string key in writtenParameters.Keys)
            {
                projectedCoordinate.Parameters.Add(key, writtenParameters[key]);
            }

            trajectory.SetCoordinateSystem(projectedCoordinate);

            return true;
        }

        public static bool DoProject(IShapefile shapefile, IProjection towardProjection)
        {
            CoordinateBase shpOriCoorSys = shapefile.GetCoordinateRef();

            // 如果默认坐标系为空，则自动生成一个WGS84坐标系统
            if (shpOriCoorSys == null || shpOriCoorSys.GetCoordinateType() == CoordinateType.Unknown)
            {
                shpOriCoorSys = CoordinateGenerator.CRSParseFromEPSG(4326);
            }
            // 如果当前已经是投影坐标系，则需要先把当前投影解除
            else if (shpOriCoorSys.GetCoordinateType() == CoordinateType.Projected)
            {
                DeProject(shapefile);
                shpOriCoorSys = shapefile.GetCoordinateRef();
            }

            // 开始进行投影运算
            GeographicCoordinate baseCoordinate = shpOriCoorSys as GeographicCoordinate;

            // 获取基准椭球体的参数
            // 长半轴
            double a = baseCoordinate.Datum.Spheroid.SemiMajorAxis;
            // 扁率
            double flatten = baseCoordinate.Datum.Spheroid.InverseFlattening;

            for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
            {
                IShpGeometryBase nowGeometry = shapefile.GetFeature(i);
                switch (nowGeometry.GetFeatureType())
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        IShpPoint nowPoint = nowGeometry as IShpPoint;
                        towardProjection.Toward(nowPoint, a, flatten);
                        shapefile.SetFeature(i, nowPoint);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        IShpPolyline nowLine = nowGeometry as IShpPolyline;
                        for (int j = 0; j < nowLine.GetPointNumber(); j++)
                        {
                            towardProjection.Toward(nowLine.GetPointByIndex(j), a, flatten);
                        }
                        shapefile.SetFeature(i, nowLine);
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        IShpPolygon nowGon = nowGeometry as IShpPolygon;
                        for (int j = 0; j < nowGon.GetPointNumber(); j++)
                        {
                            towardProjection.Toward(nowGon.GetPointByIndex(j), a, flatten);
                        }
                        shapefile.SetFeature(i, nowGon);
                        break;
                    default:
                        throw new Exception("无法识别的类型！");
                }
            }

            //// 并行投影每个Geometry
            //Parallel.For(0, shapefile.GetFeatureNumber(), i =>
            //{
            //    IShpGeometryBase nowGeometry = shapefile.GetFeature(i);
            //    switch (nowGeometry.GetFeatureType())
            //    {
            //        case ESRIShapeType.Point:
            //        case ESRIShapeType.PointZ:
            //            IShpPoint nowPoint = nowGeometry as IShpPoint;
            //            towardProjection.Toward(nowPoint, a, flatten);
            //            shapefile.SetFeature(i, nowPoint);
            //            break;
            //        case ESRIShapeType.Polyline:
            //        case ESRIShapeType.PolylineZ:
            //            IShpPolyline nowLine = nowGeometry as IShpPolyline;
            //            for (int j = 0; j < nowLine.GetPointNumber(); j++)
            //            {
            //                towardProjection.Toward(nowLine.GetPointByIndex(j), a, flatten);
            //            }
            //            shapefile.SetFeature(i, nowLine);
            //            break;
            //        case ESRIShapeType.Polygon:
            //        case ESRIShapeType.PolygonZ:
            //            IShpPolygon nowGon = nowGeometry as IShpPolygon;
            //            for (int j = 0; j < nowGon.GetPointNumber(); j++)
            //            {
            //                towardProjection.Toward(nowGon.GetPointByIndex(j), a, flatten);
            //            }
            //            shapefile.SetFeature(i, nowGon);
            //            break;
            //        default:
            //            throw new Exception("无法识别的类型！");
            //    }
            //});

            // 投影为投影坐标
            ProjectedCoordinate projectedCoordinate = new ProjectedCoordinate(baseCoordinate, towardProjection.GetProjectionName());
            Dictionary<string, double> writtenParameters = towardProjection.GetWrittenParameters();
            projectedCoordinate.Projection = towardProjection.GetProjectionType();
            foreach (string key in writtenParameters.Keys)
            {
                projectedCoordinate.Parameters.Add(key, writtenParameters[key]);
            }
            shapefile.SetCoordinateRef(projectedCoordinate);

            return true;
        }

        public static bool DoProject(ICollection<IShpGeometryBase> featureList, IProjection towardProjection, GeographicCoordinate baseCoordinate = null)
        {
            if (baseCoordinate == null)
            {
                baseCoordinate = CoordinateGenerator.CRSParseFromEPSG(4326) as GeographicCoordinate;
            }

            // 获取基准椭球体的参数
            // 长半轴
            double a = baseCoordinate.Datum.Spheroid.SemiMajorAxis;
            // 扁率
            double flatten = baseCoordinate.Datum.Spheroid.InverseFlattening;

            Parallel.ForEach(featureList, oneFeature =>
            {
                ESRIShapeType type = oneFeature.GetFeatureType();
                switch (type)
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        towardProjection.Toward((Point)oneFeature, a, flatten);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        for (int j = 0; j < ((ShpPolyline)oneFeature).PointNumber; j++)
                        {
                            towardProjection.Toward(((ShpPolyline)oneFeature).PointList[j], a, flatten);
                        }
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        for (int j = 0; j < ((ShpPolygon)oneFeature).PointNumber; j++)
                        {
                            towardProjection.Toward(((ShpPolygon)oneFeature).PointList[j], a, flatten);
                        }
                        break;
                    default:
                        throw new Exception("无法识别的类型！");

                }
            });

            return true;
        }

        public static bool DoGeoTransform(IShapefile shapefile, ITransform towardTransform)
        {
            CoordinateBase nowCoordinate = shapefile.GetCoordinateRef();
            if (nowCoordinate.GetCoordinateType() == CoordinateType.Projected || nowCoordinate.GetCoordinateType() == CoordinateType.Unknown)
            {
                throw new Exception("该功能仅适用于地理坐标间的转换！");
            }

            Parallel.For(0, shapefile.GetFeatureNumber(), i =>
            {
                IShpGeometryBase nowGeometry = shapefile.GetFeature(i);
                switch (nowGeometry.GetFeatureType())
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        ShpPoint nowPoint = nowGeometry as ShpPoint;
                        towardTransform.Toward(nowPoint);
                        shapefile.SetFeature(i, nowPoint);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        ShpPolyline nowLine = nowGeometry as ShpPolyline;
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            towardTransform.Toward(nowLine.PointList[j]);
                        }
                        shapefile.SetFeature(i, nowLine);
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        ShpPolygon nowGon = nowGeometry as ShpPolygon;
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            towardTransform.Toward(nowGon.PointList[j]);
                        }
                        shapefile.SetFeature(i, nowGon);
                        break;
                    default:
                        throw new Exception("无法识别的类型！");
                }
            });

            return true;
        }

        public static bool DeGeoTransform(IShapefile shapefile, ITransform nowTransform)
        {
            CoordinateBase nowCoordinate = shapefile.GetCoordinateRef();
            if (nowCoordinate.GetCoordinateType() == CoordinateType.Projected || nowCoordinate.GetCoordinateType() == CoordinateType.Unknown)
            {
                throw new Exception("该功能仅适用于地理坐标间的转换！");
            }

            // 并行反算每个Geometry
            Parallel.For(0, shapefile.GetFeatureNumber(), i => {
                IShpGeometryBase nowGeometry = shapefile.GetFeature(i);
                switch (nowGeometry.GetFeatureType())
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        ShpPoint nowPoint = nowGeometry as ShpPoint;
                        nowTransform.Backward(nowPoint);
                        shapefile.SetFeature(i, nowPoint);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        ShpPolyline nowLine = nowGeometry as ShpPolyline;
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            nowTransform.Backward(nowLine.PointList[j]);
                        }
                        shapefile.SetFeature(i, nowLine);
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        ShpPolygon nowGon = nowGeometry as ShpPolygon;
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            nowTransform.Backward(nowGon.PointList[j]);
                        }
                        shapefile.SetFeature(i, nowGon);
                        break;
                    default:
                        throw new Exception("无法识别的类型！");
                }
            });

            return true;
        }

        public static bool DoGeoTransform(IPoint point, ITransform towardTransform)
        {
            towardTransform.Toward(point);
            return true;
        }

        public static bool DoGeoTransform(ITrajectorySet trajectorySet, ITransform towardTransform)
        {
            Parallel.ForEach(trajectorySet.GetTrajectoryEnumerable(), trajectory =>
            {
                DoGeoTransform(trajectory, towardTransform);
            });

            return true;
        }

        public static bool DoGeoTransform(ITrajectory trajectory, ITransform towardTransform)
        {
            if (trajectory.GetCoordinateSystem().GetCoordinateType() != CoordinateType.Geographic) throw new Exception("仅支持地理坐标系下的变换");

            Parallel.ForEach(trajectory.GetPointEnumerable(), point =>
            {
                towardTransform.Toward(point);
            });

            return true;
        }

        public static bool DeGeoTransform(ITrajectorySet trajectorySet, ITransform nowTransform)
        {
            Parallel.ForEach(trajectorySet.GetTrajectoryEnumerable(), trajectory =>
            {
                DeGeoTransform(trajectory, nowTransform);
            });

            return true;
        }

        public static bool DeGeoTransform(ITrajectory trajectory, ITransform nowTransform)
        {
            if (trajectory.GetCoordinateSystem().GetCoordinateType() != CoordinateType.Geographic) throw new Exception("仅支持地理坐标系下的变换");

            Parallel.ForEach(trajectory.GetPointEnumerable(), point =>
            {
                nowTransform.Backward(point);
            });

            return true;
        }

        public static bool DeGeoTransform(IPoint point, ITransform nowTransform)
        {
            nowTransform.Backward(point);
            return true;
        }

        public static bool DoGeoTransform(ICollection<IShpGeometryBase> featureList, ITransform towardTransform)
        {

            Parallel.ForEach(featureList, oneFeature =>
            {
                ESRIShapeType type = oneFeature.GetFeatureType();
                switch (type)
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        towardTransform.Toward((Point)oneFeature);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        for (int j = 0; j < ((ShpPolyline)oneFeature).PointNumber; j++)
                        {
                            towardTransform.Toward(((ShpPolyline)oneFeature).PointList[j]);
                        }
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        for (int j = 0; j < ((ShpPolygon)oneFeature).PointNumber; j++)
                        {
                            towardTransform.Toward(((ShpPolygon)oneFeature).PointList[j]);
                        }
                        break;
                    default:
                        throw new Exception("无法识别的类型！");

                }
            });

            return true;
        }

        public static bool DeGeoTransform(ICollection<IShpGeometryBase> featureList, ITransform nowTransform)
        {
            Parallel.ForEach(featureList, oneFeature =>
            {
                ESRIShapeType type = oneFeature.GetFeatureType();
                switch (type)
                {
                    case ESRIShapeType.Point:
                    case ESRIShapeType.PointZ:
                        nowTransform.Toward((Point)oneFeature);
                        break;
                    case ESRIShapeType.Polyline:
                    case ESRIShapeType.PolylineZ:
                        for (int j = 0; j < ((ShpPolyline)oneFeature).PointNumber; j++)
                        {
                            nowTransform.Toward(((ShpPolyline)oneFeature).PointList[j]);
                        }
                        break;
                    case ESRIShapeType.Polygon:
                    case ESRIShapeType.PolygonZ:
                        for (int j = 0; j < ((ShpPolygon)oneFeature).PointNumber; j++)
                        {
                            nowTransform.Toward(((ShpPolygon)oneFeature).PointList[j]);
                        }
                        break;
                    default:
                        throw new Exception("无法识别的类型！");

                }
            });

            return true;
        }

        public static bool DoGeoTransform(ITrajectoryOrder trajectoryOrder, ITransform towardTransform)
        {
            CoordinateBase coordinateBase = trajectoryOrder.GetCoordinateSystem();
            if (coordinateBase != null && coordinateBase.GetCoordinateType() == CoordinateType.Projected) return false;

            towardTransform.Toward(trajectoryOrder.GetStartPoint());
            towardTransform.Toward(trajectoryOrder.GetEndPoint());
            return true;
        }

        public static bool DoGeoTransform(ITrajectoryOrderSet trajectoryOrderSet, ITransform towardTransform)
        {
            Parallel.ForEach(trajectoryOrderSet.GetOrderEnumerable(), oneOrder =>
            {
                DoGeoTransform(oneOrder, towardTransform);
            });

            return true;
        }

        public static bool DeGeoTransform(ITrajectoryOrder trajectoryOrder, ITransform nowTransform)
        {
            CoordinateBase coordinateBase = trajectoryOrder.GetCoordinateSystem();
            if (coordinateBase != null && coordinateBase.GetCoordinateType() == CoordinateType.Projected) return false;

            nowTransform.Backward(trajectoryOrder.GetStartPoint());
            nowTransform.Backward(trajectoryOrder.GetEndPoint());
            return true;
        }

        public static bool DeGeoTransform(ITrajectoryOrderSet trajectoryOrderSet, ITransform nowTransform)
        { 
            Parallel.ForEach(trajectoryOrderSet.GetOrderEnumerable(), oneOrder =>
            {
                nowTransform.Backward(oneOrder.GetStartPoint());
                nowTransform.Backward(oneOrder.GetEndPoint());
            });

            return true;
        }

        public static bool DeProject(ITrajectoryOrder trajectoryOrder)
        {
            CoordinateBase coordinateBase = trajectoryOrder.GetCoordinateSystem();
            if (coordinateBase == null || coordinateBase.GetCoordinateType() == CoordinateType.Geographic) return false;

            ProjectedCoordinate projectedCoordinate = coordinateBase as ProjectedCoordinate;

            IProjection nowProjection = ProjectionGenerator.GenerateProjection(projectedCoordinate);

            nowProjection.Backward(trajectoryOrder.GetStartPoint(), projectedCoordinate.GeoCoordinate.Datum.Spheroid.SemiMajorAxis, projectedCoordinate.GeoCoordinate.Datum.Spheroid.InverseFlattening);
            nowProjection.Backward(trajectoryOrder.GetEndPoint(), projectedCoordinate.GeoCoordinate.Datum.Spheroid.SemiMajorAxis, projectedCoordinate.GeoCoordinate.Datum.Spheroid.InverseFlattening);

            trajectoryOrder.SetCoordinateSystem(projectedCoordinate.GeoCoordinate);

            return true;
        }

        public static bool DeProject(ITrajectoryOrderSet trajectoryOrderSet)
        {
            Parallel.ForEach(trajectoryOrderSet.GetOrderEnumerable(), oneOrder =>
            {
                DeProject(oneOrder);
            });

            return true;
        }

        public static bool DoProject(ITrajectoryOrder trajectoryOrder, IProjection towardProjection)
        {
            CoordinateBase coordinateBase = trajectoryOrder.GetCoordinateSystem();
            if (coordinateBase == null) return false;

            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                DeProject(trajectoryOrder);
                coordinateBase = trajectoryOrder.GetCoordinateSystem();
            }

            GeographicCoordinate baseCoordinate = coordinateBase as GeographicCoordinate;

            // 获取基准椭球体的参数
            // 长半轴
            double a = baseCoordinate.Datum.Spheroid.SemiMajorAxis;
            // 扁率
            double flatten = baseCoordinate.Datum.Spheroid.InverseFlattening;

            towardProjection.Backward(trajectoryOrder.GetStartPoint(), a, flatten);
            towardProjection.Backward(trajectoryOrder.GetEndPoint(), a, flatten);

            // 投影为投影坐标
            ProjectedCoordinate projectedCoordinate = new ProjectedCoordinate(baseCoordinate, towardProjection.GetProjectionName());
            Dictionary<string, double> writtenParameters = towardProjection.GetWrittenParameters();
            projectedCoordinate.Projection = towardProjection.GetProjectionType();
            foreach (string key in writtenParameters.Keys)
            {
                projectedCoordinate.Parameters.Add(key, writtenParameters[key]);
            }

            trajectoryOrder.SetCoordinateSystem(projectedCoordinate);

            return true;
        }

        public static bool DoProject(ITrajectoryOrderSet trajectoryOrderSet, IProjection towardProjection)
        {
            Parallel.ForEach(trajectoryOrderSet.GetOrderEnumerable(), oneOrder =>
            {
                DoProject(oneOrder, towardProjection);
            });

            return true;
        }

        public static bool DoGeoTransform(IRaster raster, ITransform towardTransform)
        {
            CoordinateBase coordinateBase = raster.GetCoordinateSystem();
            if (coordinateBase == null || coordinateBase.GetCoordinateType() == CoordinateType.Projected) return false;

            double[] geoTransform = raster.GetGeoTransform();
            double xMin = geoTransform[0];
            double yMax = geoTransform[3];
            int rows = raster.GetRows();
            int cols = raster.GetCols();
            double xScale = geoTransform[1];
            double yScale = geoTransform[5];
            double xMax = xMin + xScale * cols;
            double yMin = yMax - yScale * rows;

            IPoint leftTop = new ThomasGIS.Geometries.Point(xMin, yMax);
            IPoint rightBottom = new ThomasGIS.Geometries.Point(xMax, yMin);

            towardTransform.Toward(leftTop);
            towardTransform.Toward(rightBottom);

            double newXScale = (rightBottom.GetX() - leftTop.GetX()) / cols;
            double newYScale = (leftTop.GetY() - rightBottom.GetY()) / rows;

            if (xScale == yScale && newXScale != newYScale)
            {
                newXScale = newYScale;
            }

            raster.SetGeoTransform(leftTop.GetX(), leftTop.GetY(), newXScale, newYScale);

            return true;
        }

        public static bool DeGeoTransform(IRaster raster, ITransform nowTransform)
        {
            CoordinateBase coordinateBase = raster.GetCoordinateSystem();
            if (coordinateBase == null || coordinateBase.GetCoordinateType() == CoordinateType.Projected) return false;

            double[] geoTransform = raster.GetGeoTransform();
            double xMin = geoTransform[0];
            double yMax = geoTransform[3];
            int rows = raster.GetRows();
            int cols = raster.GetCols();
            double xScale = geoTransform[1];
            double yScale = geoTransform[5];
            double xMax = xMin + xScale * cols;
            double yMin = yMax - yScale * rows;

            IPoint leftTop = new ThomasGIS.Geometries.Point(xMin, yMax);
            IPoint rightBottom = new ThomasGIS.Geometries.Point(xMax, yMin);

            nowTransform.Backward(leftTop);
            nowTransform.Backward(rightBottom);

            double newXScale = (rightBottom.GetX() - leftTop.GetX()) / cols;
            double newYScale = (leftTop.GetY() - rightBottom.GetY()) / rows;

            if (xScale == yScale && newXScale != newYScale)
            {
                newXScale = newYScale;
            }

            raster.SetGeoTransform(leftTop.GetX(), leftTop.GetY(), newXScale, newYScale);

            return true;
        }

        public static bool DeProject(IRaster raster)
        {
            CoordinateBase coordinateBase = raster.GetCoordinateSystem();
            if (coordinateBase == null || coordinateBase.GetCoordinateType() == CoordinateType.Geographic || coordinateBase.GetCoordinateType() == CoordinateType.Unknown) return false;

            ProjectedCoordinate projectedCoordinate = coordinateBase as ProjectedCoordinate;

            IProjection nowProjection = ProjectionGenerator.GenerateProjection(projectedCoordinate);

            double[] geoTransform = raster.GetGeoTransform();
            double xMin = geoTransform[0];
            double yMax = geoTransform[3];
            int rows = raster.GetRows();
            int cols = raster.GetCols();
            double xScale = geoTransform[1];
            double yScale = geoTransform[5];
            double xMax = xMin + xScale * cols;
            double yMin = yMax - yScale * rows;

            IPoint leftTop = new ThomasGIS.Geometries.Point(xMin, yMax);
            IPoint rightBottom = new ThomasGIS.Geometries.Point(xMax, yMin);

            double a = projectedCoordinate.GeoCoordinate.Datum.Spheroid.SemiMajorAxis;
            double flatten = projectedCoordinate.GeoCoordinate.Datum.Spheroid.InverseFlattening;

            nowProjection.Backward(leftTop, a, flatten);
            nowProjection.Backward(rightBottom, a, flatten);

            double newXScale = (rightBottom.GetX() - leftTop.GetX()) / cols;
            double newYScale = (leftTop.GetY() - rightBottom.GetY()) / rows;

            if (xScale == yScale && newXScale != newYScale)
            {
                newXScale = newYScale;
            }

            raster.SetGeoTransform(leftTop.GetX(), leftTop.GetY(), newXScale, newYScale);

            raster.SetCoordinateSystem(projectedCoordinate.GeoCoordinate);

            return true;
        }

        public static bool DoProject(IRaster raster, IProjection towardProjection)
        {
            CoordinateBase coordinateBase = raster.GetCoordinateSystem();
            if (coordinateBase == null || coordinateBase.GetCoordinateType() == CoordinateType.Unknown) return false;

            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                DeProject(raster);
                coordinateBase = raster.GetCoordinateSystem();
            }

            GeographicCoordinate geographicCoordinate = coordinateBase as GeographicCoordinate;

            double[] geoTransform = raster.GetGeoTransform();
            double xMin = geoTransform[0];
            double yMax = geoTransform[3];
            int rows = raster.GetRows();
            int cols = raster.GetCols();
            double xScale = geoTransform[1];
            double yScale = geoTransform[5];
            double xMax = xMin + xScale * cols;
            double yMin = yMax - yScale * rows;

            IPoint leftTop = new ThomasGIS.Geometries.Point(xMin, yMax);
            IPoint rightBottom = new ThomasGIS.Geometries.Point(xMax, yMin);

            double a = geographicCoordinate.Datum.Spheroid.SemiMajorAxis;
            double flatten = geographicCoordinate.Datum.Spheroid.InverseFlattening;

            towardProjection.Toward(leftTop, a, flatten);
            towardProjection.Toward(rightBottom, a, flatten);

            double newXScale = (rightBottom.GetX() - leftTop.GetX()) / cols;
            double newYScale = (leftTop.GetY() - rightBottom.GetY()) / rows;

            if (xScale == yScale && newXScale != newYScale)
            {
                newXScale = newYScale;
            }

            raster.SetGeoTransform(leftTop.GetX(), leftTop.GetY(), newXScale, newYScale);

            // 投影为投影坐标
            ProjectedCoordinate projectedCoordinate = new ProjectedCoordinate(geographicCoordinate, towardProjection.GetProjectionName());
            Dictionary<string, double> writtenParameters = towardProjection.GetWrittenParameters();
            projectedCoordinate.Projection = towardProjection.GetProjectionType();
            foreach (string key in writtenParameters.Keys)
            {
                projectedCoordinate.Parameters.Add(key, writtenParameters[key]);
            }

            raster.SetCoordinateSystem(projectedCoordinate);

            return true;
        }
    }
}
  