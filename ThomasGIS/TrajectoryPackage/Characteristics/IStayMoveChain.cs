using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Vector;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public interface IStayMoveChain
    {
        IStay GetStayByIndex(int index);

        IMove GetMoveByIndex(int index);

        IShapefile ExportMoveToShapefile();

        IShapefile ExportStayToShapefile();
    }
}
