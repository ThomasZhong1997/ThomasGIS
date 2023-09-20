using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ThomasGIS.DataManagement
{
    public class ByteArrayReader
    {
        public ByteArrayReader(byte[] inData, bool reverseOrder = false)
        {
            mIndex = 0;
            mData = inData;
            mLength = inData.Length;
            mReverse = reverseOrder;
        }

        private bool ReadCheck(int length)
        {
            if (mIndex + length > mLength) return false;

            return true;
        }

        public double ReadDouble()
        {
            if (!ReadCheck(8)) throw new Exception("ByteArrayReader::ReadDouble: Index Out Of Range!");

            byte[] temp = new byte[8];
            if (mReverse)
            {
                for (int i = 0; i < 8; i++)
                {
                    temp[i] = mData[mIndex + 7 - i];
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    temp[i] = mData[mIndex + i];
                }
            }
            mIndex += 8;

            return BitConverter.ToDouble(temp, 0);
        }

        public uint ReadUInt()
        {
            if (!ReadCheck(4)) throw new Exception("ByteArrayReader::ReadUInt: Index Out Of Range!");

            byte[] temp = new byte[4];
            if (mReverse)
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + 3 - i];
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + i];
                }
            }
            mIndex += 4;

            return BitConverter.ToUInt32(temp, 0);
        }

        public int ReadInt() 
        {
            if (!ReadCheck(4)) throw new Exception("ByteArrayReader::ReadInt: Index Out Of Range!");

            byte[] temp = new byte[4];
            if (mReverse)
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + 3 - i];
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + i];
                }
            }
            mIndex += 4;

            return BitConverter.ToInt32(temp, 0);
        }

        public float ReadFloat()
        {
            if (!ReadCheck(4)) throw new Exception("ByteArrayReader::ReadFloat: Index Out Of Range!");

            byte[] temp = new byte[4];
            if (mReverse)
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + 3 - i];
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    temp[i] = mData[mIndex + i];
                }
            }
            mIndex += 4;

            return BitConverter.ToSingle(temp, 0);
        }

        public string ReadString(int length)
        {
            if (!ReadCheck(length)) throw new Exception("ByteArrayReader::ReadString: Index Out Of Range!");

            byte[] temp = new byte[length];
            if (mReverse)
            {
                for (int i = 0; i < length; i++)
                {
                    temp[i] = mData[mIndex + length - i - 1];
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    temp[i] = mData[mIndex + i];
                }
            }
            mIndex += length;

            return BitConverter.ToString(temp, 0);
        }

        public byte ReadByte()
        {
            if (!ReadCheck(1)) throw new Exception("ByteArrayReader::ReadByte: Index Out Of Range!");

            return mData[mIndex++];
        }

        private int mIndex;

        private int mLength;

        private byte[] mData;

        bool mReverse = false;
    }
}
