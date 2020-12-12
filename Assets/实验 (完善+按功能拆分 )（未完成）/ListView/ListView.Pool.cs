using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
    public partial class ListView
    {
        private RectTransform cellRTSeed;
        private Dictionary<int, RectTransform> cellRTDict;  //index-Cell字典    
        private Stack<RectTransform> unUseCellRTStack = new Stack<RectTransform>();                //空闲Cell堆栈

        private void InitPool()
        {
            cellRTDict = new Dictionary<int, RectTransform>();
            unUseCellRTStack = new Stack<RectTransform>();
        }

        public void SetSeedOfThePool(RectTransform cellSeed)
        {
            cellSeed.gameObject.SetActive(false);
            this.cellRTSeed = cellSeed;
        }

        private RectTransform TakeoutFromPool(int index)
        {
            RectTransform cellRT;
            if (unUseCellRTStack.Count > 0)
            {
                cellRT = unUseCellRTStack.Pop();
            }
            else
            {
                cellRT = RectTransform.Instantiate(cellRTSeed);
                cellRT.transform.SetParent(contentRT, false);
            }

            cellRTDict[index] = cellRT;
            cellRT.gameObject.SetActive(true);
            return cellRT;
        }

        private void PutbackToPool(int index)
        {
            RectTransform cell = cellRTDict[index];
            cellRTDict.Remove(index);

            unUseCellRTStack.Push(cell);
            cell.gameObject.SetActive(false);
        }
    }
}
