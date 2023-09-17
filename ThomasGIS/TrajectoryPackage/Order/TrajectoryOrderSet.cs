using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Order
{
    public class TrajectoryOrderSet : ITrajectoryOrderSet
    {
        private List<ITrajectoryOrder> innerOrderList;
        public int OrderNumber => this.innerOrderList.Count;

        public TrajectoryOrderSet()
        {
            this.innerOrderList = new List<ITrajectoryOrder>();
        }

        public bool AddOrder(ITrajectoryOrder newOrder)
        {
            innerOrderList.Add(newOrder);
            return true;
        }

        public bool AddOrders(IEnumerable<ITrajectoryOrder> orderList)
        {
            innerOrderList.AddRange(orderList);
            return true;
        }

        public bool DeleteOrder(int index)
        {
            if (index < -OrderNumber || index >= OrderNumber) return false;
            if (index < 0) index += OrderNumber;
            innerOrderList.RemoveAt(index);
            return true;
        }

        public bool DeleteOrder(ITrajectoryOrder deleteOrder)
        {
            bool isDeleted = innerOrderList.Remove(deleteOrder);
            return isDeleted;
        }

        public ITrajectoryOrder GetOrder(int index)
        {
            if (index < -OrderNumber || index >= OrderNumber) throw new IndexOutOfRangeException();
            if (index < 0) index += OrderNumber;
            return innerOrderList[index];
        }

        public int GetOrderNumber()
        {
            return this.innerOrderList.Count;
        }

        public bool ExportToText(string outputPath, char separator)
        {
            if (this.OrderNumber == 0) return false;

            ITrajectoryOrder sampleOrder = this.innerOrderList[0];
            string title = sampleOrder.ExportTitle(separator);

            using (StreamWriter sw = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Write)))
            {
                sw.WriteLine(title);
                foreach (ITrajectoryOrder oneOrder in this.innerOrderList)
                {
                    sw.WriteLine(oneOrder.ExportString(separator));
                }
                sw.Close();
            }

            return false;
        }

        public IEnumerable<ITrajectoryOrder> GetOrderEnumerable()
        {
            return this.innerOrderList;
        }

        public BoundaryBox GetBoundaryBox()
        {
            if (this.GetOrderNumber() == 0) return new BoundaryBox(0, 0, 0, 0);

            double xmin = double.MaxValue;
            double ymin = double.MaxValue;
            double xmax = double.MinValue;
            double ymax = double.MinValue;

            foreach (TrajectoryOrder oneOrder in this.GetOrderEnumerable())
            {
                BoundaryBox oneOrderBox = oneOrder.GetBoundaryBox();
                xmin = Math.Min(xmin, oneOrderBox.XMin);
                xmax = Math.Max(xmax, oneOrderBox.XMax);
                ymin = Math.Min(ymin, oneOrderBox.YMin);
                ymax = Math.Max(ymax, oneOrderBox.YMax);
            }

            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }
    }
}
