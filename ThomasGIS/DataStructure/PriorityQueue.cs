using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.DataStructure
{
    // 优先队列
    public class PriorityQueue<T>
        where T : IComparable
    {
        private T[] keyList;
        private int nowItemCount;
        private int maxItemCount;
        private bool converse;

        public int Count => nowItemCount;

        public PriorityQueue(bool converse = false, int initSize = 32)
        {
            if (initSize <= 16) 
            {
                initSize = 32;
            }
            this.keyList = new T[initSize];
            this.nowItemCount = 0;
            this.maxItemCount = initSize;
            this.converse = converse;
        }

        public bool IsEmpty()
        {
            return this.Count == 0 ? true : false;
        }

        public T Peek()
        {
            return this.keyList[0];
        }

        private void sortHeapUp()
        {
            for (int i = this.nowItemCount - 1; i > 0; i /= 2)
            {
                bool changed = false;
                int parentIndex = i / 2;
                if (converse && this.keyList[parentIndex].CompareTo(this.keyList[i]) > 0)
                {
                    T temp = this.keyList[parentIndex];
                    this.keyList[parentIndex] = this.keyList[i];
                    this.keyList[i] = temp;
                    changed = true;
                }
                
                if (!converse && this.keyList[parentIndex].CompareTo(this.keyList[i]) < 0)
                {
                    T temp = this.keyList[parentIndex];
                    this.keyList[parentIndex] = this.keyList[i];
                    this.keyList[i] = temp;
                    changed = true;
                }

                if (!changed) break;
            }
        }

        private void sortHeapDown()
        {
            if (this.nowItemCount <= 1) return;

            int i = 0;
            while (i < this.nowItemCount / 2)
            {
                int leftChildIndex = i * 2 + 1;
                int rightChildIndex = i * 2 + 2;

                int targetChildIndex = leftChildIndex;
                if (converse && rightChildIndex < this.nowItemCount && this.keyList[leftChildIndex].CompareTo(this.keyList[rightChildIndex]) > 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                if (!converse && rightChildIndex < this.nowItemCount && this.keyList[leftChildIndex].CompareTo(this.keyList[rightChildIndex]) < 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                bool changed = false;

                if (converse && this.keyList[i].CompareTo(this.keyList[targetChildIndex]) > 0)
                {
                    T temp = this.keyList[i];
                    this.keyList[i] = this.keyList[targetChildIndex];
                    this.keyList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!converse && this.keyList[i].CompareTo(this.keyList[targetChildIndex]) < 0)
                {
                    T temp = this.keyList[i];
                    this.keyList[i] = this.keyList[targetChildIndex];
                    this.keyList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!changed) break;

                i = targetChildIndex;
            }
        }

        public T Pop()
        {
            T result = this.keyList[0];
            keyList[0] = keyList[--this.nowItemCount];
            sortHeapDown();
            return result;
        }

        public T GetValueAndPopAt(int index)
        {
            T result = this.keyList[index];
            keyList[index] = keyList[--this.nowItemCount];
            ResortHeap();
            return result;
        }

        public bool Add(T key)
        {
            this.keyList[nowItemCount++] = key;
            if (nowItemCount == maxItemCount)
            {
                T[] newList = new T[maxItemCount * 2];
                maxItemCount = maxItemCount * 2;
                for (int i = 0; i < nowItemCount; i++)
                {
                    newList[i] = this.keyList[i];
                }
                this.keyList = newList;
            }

            sortHeapUp();

            return true;
        }

        public T At(int index)
        {
            if (index < -this.nowItemCount && index >= this.nowItemCount) return default(T);

            if (index < 0) index += this.nowItemCount;

            return this.keyList[index];
        }

        public void ResortHeap()
        {
            for (int i = this.nowItemCount / 2 - 1; i >= 0; i--)
            {
                T parentNode = this.keyList[i];
                T leftChild = this.keyList[i * 2 + 1];
                if (converse && parentNode.CompareTo(leftChild) > 0)
                {
                    this.keyList[i] = leftChild;
                    this.keyList[i * 2 + 1] = parentNode;
                }

                if (!converse && parentNode.CompareTo(leftChild) < 0)
                {
                    this.keyList[i] = leftChild;
                    this.keyList[i * 2 + 1] = parentNode;
                }

                parentNode = this.keyList[i];
                if (i * 2 + 2 < this.nowItemCount)
                {
                    T rightChild = this.keyList[i * 2 + 2];
                    if (converse && parentNode.CompareTo(rightChild) > 0)
                    {
                        this.keyList[i] = rightChild;
                        this.keyList[i * 2 + 2] = parentNode;
                    }

                    if (!converse && parentNode.CompareTo(rightChild) < 0)
                    {
                        this.keyList[i] = rightChild;
                        this.keyList[i * 2 + 2] = parentNode;
                    }
                }
            }
        }
    }
}
