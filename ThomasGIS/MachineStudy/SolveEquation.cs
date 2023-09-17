using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.MachineStudy
{
    public class LinearEquation
    {
        public double k;
        public double b;
        public bool kExist;

        public LinearEquation(double k, double b)
        {
            this.k = k;
            this.b = b;
            this.kExist = true;
        }

        // 若k不存在则用b存储 值为x的竖线
        public LinearEquation(double b)
        {
            this.b = b;
            this.k = double.NaN;
            this.kExist = false;
        }

        public IPoint CrossPoint(LinearEquation otherEquation)
        {
            // 平行线
            if (otherEquation.kExist == false && this.kExist == false) return null;

            // other是竖线，自己正常
            if (otherEquation.kExist == false && this.kExist == true)
            {
                return new Point(otherEquation.b, this.k * otherEquation.b + this.b);
            }

            if (otherEquation.kExist == true && this.kExist == false)
            {
                return new Point(this.b, otherEquation.k * this.b + otherEquation.b);
            }

            // k 相等平行
            if (otherEquation.k == this.k) return null;

            double x = (otherEquation.b - this.b) / (this.k - otherEquation.k);
            double y = this.k * x + this.b;

            return new Point(x, y);
        }
    }

    public static class SolveEquation
    {
        public static double MinDifferenceYWithMultipleEquation_RegressionX(List<LinearEquation> equations, double initX = 0, double learnRatio = 0.05)
        {
            double prevX;
            int maxIterationNumber = 100;
            int nowIterationNumber = 0;
            List<double> priceList = new List<double>();
            while (nowIterationNumber < maxIterationNumber)
            {
                double sumDX = 0;
                // 先算出所有的Y值
                List<double> resultY = new List<double>();
                for (int i = 0; i < equations.Count; i++)
                {
                    double oneY = equations[i].k * initX + equations[i].b;
                    resultY.Add(oneY);
                }

                List<double> dxList = new List<double>();
                List<double> dyList = new List<double>();
                double sumDY = 0;
                for (int i = 0; i < equations.Count; i++)
                {
                    for (int j = i + 1; j < equations.Count; j++)
                    {
                        double dy = Math.Abs(resultY[i] - resultY[j]);
                        sumDY += dy;
                        dyList.Add(dy);
                        IPoint crossPoint = equations[i].CrossPoint(equations[j]);
                        if (crossPoint != null)
                        {
                            dxList.Add(crossPoint.GetX() - initX);
                        }
                        else
                        {
                            dxList.Add(0);
                        }
                    }
                }

                for (int i = 0; i < dxList.Count; i++)
                {
                    sumDX += (dxList[i]) * (dyList[i] / sumDY) * learnRatio;
                }
                priceList.Add(sumDY);
                prevX = initX;
                initX += sumDX;
                if (Math.Abs(prevX - initX) < 0.000001)
                {
                    break;
                }
                nowIterationNumber += 1;
            }

            return initX;
        }


        public static double MinDifferenceYWithMultipleEquation_RegressionX_WithABS(List<LinearEquation> equations, double initX = 0, double learnRatio = 0.05)
        {
            double prevX;
            int maxIterationNumber = 100;
            int nowIterationNumber = 0;
            List<double> priceList = new List<double>();
            while (nowIterationNumber < maxIterationNumber)
            {
                double sumDX = 0;
                // 先算出所有的Y值
                List<double> resultY = new List<double>();
                for (int i = 0; i < equations.Count; i++)
                {
                    double oneY = equations[i].k * initX + equations[i].b;
                    resultY.Add(oneY);
                }

                List<double> dxList = new List<double>();
                List<double> dyList = new List<double>();
                double sumDY = 0;
                for (int i = 0; i < equations.Count; i++)
                {
                    for (int j = i + 1; j < equations.Count; j++)
                    {
                        double a1 = equations[i].b / (-equations[i].k);
                        double a2 = equations[j].b / (-equations[j].k);

                        // Abs的处理
                        if (initX > a1)
                        {
                            equations[i].b = -equations[i].b;
                            equations[i].k = -equations[i].k;
                        }

                        if (initX > a2)
                        {
                            equations[j].b = -equations[j].b;
                            equations[j].k = -equations[j].k;
                        }

                        double dy = Math.Abs(resultY[i] - resultY[j]);
                        sumDY += dy;
                        dyList.Add(dy);
                        IPoint crossPoint = equations[i].CrossPoint(equations[j]);
                        if (crossPoint != null)
                        {
                            dxList.Add(crossPoint.GetX() - initX);
                        }
                        else
                        {
                            dxList.Add(0);
                        }

                        // 还原
                        if (initX > a1)
                        {
                            equations[i].b = -equations[i].b;
                            equations[i].k = -equations[i].k;
                        }

                        if (initX > a2)
                        {
                            equations[j].b = -equations[j].b;
                            equations[j].k = -equations[j].k;
                        }
                    }
                }

                for (int i = 0; i < dxList.Count; i++)
                {
                    sumDX += (dxList[i]) * (dyList[i] / sumDY) * learnRatio;
                }
                priceList.Add(sumDY);
                prevX = initX;
                initX += sumDX;
                if (Math.Abs(prevX - initX) < 0.000001)
                {
                    break;
                }
                nowIterationNumber += 1;
            }

            return initX;
        }
    }
}
