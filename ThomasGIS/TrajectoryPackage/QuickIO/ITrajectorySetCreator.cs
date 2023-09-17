using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ThomasGIS.TrajectoryPackage;

namespace ThomasGIS.TrajectoryPackage.QuickIO
{
    public interface ITrajectorySetCreator
    {
        GnssPoint Reader(string line);

        void PointFilter(Trajectory oneTrajectory);

        IEnumerable<Trajectory> TrajectorySpliter(Trajectory oneTrajectory);

        bool IsJumpTitleLine();

        bool SetJumpTitleLine(bool option);
    }
}
