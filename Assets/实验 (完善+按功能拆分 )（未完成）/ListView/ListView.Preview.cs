using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NRatel
{
#if UNITY_EDITOR
    public partial class ListView
    {
        [SerializeField] private bool usePreView = true;        //是否启用预览
        [SerializeField] private RectTransform preViewCell;     //预览用Cell
        [SerializeField] private int preViewCount = 0;          //预览数量

        private void TryStartPreview()
        {
            if (!usePreView) { return; }

            onCellAppear = (int index) =>
            {
                RectTransform cellRT = TakeoutFromPool(index);
                cellRT.anchoredPosition = new Vector2(GetCellPosX(index), cellRT.anchoredPosition.y);
            };
            onCellDisAppear = (int index) =>
            {
                PutbackToPool(index);
            };

            SetCellLayoutInfo(preViewCell);
            SetSeedOfThePool(preViewCell);
            Show(preViewCount);
        }
    }
#endif
}