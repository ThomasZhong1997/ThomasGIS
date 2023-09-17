using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage
{
    public class GnssPoint : Point, IComparable
    {
        public string ID { get; set; } = "";
        public double Timestamp { get; set; } = 0;
        public double Speed { get; set; } = -1;
        public double Direction { get; set; } = -1;

        public Dictionary<string, object> ExtraInformation = new Dictionary<string, object>();

        public GnssPoint(string ID, double x, double y, double timeStamp) : base(x, y)
        {
            this.ID = ID;
            this.Timestamp = timeStamp;
        }

        public GnssPoint(string ID, double x, double y, double timeStamp, double direction = -1, double speed = -1) : base(x, y)
        {
            this.ID = ID;
            this.Timestamp = timeStamp;
            this.Direction = direction;
            this.Speed = speed;
        }

        public GnssPoint(string ID, double x, double y, double timeStamp, Dictionary<string, object> extraInfo) : base(x, y)
        {
            this.ID = ID;
            this.Timestamp = timeStamp;
            foreach (string key in extraInfo.Keys)
            {
                this.ExtraInformation.Add(key, extraInfo[key]);
            }
        }

        public GnssPoint(string ID, double x, double y, double timeStamp, double direction, double speed, Dictionary<string, object> extraInfo) : base(x, y)
        {
            this.ID = ID;
            this.Timestamp = timeStamp;
            this.Direction = direction;
            this.Speed = speed;
            foreach (string key in extraInfo.Keys)
            {
                this.ExtraInformation.Add(key, extraInfo[key]);
            }
        }

        public int CompareTo(object obj)
        {
            if (this.Timestamp > ((GnssPoint)obj).Timestamp)
            {
                return 1;
            }
            else if (this.Timestamp == ((GnssPoint)obj).Timestamp)
            {
                return 0;
            }
            else 
            {
                return -1;
            }
        }

        public string toXXXXX()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.ID);
            sb.Append("~");
            sb.Append("~");
            sb.Append(this.ExtraInformation["aistype"]);
            sb.Append("~");
            sb.Append(this.Timestamp);
            sb.Append("~");
            sb.Append(this.X);
            sb.Append("~");
            sb.Append(this.Y);
            sb.Append("~");
            sb.Append(this.ExtraInformation["origin"]);
            sb.Append("~");
            sb.Append(this.ExtraInformation["destination"]);
            sb.Append("~");
            return sb.ToString();
        }
    }
}
