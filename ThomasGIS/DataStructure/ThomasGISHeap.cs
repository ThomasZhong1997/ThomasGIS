using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.DataStructure
{
    public class ThomasGISHeap<T, V> : Dictionary<T, V>
        where V : class, IComparable
    {
        private bool converse = false;
        private int nowMaxSize = 32;
        public T[] orderList;
        private bool heapChanged = false;

        public ThomasGISHeap(bool converse)
        {
            orderList = new T[nowMaxSize];
            this.converse = converse;
        }

        private void ExtendArray()
        {
            T[] newList = new T[nowMaxSize * 2];
            for (int i = 0; i < this.Count; i++)
            {
                newList[i] = this.orderList[i];
            }
            orderList = newList;
            nowMaxSize *= 2;
        }

        public new void Add(T item, V value)
        {
            if (heapChanged)
            {
                ResortHeap();
                heapChanged = false;
            }
            this.orderList[this.Count] = item;
            base.Add(item, value);
            if (this.Count == nowMaxSize - 2) ExtendArray();
            sortHeapUp();
        }

        public void PopItem(out T item, out V value)
        {
            if (heapChanged)
            {
                ResortHeap();
                heapChanged = false;
            }
            item = this.orderList[0];
            value = this[item];
            base.Remove(item);
            this.orderList[0] = this.orderList[this.Count];
            sortHeapDown();
        }

        private void sortHeapDown()
        {
            if (this.Count <= 1) return;

            int i = 0;
            while (i < this.Count / 2)
            {
                int leftChildIndex = i * 2 + 1;
                int rightChildIndex = i * 2 + 2;

                int targetChildIndex = leftChildIndex;
                if (converse && rightChildIndex < this.Count && this[this.orderList[leftChildIndex]].CompareTo(this[this.orderList[rightChildIndex]]) > 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                if (!converse && rightChildIndex < this.Count && this[this.orderList[leftChildIndex]].CompareTo(this[this.orderList[rightChildIndex]]) < 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                bool changed = false;

                if (converse && this[this.orderList[i]].CompareTo(this[this.orderList[targetChildIndex]]) > 0)
                {
                    T temp = this.orderList[i];
                    this.orderList[i] = this.orderList[targetChildIndex];
                    this.orderList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!converse && this[this.orderList[i]].CompareTo(this[this.orderList[targetChildIndex]]) < 0)
                {
                    T temp = this.orderList[i];
                    this.orderList[i] = this.orderList[targetChildIndex];
                    this.orderList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!changed) break;

                i = targetChildIndex;
            }
        }

        private void sortHeapUp()
        {
            for (int i = this.Count - 1; i > 0; i /= 2)
            {
                bool changed = false;
                int parentIndex = i / 2;
                if (converse && this[this.orderList[parentIndex]].CompareTo(this[this.orderList[i]]) > 0)
                {
                    T temp = this.orderList[parentIndex];
                    this.orderList[parentIndex] = this.orderList[i];
                    this.orderList[i] = temp;
                    changed = true;
                }

                if (!converse && this[this.orderList[parentIndex]].CompareTo(this[this.orderList[i]]) < 0)
                {
                    T temp = this.orderList[parentIndex];
                    this.orderList[parentIndex] = this.orderList[i];
                    this.orderList[i] = temp;
                    changed = true;
                }

                if (!changed) break;
            }
        }

        public void SetValue(T item, V value)
        {
            base[item] = value;
            heapChanged = true;
        }

        public new void Remove(T item)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (item.Equals(this.orderList[i]))
                {
                    for (int j = i; j < this.Count - 1; j++)
                    {
                        this.orderList[j] = this.orderList[j + 1];
                    }
                    base.Remove(item);
                    heapChanged = true;
                    break;
                }
            }
        }

        private void ResortHeap()
        {
            for (int i = this.Count / 2 - 1; i >= 0; i--)
            {
                T parentNode = this.orderList[i];
                T leftChild = this.orderList[i * 2 + 1];
                if (converse && this[parentNode].CompareTo(this[leftChild]) > 0)
                {
                    this.orderList[i] = leftChild;
                    this.orderList[i * 2 + 1] = parentNode;
                }

                if (!converse && this[parentNode].CompareTo(this[leftChild]) < 0)
                {
                    this.orderList[i] = leftChild;
                    this.orderList[i * 2 + 1] = parentNode;
                }

                parentNode = this.orderList[i];
                if (i * 2 + 2 < this.Count)
                {
                    T rightChild = this.orderList[i * 2 + 2];
                    if (converse && this[parentNode].CompareTo(this[rightChild]) > 0)
                    {
                        this.orderList[i] = rightChild;
                        this.orderList[i * 2 + 2] = parentNode;
                    }

                    if (!converse && this[parentNode].CompareTo(this[rightChild]) < 0)
                    {
                        this.orderList[i] = rightChild;
                        this.orderList[i * 2 + 2] = parentNode;
                    }
                }
            }
        }
    }

    public class ThomasGISHeapDouble<T> : Dictionary<T, double>
    {
        private bool converse = false;
        private int nowMaxSize = 32;
        public T[] orderList;
        private bool heapChanged = false;

        public ThomasGISHeapDouble(bool converse)
        {
            orderList = new T[nowMaxSize];
            this.converse = converse;
        }

        private void ExtendArray()
        {
            T[] newList = new T[nowMaxSize * 2];
            for (int i = 0; i < this.Count; i++)
            {
                newList[i] = this.orderList[i];
            }
            orderList = newList;
            nowMaxSize *= 2;
        }

        public new void Add(T item, double value)
        {
            if (heapChanged)
            {
                ResortHeap();
                heapChanged = false;
            }
            this.orderList[this.Count] = item;
            base.Add(item, value);
            if (this.Count == nowMaxSize - 2) ExtendArray();
            sortHeapUp();
        }

        public void PopItem(out T item, out double value)
        {
            if (heapChanged)
            {
                ResortHeap();
                heapChanged = false;
            }
            item = this.orderList[0];
            value = this[item];
            base.Remove(item);
            this.orderList[0] = this.orderList[this.Count];
            sortHeapDown();
        }

        private void sortHeapDown()
        {
            if (this.Count <= 1) return;

            int i = 0;
            while (i < this.Count / 2)
            {
                int leftChildIndex = i * 2 + 1;
                int rightChildIndex = i * 2 + 2;

                int targetChildIndex = leftChildIndex;
                if (converse && rightChildIndex < this.Count && this[this.orderList[leftChildIndex]].CompareTo(this[this.orderList[rightChildIndex]]) > 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                if (!converse && rightChildIndex < this.Count && this[this.orderList[leftChildIndex]].CompareTo(this[this.orderList[rightChildIndex]]) < 0)
                {
                    targetChildIndex = rightChildIndex;
                }

                bool changed = false;

                if (converse && this[this.orderList[i]].CompareTo(this[this.orderList[targetChildIndex]]) > 0)
                {
                    T temp = this.orderList[i];
                    this.orderList[i] = this.orderList[targetChildIndex];
                    this.orderList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!converse && this[this.orderList[i]].CompareTo(this[this.orderList[targetChildIndex]]) < 0)
                {
                    T temp = this.orderList[i];
                    this.orderList[i] = this.orderList[targetChildIndex];
                    this.orderList[targetChildIndex] = temp;
                    changed = true;
                }

                if (!changed) break;

                i = targetChildIndex;
            }
        }

        private void sortHeapUp()
        {
            for (int i = this.Count - 1; i > 0; i /= 2)
            {
                bool changed = false;
                int parentIndex = i / 2;
                if (converse && this[this.orderList[parentIndex]].CompareTo(this[this.orderList[i]]) > 0)
                {
                    T temp = this.orderList[parentIndex];
                    this.orderList[parentIndex] = this.orderList[i];
                    this.orderList[i] = temp;
                    changed = true;
                }

                if (!converse && this[this.orderList[parentIndex]].CompareTo(this[this.orderList[i]]) < 0)
                {
                    T temp = this.orderList[parentIndex];
                    this.orderList[parentIndex] = this.orderList[i];
                    this.orderList[i] = temp;
                    changed = true;
                }

                if (!changed) break;
            }
        }

        public void SetValue(T item, double value)
        {
            base[item] = value;
            heapChanged = true;
        }

        public new void Remove(T item)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (item.Equals(this.orderList[i]))
                {
                    for (int j = i; j < this.Count - 1; j++)
                    {
                        this.orderList[j] = this.orderList[j + 1];
                    }
                    base.Remove(item);
                    heapChanged = true;
                    break;
                }
            }
        }

        private void ResortHeap()
        {
            for (int i = this.Count / 2 - 1; i >= 0; i--)
            {
                T parentNode = this.orderList[i];
                T leftChild = this.orderList[i * 2 + 1];
                if (converse && this[parentNode].CompareTo(this[leftChild]) > 0)
                {
                    this.orderList[i] = leftChild;
                    this.orderList[i * 2 + 1] = parentNode;
                }

                if (!converse && this[parentNode].CompareTo(this[leftChild]) < 0)
                {
                    this.orderList[i] = leftChild;
                    this.orderList[i * 2 + 1] = parentNode;
                }

                parentNode = this.orderList[i];
                if (i * 2 + 2 < this.Count)
                {
                    T rightChild = this.orderList[i * 2 + 2];
                    if (converse && this[parentNode].CompareTo(this[rightChild]) > 0)
                    {
                        this.orderList[i] = rightChild;
                        this.orderList[i * 2 + 2] = parentNode;
                    }

                    if (!converse && this[parentNode].CompareTo(this[rightChild]) < 0)
                    {
                        this.orderList[i] = rightChild;
                        this.orderList[i * 2 + 2] = parentNode;
                    }
                }
            }
        }
    }
}
