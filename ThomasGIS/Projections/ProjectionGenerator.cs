using ThomasGIS.Coordinates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public static class ProjectionGenerator
    {
        public static IProjection GenerateProjection(CoordinateBase coordinateBase)
        {
            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                ProjectedCoordinate projectedCoordinate = coordinateBase as ProjectedCoordinate;
                string projectionName = projectedCoordinate.Projection;
                switch (projectionName.ToLower())
                {
                    case "gauss_kruger":
                        return new GaussKrugerProjection(coordinateBase);
                    case "mercator":
                        return new MercatorProjection(coordinateBase);
                    case "transverse_mercator":
                        return new TransverseMercatorProjection(coordinateBase);
                    case "lambert_conformal_conic":
                        return new LambertProjection(coordinateBase);
                    default:
                        throw new Exception("当前暂未支持的投影类型！");
                }
            }

            throw new Exception("非投影坐标系统，无法生成对象！");
        }

        public static List<string> GetNowSupportedProjection()
        {
            List<string> result = new List<string>();
            result.Add("gauss_kruger");
            result.Add("mercator");
            result.Add("transverse_mercator");
            result.Add("lambert_conformal_conic");
            return result;
        }

        public static IProjection GetDefaultTargetProjection(string projectionName)
        {
            switch (projectionName)
            {
                case "gauss_kruger":
                    return new GaussKrugerProjection("GaussKruger", 0);
                case "mercator":
                    return new MercatorProjection("Mercator", 0, 0);
                case "transverse_mercator":
                    return new TransverseMercatorProjection("TransverseMercator", 0, 0);
                case "lambert_conformal_conic":
                    return new LambertProjection("Lambert", 0, 0, 0, 0);
                default:
                    throw new Exception("当前暂未支持的投影类型！");
            }
        }

        public static IProjection GaussKrugerProjection(string name, double L0, double xOffset = 500000.0, double yOffset = 0)
        {
            return new GaussKrugerProjection(name, L0, xOffset, yOffset);
        }

        public static IProjection MecatorProjection(string name, double B0, double L0, double xOffset = 0, double yOffset = 0)
        {
            return new MercatorProjection(name, B0, L0, xOffset, yOffset);
        }

        public static ITransform BD09Transform()
        {
            return new BD09();
        }

        public static ITransform GCJ02Transform()
        {
            return new GCJ02();
        }
    }
}
