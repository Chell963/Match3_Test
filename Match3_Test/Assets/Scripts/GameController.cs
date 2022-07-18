using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private int length = 8;
    
    [SerializeField] private Point basicPoint;

    [SerializeField] private GameObject pointParent;

    [SerializeField] private Canvas mainCanvas;

    [SerializeField] private Image inputBLocker;

    private List<List<Point>> pointList = new List<List<Point>>();

    private int clicksNumber;

    private void Start()
    {
        inputBLocker.gameObject.SetActive(false);
        for (var i = 0; i < length; i++)
        {
            pointList.Add(new List<Point>());
            var newList = pointList[i];
            for (var j = 0; j < length; j++)
            {
                var newPoint = Instantiate(basicPoint, pointParent.transform);
                newPoint.Setup(i,j);
                var pixelRect = mainCanvas.pixelRect;
                newPoint.transform.position = 
                    new Vector2(-350 + j*100 + pixelRect.width/2,350 - i*100 + pixelRect.height/2);
                newPoint.Selected += ClicksCount;
                newList.Add(newPoint);
            }
        }
    }

    private async void ClicksCount()
    {
        clicksNumber++;
        if (clicksNumber > 1)
        {
            inputBLocker.gameObject.SetActive(true);
            clicksNumber = 0;
            foreach (var points in pointList)
            {
                foreach (var point in points)
                {
                    point.Deselect();
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(1f));
            inputBLocker.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach (var points in pointList)
        {
            foreach (var point in points)
            {
                point.Selected -= ClicksCount;
            }
        }
    }
}
