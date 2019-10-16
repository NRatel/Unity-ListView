using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRatel
{
    public partial class ListView
    {
        public enum SblingType
        {
            None,
            Left,
            Right,
        }

        public SblingType sortSbling = SblingType.None;

        private List<KeyValuePair<int, RectTransform>> cellRTListForSort;     //Cell列表用于辅助Sbling排序

        //计算并设置Cells的SblingIndex
        private void CalcAndSetCellsSblingIndex()
        {
            if (sortSbling == SblingType.None) { return; }

            if (cellRTListForSort == null)
            {
                cellRTListForSort = new List<KeyValuePair<int, RectTransform>>();
            }
            else
            {
                cellRTListForSort.Clear();
            }

            foreach (KeyValuePair<int, RectTransform> kvp in cellRTDict)
            {
                cellRTListForSort.Add(kvp);
            }

            cellRTListForSort.Sort((x, y) =>
            {
                return x.Key - y.Key; //按index升序
            });

            if (sortSbling == SblingType.Left)
            {
                foreach (KeyValuePair<int, RectTransform> kvp in cellRTListForSort)
                {
                    //索引大的在上
                    kvp.Value.SetAsLastSibling();
                }
            }
            else if (sortSbling == SblingType.Right)
            {
                foreach (KeyValuePair<int, RectTransform> kvp in cellRTListForSort)
                {
                    //索引大的在下
                    kvp.Value.SetAsFirstSibling();
                }
            } 
        }
    }
}

