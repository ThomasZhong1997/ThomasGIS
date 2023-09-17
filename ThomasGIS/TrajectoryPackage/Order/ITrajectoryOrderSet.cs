using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Order
{
    public interface ITrajectoryOrderSet
    {
        bool AddOrder(ITrajectoryOrder newOrder);

        bool AddOrders(IEnumerable<ITrajectoryOrder> orderList);

        bool DeleteOrder(int index);

        bool DeleteOrder(ITrajectoryOrder deleteOrder);

        ITrajectoryOrder GetOrder(int index);

        int GetOrderNumber();

        bool ExportToText(string outputPath, char separator);

        IEnumerable<ITrajectoryOrder> GetOrderEnumerable();

        BoundaryBox GetBoundaryBox();
    }
}
