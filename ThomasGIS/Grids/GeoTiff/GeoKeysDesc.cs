using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;

namespace ThomasGIS.Grids.GeoTiff
{
    public class GeoKeysDesc
    {
        // 编号1024
        /* 
        ModelTypeProjected   = 1  Projection Coordinate System
        ModelTypeGeographic  = 2   Geographic latitude-longitude System 
        ModelTypeGeocentric  = 3   Geocentric (X,Y,Z) Coordinate System 
        */
        public int GTModelTypeGeoKey = -1;

        // 编号1025
        /*
        RasterPixelIsArea  = 1
        RasterPixelIsPoint = 2
        */
        public int GTRasterTypeGeoKey = -1;

        // 编号2048
        public int GeogGeodeticCRSGeoKey = -1;

        // 编号2050
        public int GeogGeodeticDatumGeoKey = -1;

        // 编号2051
        public int GeogPrimeMeridianGeoKey = -1;

        // 编号2052
        public int GeogLinearUnitsGeoKey = -1;

        // 编号2054
        public int GeogAngularUnitsGeoKey = -1;

        // 编号2056
        public int GeogEllipsoidGeoKey = -1;

        // 编号2060
        public int GeogAzimuthUnitGeoKey = -1;

        // 编号3072
        public int ProjectedCRSGeoKey = -1;

        // 编号3074
        public int ProjectionGeoKey = -1;

        // 编号3075
        public int ProjectionMethodGeoKey = -1;

        // 编号3076
        public int ProjLinearUnitGeoKey = -1;

        // 编号4096
        public int VerticalGeoKey = -1;

        // 编号4098
        public int VerticalDatumGeoKey = -1;

        // 编号4099
        public int VerticalUnitSizeGeoKey = -1;

        // 编号2061
        // This key allows definition of user-defined Prime Meridians, the location of which is defined by its longitude relative to Greenwich.
        public double GeogPrimeMeridianLongitudeGeoKey = -1;

        // 编号2053
        // Allows the definition of user-defined linear geocentric units, as measured in meters.
        public double GeogLinearUnitSizeGeoKey = -1;

        // 编号2055
        // Allows the definition of user-defined angular geographic units, as measured in radians.
        public double GeogAngularUnitSizeGeoKey = -1;

        // 编号2057
        public double EllipsoidSemiMajorAxisGeoKey = -1;

        // 编号2058
        public double EllipsoidSemiMinorAxisGeoKey = -1;

        // 编号2059
        public double EllipsoidInvFlatteningGeoKey = -1;

        // 编号3077
        public double ProjLinearUnitSizeGeoKey = -1;

        // 编号3078
        public double ProjStandardParallel_1 = -1;

        // 编号3079
        public double ProjStandardParallel_2 = -1;

        // 编号3080
        public double ProjNaturalOriginLongitude = -1;

        // 编号3081
        public double ProjNaturalOriginLatitude = -1;

        // 编号3082
        public double ProjFalseEasting = -1;

        // 编号3083
        public double ProjFalseNorthing = -1;

        // 编号3084
        public double ProjFalseOriginLongitude = -1;

        // 编号3085
        public double ProjFalseOriginLatitude = -1;

        // 编号3086
        public double ProjFalseOriginEasting = -1;

        // 编号3087
        public double ProjFalseOriginNorthing = -1;

        // 编号3088
        public double ProjCenterLongitude = -1;

        // 编号3089
        public double ProjCenterLatitude = -1;

        // 编号3090
        public double ProjCenterEasting = -1;

        // 编号3091
        public double ProjCenterNorthing = -1;

        // 编号3092
        public double ProjScaleAtNaturalOrigin = -1;

        // 编号3093
        public double ProjScaleAtCenter = -1;

        // 编号3094
        public double ProjProjAzimuthAngleGeoKey = -1;

        // 编号3095
        public double ProjStraightVerticalPole = -1;

        // 编号1026
        // As with all the "Citation" GeoKeys, this is provided to give an ASCII reference to published documentation on the overall configuration of this GeoTIFF file.
        public string GTCitationGeoKey = "";

        // 编号2049
        // General citation and reference for all Geographic CS parameters.
        public string GeogCitationGeoKey = "";

        // 编号3073
        public string ProjectedCitationGeoKey = "";

        // 编号4097
        public string VerticalCitationGeoKey = "";


        public GeoKeysDesc()
        {
            
        }


        private bool ParseGeog(GeographicCoordinate coordinate)
        {
            GeographicCoordinate geographicCoordinate = coordinate;
            // 地理坐标系
            if (GTModelTypeGeoKey == -1)
            {
                GTModelTypeGeoKey = 2;
            } 

            if (geographicCoordinate.Authority != null)
            {
                GeogGeodeticCRSGeoKey = Convert.ToInt32(geographicCoordinate.Authority.SRID);
                GeogCitationGeoKey = $"GCS Name = { geographicCoordinate.Name }";

                if (geographicCoordinate.Datum.Authority != null)
                {
                    GeogGeodeticDatumGeoKey = Convert.ToInt32(geographicCoordinate.Datum.Authority.SRID);
                    GeogCitationGeoKey += $"|Datum = { geographicCoordinate.Datum.Name }";

                    if (geographicCoordinate.Datum.Spheroid.Authority != null)
                    {
                        GeogEllipsoidGeoKey = Convert.ToInt32(geographicCoordinate.Datum.Spheroid.Authority.SRID);
                        GeogCitationGeoKey += $"|Ellipsoid = { geographicCoordinate.Datum.Spheroid.Name }";
                        EllipsoidSemiMajorAxisGeoKey = geographicCoordinate.Datum.Spheroid.SemiMajorAxis;
                        EllipsoidInvFlatteningGeoKey = geographicCoordinate.Datum.Spheroid.InverseFlattening;
                    }

                    if (geographicCoordinate.Primem.Authority != null)
                    {
                        GeogPrimeMeridianGeoKey = Convert.ToInt32(geographicCoordinate.Primem.Authority.SRID);
                        GeogCitationGeoKey += $"|Primem = { geographicCoordinate.Primem.Name }";
                        GeogPrimeMeridianGeoKey = 8901;
                        GeogPrimeMeridianLongitudeGeoKey = geographicCoordinate.Primem.Longitude;
                    }

                    if (geographicCoordinate.Unit.Authority != null)
                    {
                        GeogAngularUnitsGeoKey = Convert.ToInt32(geographicCoordinate.Unit.Authority.SRID);
                        GeogAngularUnitSizeGeoKey = geographicCoordinate.Unit.Value;
                    }
                }
            }
            else
            {
                GeogGeodeticCRSGeoKey = 32767;
                GeogCitationGeoKey = $"GCS Name = { geographicCoordinate.Name }";

                if (geographicCoordinate.Datum.Authority != null)
                {
                    GeogGeodeticDatumGeoKey = Convert.ToInt32(geographicCoordinate.Datum.Authority.SRID);
                }
                else
                {
                    GeogGeodeticDatumGeoKey = 32767;
                    GeogCitationGeoKey += $"|Datum = { geographicCoordinate.Datum.Name }";

                    if (geographicCoordinate.Datum.Spheroid.Authority != null)
                    {
                        GeogEllipsoidGeoKey = Convert.ToInt32(geographicCoordinate.Datum.Spheroid.Authority);
                    }
                    else
                    {
                        GeogEllipsoidGeoKey = 32767;
                        GeogCitationGeoKey += $"|Ellipsoid = { geographicCoordinate.Datum.Spheroid.Name }";
                        EllipsoidSemiMajorAxisGeoKey = geographicCoordinate.Datum.Spheroid.SemiMajorAxis;
                        EllipsoidInvFlatteningGeoKey = geographicCoordinate.Datum.Spheroid.InverseFlattening;
                    }
                }

                if (geographicCoordinate.Primem.Authority != null)
                {
                    GeogPrimeMeridianGeoKey = Convert.ToInt32(geographicCoordinate.Primem.Authority.SRID);
                }
                else
                {
                    GeogPrimeMeridianGeoKey = 32767;
                    GeogCitationGeoKey += $"|Primem = { geographicCoordinate.Primem.Name }";
                    GeogPrimeMeridianLongitudeGeoKey = geographicCoordinate.Primem.Longitude;
                }

                if (geographicCoordinate.Unit.Authority != null)
                {
                    GeogAngularUnitsGeoKey = Convert.ToInt32(geographicCoordinate.Unit.Authority.SRID);
                }
                else
                {
                    GeogAngularUnitsGeoKey = 32767;
                    GeogAngularUnitSizeGeoKey = geographicCoordinate.Unit.Value;
                }
            }

            return true;
        }

        private bool ParseProj(ProjectedCoordinate coordinate)
        {
            GTModelTypeGeoKey = 1;
            if (coordinate.Authority != null)
            {
                ProjectedCRSGeoKey = Convert.ToInt32(coordinate.Authority.SRID);
            }
            else
            {
                ProjectedCRSGeoKey = 32767;
                ProjectionGeoKey = 32767;
                ProjectedCitationGeoKey = "ESRI PE String = " + coordinate.ExportToWkt() + "|";

                ParseGeog(coordinate.GeoCoordinate);

                // 投影对应的编号
                ProjectionMethodGeoKey = ProjMethodValue(coordinate.Projection);

                if (coordinate.Unit.Authority != null)
                {
                    ProjLinearUnitGeoKey = Convert.ToInt32(coordinate.Unit.Authority.SRID);
                }
                else
                {
                    ProjLinearUnitGeoKey = 32767;
                    ProjLinearUnitSizeGeoKey = coordinate.Unit.Value;
                }

                if (ProjectionMethodGeoKey == 1)
                {
                    foreach (KeyValuePair<string, double> pair in coordinate.Parameters)
                    {
                        switch (pair.Key.ToLower())
                        {
                            case "false_easting":
                                ProjFalseEasting = pair.Value;
                                break;
                            case "false_northing":
                                ProjFalseNorthing = pair.Value;
                                break;
                            case "central_meridian":
                                ProjNaturalOriginLongitude = Math.Round(pair.Value, 6);
                                ProjCenterLongitude = Math.Round(pair.Value, 6);
                                break;
                            case "standard_parallel_1":
                                ProjStandardParallel_1 = pair.Value;
                                break;
                            case "standard_parallel_2":
                                ProjStandardParallel_2 = pair.Value;
                                break;
                            case "latitude_of_origin":
                                ProjNaturalOriginLatitude = Math.Round(pair.Value, 6);
                                ProjCenterLatitude = Math.Round(pair.Value, 6);
                                break;
                            case "scale_factor":
                                ProjScaleAtNaturalOrigin = pair.Value;
                                break;
                            case "longitude_of_center":
                                ProjCenterLongitude = pair.Value;
                                break;
                            case "latitude_of_center":
                                ProjCenterLatitude = pair.Value;
                                break;
                            case "azimuth":
                                ProjProjAzimuthAngleGeoKey = pair.Value;
                                break;
                            default:
                                continue;
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, double> pair in coordinate.Parameters)
                    {
                        switch (pair.Key.ToLower())
                        {
                            case "false_easting":
                                ProjFalseEasting = pair.Value;
                                break;
                            case "false_northing":
                                ProjFalseNorthing = pair.Value;
                                break;
                            case "central_meridian":
                                ProjFalseOriginLongitude = pair.Value;
                                break;
                            case "standard_parallel_1":
                                ProjStandardParallel_1 = pair.Value;
                                break;
                            case "standard_parallel_2":
                                ProjStandardParallel_2 = pair.Value;
                                break;
                            case "latitude_of_origin":
                                ProjFalseOriginLatitude = pair.Value;
                                break;
                            case "scale_factor":
                                ProjScaleAtCenter = pair.Value;
                                break;
                            case "longitude_of_center":
                                ProjCenterLongitude = pair.Value;
                                break;
                            case "latitude_of_center":
                                ProjCenterLatitude = pair.Value;
                                break;
                            case "azimuth":
                                ProjProjAzimuthAngleGeoKey = pair.Value;
                                break;
                            default:
                                continue;
                        }
                    }
                }
            }

            return true;
        }

        private int ProjMethodValue(string name)
        {
            switch (name)
            {
                case "Transverse_Mercator":
                case "Transverse_Mercator_NGA_2014":
                case "Transverse_Mercator_Complex":
                    return 1;
                case "Hotine_Oblique_Mercator_Azimuth_Natural_Origin":
                    return 3;
                case "Mercator":
                case "Mercator_Auxiliary_Sphere":
                    return 7;
                case "Lambert_Conformal_Conic":
                    return 8;
                case "Lambert_Azimuthal_Equal_Area":
                    return 10;
                case "Albers":
                    return 11;
                case "Azimuthal_Equidistant":
                case "Azimuthal_Equidistant_Auxiliary_Sphere":
                    return 12;
                case "Equidistant_Conic":
                    return 13;
                case "Stereographic":
                    return 14;
                case "Polar_Stereographic_Variant_A":
                    return 15;
                case "Equidistant_Cylindrical":
                case "Equidistant_Cylindrical_Auxiliary_Sphere":
                    return 17;
                case "Cassini":
                    return 18;
                case "Gnomonic":
                case "Gnomonic_Auxiliary_Sphere":
                    return 19;
                case "Miller_Cylindrical":
                case "Miller_Cylindrical_Auxiliary_Sphere":
                    return 20;
                case "Orthographic":
                case "Orthographic_Auxiliary_Sphere":
                    return 21;
                case "Polyconic":
                    return 22;
                case "Robinson":
                    return 23;
                case "Sinusoidal":
                    return 24;
                case "Van_der_Grinten_I":
                    return 25;
                case "New_Zealand_Map_Grid":
                    return 26;
                default:
                    throw new Exception("当前投影类型无法支持写出至GeoTIFF！");
            }
        }

        public GeoKeysDesc(GeographicCoordinate coordinate)
        {
            GTModelTypeGeoKey = 2;
            GTRasterTypeGeoKey = 1;
            GTCitationGeoKey = $"PCS Name = {coordinate.Name}|";
            ParseGeog(coordinate);
        }

        public GeoKeysDesc(ProjectedCoordinate coordinate)
        {
            GTModelTypeGeoKey = 1;
            GTRasterTypeGeoKey = 1;
            GTCitationGeoKey = $"PCS Name = {coordinate.Name}|";
            ParseProj(coordinate);
        }
    }
}
