using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Grids.GeoTiff
{
    public class TiffDE
    {
        public int ID { get; }

        public int DataType { get; }

        public int DataNumber { get; }

        public byte[] Innerdata { get; set; }


        public int Pointer { get; set; } = -1;

        public TiffDE(int id, int dataType, int dataNumber, byte[] innerData, int pointer = -1)
        {
            this.ID = id;
            this.DataType = dataType;
            this.DataNumber = dataNumber;
            this.Innerdata = new byte[innerData.Length];
            for (int i = 0; i < innerData.Length; i++)
            {
                this.Innerdata[i] = innerData[i];
            }
            this.Pointer = pointer;
        }
    }
}
