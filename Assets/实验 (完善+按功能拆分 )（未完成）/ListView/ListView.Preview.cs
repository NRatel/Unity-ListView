using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
#if UNITY_EDITOR
    //[ExecuteInEditMode]
    public partial class ListView
    {
        [SerializeField] private int preViewCount = 0;        //预览数量

        private Dictionary<int, RectTransform> cellRTDict;    //index-Cell字典    
        private Stack<RectTransform> unUseCellRTStack;        //空闲Cell堆栈

        private void Start()
        {
            cellRTDict = new Dictionary<int, RectTransform>();
            unUseCellRTStack = new Stack<RectTransform>();

            onCellAppear = (int index) => 
            {
                RectTransform cellRT = GetOrCreateCell(index);
                cellRT.SetParent(contentRT, false);
                cellRT.anchoredPosition = new Vector2(GetCellPosX(index, cellRT.anchorMin, cellRT.anchorMax, cellRT.pivot), 0);
                cellRT.Find("Text").GetComponent<Text>().text = index.ToString();
                cellRTDict[index] = cellRT;
                cellRT.gameObject.SetActive(true);
            };
            onCellDisAppear = (int index) =>
            {
                RectTransform cellRT = cellRTDict[index];
                cellRTDict.Remove(index);
                unUseCellRTStack.Push(cellRT);
                cellRT.gameObject.SetActive(false);
            };

            this.Init(preViewCount);
        }

        private void OnDestroy()
        {
            //foreach (RectTransform cellRT in cellRTDict.Values)
            //{
            //    GameObject.Destroy(cellRT.gameObject);
            //}
            //foreach (RectTransform cellRT in unUseCellRTStack)
            //{
            //    GameObject.Destroy(cellRT.gameObject);
            //}
            //cellRTDict = null;
            //unUseCellRTStack = null;
        }

        //获取或创建Cell
        private RectTransform GetOrCreateCell(int index)
        {
            RectTransform cellRT;
            if (unUseCellRTStack.Count > 0)
            {
                cellRT = unUseCellRTStack.Pop();
            }
            else
            {
                cellRT = new GameObject("PreviewCell", typeof(RectTransform)).GetComponent<RectTransform>();
                cellRT.sizeDelta = new Vector2(cellWidth, cellHeight);

                GameObject image = new GameObject("Bg", typeof(RectTransform), typeof(Image));
                image.GetComponent<RectTransform>().sizeDelta = new Vector2(cellWidth, cellHeight);
                image.GetComponent<Image>().color = new Color(Random.value, Random.value, Random.value);
                image.transform.SetParent(cellRT, false);

                GameObject text = new GameObject("Text", typeof(RectTransform), typeof(Text));
                text.GetComponent<RectTransform>().sizeDelta = new Vector2(cellWidth, cellHeight);
                text.transform.SetParent(cellRT, false);
                text.GetComponent<Text>().fontSize = 30;
            }

            return cellRT;
        }
    }
#endif
}