using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Geometries;
using ThomasGIS.SpatialIndex;

// 基于《数据结构C语言版》实现的BTree
namespace ThomasGIS.SpatialIndex
{
    public class BTreeNode<T>
        where T : IComparable
    {
        // 节点中关键字的个数
        public int maxKeyNumber { get; }

        // 指向双亲节点的指针（C#表示为为双亲对象）
        public BTreeNode<T> parentNode = null;

        // 关键词列表，0号单元未用，基于关键字个数在构造函数中初始化
        public bool isLeaf = true;

        // 子树对象列表
        private List<BTreeNode<T>> childList = new List<BTreeNode<T>>();

        // key对象列表，index = 0 处不存放key
        private List<T> keyList = new List<T>();

        // 节点当前时刻存放的 key 的数量
        public int nowKeyNumber => keyList.Count;

        // 节点当前时刻存放的 child 的数量
        public int nowChildNumber => childList.Count;

        // Node 的构造函数
        public BTreeNode(int keyNumber) 
        {
            this.maxKeyNumber = keyNumber;
        }

        // 获取 node 在 index 处的 key 值
        public T GetKey(int keyIndex) 
        {
            if (keyIndex >= 0 && keyIndex < this.nowKeyNumber)
            {
                return this.keyList[keyIndex];
            }
            throw new IndexOutOfRangeException();
        }

        public BTreeNode<T> GetChild(int childIndex)
        {
            if (childIndex >= 0 && childIndex < nowChildNumber)
            {
                return this.childList[childIndex];
            }

            return null;
        }

        // 向 BTreeNode 中添加一个 T 对象
        public bool AddKey(T value, int index = -1) 
        {
            // 如果当前节点已满，则无法继续添加
            if (nowKeyNumber == maxKeyNumber) return false;
            // 反之则在 index 处添加一个 key
            if (index == -1)
            {
                this.keyList.Add(value);
            }
            else
            {
                this.keyList.Insert(index, value);
            }
            
            // 添加成功
            return true;
        }

        // 向 BTreeNode 中添加一个 child 对象
        public bool AddChild(BTreeNode<T> child, int index = -1)
        {
            // 如果当前节点已满，则无法继续添加
            if (nowChildNumber == maxKeyNumber + 1) return false;
            // 反之则在 index 处添加一个 key
            if (index == -1)
            {
                this.childList.Add(child);
            }
            else
            {
                this.childList.Insert(index, child);
            }
            // 添加成功
            return true;
        }

        // 在当前节点中寻找 target 的位置
        public int SearchTarget(T target) 
        {
            // 第 0 个值不用，从 index = 1 开始搜索，找到比他大的前一个位置
            for (int i = 0; i < this.nowKeyNumber; i++)
            {
                if (this.keyList[i].CompareTo(target) < 0)
                {
                    continue;
                }
                else
                {
                    return i;
                }
            }

            return nowKeyNumber;
        }

        // 弹出 node 最末尾的 key
        public T PopKey(int index = -1) 
        {
            T result;

            if (index == -1) 
            {
                result = this.keyList[nowKeyNumber - 1];
                this.keyList.RemoveAt(nowKeyNumber - 1);
                return result;
            }

            if (index < 0 || index >= this.nowKeyNumber) return default(T);

            result = this.keyList[index];
            this.keyList.RemoveAt(index);
            return result;
        }

        // 弹出 node 最末尾的 child
        public BTreeNode<T> PopChild(int index = -1)
        {
            BTreeNode<T> result;

            if (index == -1)
            {
                result = this.childList[nowChildNumber - 1];
                this.childList.RemoveAt(nowChildNumber - 1);
            }
            else
            {
                if (index < 0 || index >= this.nowChildNumber)
                {
                    result = null;
                }
                else
                {
                    result = this.childList[index];
                    this.childList.RemoveAt(index);
                }
            }

            return result;
        }

        public bool DeleteKey(int index) 
        {
            if (index < 0 || index >= this.nowKeyNumber) return false;
            this.keyList.RemoveAt(index);
            return true;
        }

        public bool DeleteChild(int index)
        {
            if (index < 0 || index >= this.nowChildNumber) return false;
            this.childList.RemoveAt(index);
            return true;
        }
    }

    public class BTreeSearchResult<T>
        where T : IComparable
    {
        public BTreeNode<T> resultNode;
        public int keyIndex;
        public bool isFind;

        public BTreeSearchResult(BTreeNode<T> nowNode, int keyIndex, bool isFind)
        {
            this.resultNode = nowNode;
            this.keyIndex = keyIndex;
            this.isFind = isFind;
        }
    }

    public class BTreeIndex<T>
        where T : IComparable
    {
        private int defaultNodeSize = Convert.ToInt32(Configuration.GetConfiguration("spatialindex.btree.node.size"));
        public BTreeNode<T> root { get; set; } = null;

        public BTreeSearchResult<T> SearchTree(T target) 
        {
            BTreeNode<T> tempRoot = this.root;
            bool isFind = false;
            int thisNodeLocation = 0;

            while (tempRoot != null) 
            {
                // 在当前节点中寻找是否存在
                thisNodeLocation = tempRoot.SearchTarget(target);

                // 判断当前node的location位置的key是否与target相等，如果 target 比所有的都大则直接 false
                if (thisNodeLocation < tempRoot.nowKeyNumber && tempRoot.GetKey(thisNodeLocation).Equals(target))
                {
                    // 相等则返回True
                    isFind = true;
                    break;
                }
                // 否则看该 thisNodeLocation 对应的孩子节点是否存在
                else 
                {
                    // 如果没孩子就说明已经到了叶子节点
                    BTreeNode<T> childNode = tempRoot.GetChild(thisNodeLocation);
                    if (childNode == null)
                    {
                        isFind = false;
                        break;
                    }
                    // 否则向叶子节点继续找
                    else 
                    {
                        tempRoot = childNode;
                    }
                }
            }

            return new BTreeSearchResult<T>(tempRoot, thisNodeLocation, isFind);
        }

        public bool AddItem(T insertData) 
        {
            // 先做一次搜索
            BTreeSearchResult<T> whereToAdd = this.SearchTree(insertData);
            // 如果已经在 B 树中找到了当前数值，则说明该数值已经存在，此时无法继续添加，返回False
            if (whereToAdd.isFind == true)
            {
                Console.WriteLine("Error BT001: Insert value is already existed in this B Tree!");
                return false;
            }
            // B 树中没有找到目标数值，则在当前 Node 的目标位置添加该 Key 并执行后续验证操作
            // whereToAdd 变量中的 resultNode 一定是叶子节点
            else
            {
                // 添加Key
                BTreeNode<T> leafNode = whereToAdd.resultNode;
                leafNode.AddKey(insertData, whereToAdd.keyIndex);

                // 判断节点是否已满，若结点满了就需要进行分裂
                // 这个注释写的我自己都想笑.jpg
                while (leafNode.nowKeyNumber == leafNode.maxKeyNumber)
                {
                    // 把他爹拿出来
                    BTreeNode<T> parentNode = leafNode.parentNode;

                    // 如果他没有爹就造一个爹
                    if (parentNode == null)
                    {
                        parentNode = new BTreeNode<T>(this.defaultNodeSize);
                    }

                    // 找到爹了就再造一个兄弟给爹当儿子.gif
                    BTreeNode<T> brotherNode = new BTreeNode<T>(this.defaultNodeSize);

                    // 把自己的后一半的key和指针无偿奉献给新的兄弟
                    // 3 = 1 + 1 + 1 = 2 + 2 // 4 = 2 + 1 + 1 = 3 + 2
                    // 对于 Key 小于 halfNumber 的自己留着，等于 halfNumber 的给爹，大于 halfNumber 的给兄弟
                    // 对于 Child 小于等于 halfNumber 的自己留着，大于 halfNumber 的给兄弟
                    int halfNumber = leafNode.maxKeyNumber / 2;

                    for (int i = leafNode.maxKeyNumber - 1; i >= halfNumber + 1; i--) 
                    {
                        brotherNode.AddKey(leafNode.PopKey(), 0);
                    }

                    for (int i = leafNode.nowChildNumber; i > halfNumber + 1; i--)
                    {
                        // 这波操作是换爹
                        BTreeNode<T> popedNode = leafNode.PopChild();
                        popedNode.parentNode = brotherNode;
                        brotherNode.AddChild(popedNode, 0);
                    }

                    // 现在自己和兄弟都构建好了，开始认爹
                    leafNode.parentNode = parentNode;
                    brotherNode.parentNode = parentNode;

                    // 这个 key 是给他的爹的
                    T itemForparent = leafNode.PopKey();

                    // 认完爹那还要爹认儿子
                    // 找到在爹中 key 插入的位置，如果爹现在没儿子，那么两个直接进去
                    if (parentNode.nowChildNumber == 0)
                    {
                        parentNode.AddKey(itemForparent);
                        parentNode.AddChild(leafNode);
                        parentNode.AddChild(brotherNode);
                    }
                    // 如果爹现在有儿子，那就找到比 itemForparent 大的位置的前一个
                    else
                    {
                        int insertIndex = -1;
                        for (int i = 0; i < parentNode.nowKeyNumber; i++)
                        {
                            if (parentNode.GetKey(i).CompareTo(itemForparent) >= 0)
                            {
                                insertIndex = i;
                                break;
                            }
                        }

                        if (insertIndex != -1)
                        {
                            parentNode.AddKey(itemForparent, insertIndex);
                            parentNode.AddChild(brotherNode, insertIndex + 1);
                        }
                        else
                        {
                            parentNode.AddKey(itemForparent);
                            parentNode.AddChild(brotherNode);
                        }
                        
                    }

                    // 向上传递，如果爹满了就要去认爷爷.jpg
                    leafNode = parentNode;
                }
            }

            while (this.root.parentNode != null)
            {
                this.root = this.root.parentNode;
            }

            return true;
        }

        public BTreeIndex(int nodeSize = -1)
        {
            if (nodeSize >= 3) 
            {
                this.defaultNodeSize = nodeSize;
            }
            this.root = new BTreeNode<T>(defaultNodeSize);
        }

        public bool RemoveItem(T removeTarget) 
        {
            BTreeSearchResult<T> searchResult = this.SearchTree(removeTarget);

            if (!searchResult.isFind) 
            {
                return false;
            }

            BTreeNode<T> targetLeafNode = searchResult.resultNode;

            // 先判断是不是叶子节点，如果不是要从左边找到最大的顶替它
            if (targetLeafNode.nowChildNumber != 0)
            {
                // 先走当前 key 的左边
                targetLeafNode = targetLeafNode.GetChild(searchResult.keyIndex);

                // 再全部走最右边找到最大值
                while (targetLeafNode.nowChildNumber != 0)
                {
                    targetLeafNode = targetLeafNode.GetChild(targetLeafNode.nowChildNumber - 1);
                }

                // 叶子节点 PopKey
                T replaceValue = targetLeafNode.PopKey();

                // 中间节点的Key替换为 replaceValue
                searchResult.resultNode.DeleteKey(searchResult.keyIndex);
                searchResult.resultNode.AddKey(replaceValue, searchResult.keyIndex);
            }
            else {
                targetLeafNode.DeleteKey(searchResult.keyIndex);
            }

            // 如果不太行.jpg，就是不够满了，不能满足这个节点的最低需求了，那就要寻求兄弟给与一些小小的帮助
            // 如果它已经没有父亲了，就别搞了
            while (targetLeafNode.nowKeyNumber <= Math.Ceiling(targetLeafNode.maxKeyNumber / 2.0) - 2 && targetLeafNode.parentNode != null)
            {
                // 先看一下当前节点在他父亲心中的位置，如果在最右边则向左找兄弟，否则向右找兄弟
                BTreeNode<T> parentNode = targetLeafNode.parentNode;

                // 理论上不可能找不到.jpg
                int nodeInParentIndex = -1;
                for (int i = 0; i < parentNode.nowChildNumber; i++) 
                {
                    // 两个变量的物理地址相同则是一个对象
                    if (parentNode.GetChild(i).GetHashCode() == targetLeafNode.GetHashCode())
                    {
                        nodeInParentIndex = i;
                        break;
                    }
                }

                BTreeNode<T> brotherNode;
                // 向右或者向左找兄弟
                if (nodeInParentIndex == parentNode.nowChildNumber - 1)
                {
                    brotherNode = parentNode.GetChild(nodeInParentIndex - 1);
                    // 左边的兄弟大于最低需求，则将数值做个调换，左边最大的给父亲，父亲当前的给自己，实现平衡
                    if (brotherNode.nowKeyNumber > Math.Ceiling(targetLeafNode.maxKeyNumber / 2.0) - 1)
                    {
                        // 左边的好兄弟需要弹出一个最大的给他的父亲
                        // 顺便把他右边的那个孩子也一起放到右边的最左边
                        T moveKey = brotherNode.PopKey();
                        BTreeNode<T> givenChild = brotherNode.PopChild();

                        // 父节点完成替换
                        T parentKey = parentNode.PopKey(nodeInParentIndex - 1);
                        parentNode.AddKey(moveKey, nodeInParentIndex - 1);

                        // 填充给当前节点
                        targetLeafNode.AddKey(parentKey, 0);
                        if (givenChild != null) 
                        {
                            givenChild.parentNode = targetLeafNode;
                            targetLeafNode.AddChild(givenChild, 0);
                        }
                    }
                    // 左边的兄弟低于最低需求，则全部丢到左边去
                    else 
                    {
                        // 父亲弹一个Key和自己
                        T parentKey = parentNode.PopKey(nodeInParentIndex - 1);
                        parentNode.PopChild(nodeInParentIndex);

                        // key 加给兄弟节点
                        brotherNode.AddKey(parentKey);

                        // 自己的 key 和 child 全部给兄弟，按顺序添加
                        while (targetLeafNode.nowKeyNumber > 0) 
                        {
                            brotherNode.AddKey(targetLeafNode.PopKey(0));
                        }

                        while (targetLeafNode.nowChildNumber > 0) 
                        {
                            // 换爹
                            BTreeNode<T> givenChild = targetLeafNode.PopChild(0);
                            givenChild.parentNode = brotherNode;
                            brotherNode.AddChild(givenChild);
                        }
                    }
                }
                else
                {
                    brotherNode = parentNode.GetChild(nodeInParentIndex + 1);
                    if (brotherNode.nowKeyNumber > Math.Ceiling(targetLeafNode.maxKeyNumber / 2.0) - 1)
                    {
                        // 右边的好兄弟需要弹出一个最小的给他的父亲
                        // 顺便把他左边的那个孩子也一起放到左边的最右边
                        T moveKey = brotherNode.PopKey(0);
                        BTreeNode<T> givenChild = brotherNode.PopChild(0);

                        // 父节点完成替换
                        T parentKey = parentNode.PopKey(nodeInParentIndex);
                        parentNode.AddKey(moveKey, nodeInParentIndex);

                        // 填充给当前节点
                        targetLeafNode.AddKey(parentKey);
                        if (givenChild != null)
                        {
                            givenChild.parentNode = targetLeafNode;
                            targetLeafNode.AddChild(givenChild);
                        }
                    }
                    else
                    {
                        // 父亲弹一个Key和自己
                        T parentKey = parentNode.PopKey(nodeInParentIndex);
                        parentNode.PopChild(nodeInParentIndex);

                        // key 加给兄弟节点前面
                        brotherNode.AddKey(parentKey, 0);

                        // 自己的 key 和 child 全部给兄弟，按逆序添加
                        while (targetLeafNode.nowKeyNumber > 0)
                        {
                            brotherNode.AddKey(targetLeafNode.PopKey(), 0);
                        }

                        while (targetLeafNode.nowChildNumber > 0)
                        {
                            BTreeNode<T> givenChild = targetLeafNode.PopChild(0);
                            givenChild.parentNode = brotherNode;
                            brotherNode.AddChild(givenChild, 0);
                        }
                    }
                }

                targetLeafNode = targetLeafNode.parentNode;
            }

            if (root.nowKeyNumber == 0 && root.nowChildNumber != 0) 
            {
                root = root.GetChild(0);
                root.parentNode = null;
            }

            return true;
        }
    }
}
