using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private int length = 8;
    
    [Header("GameObjects")]
    [SerializeField] private Point basicPoint;
    [SerializeField] private GameObject pointParent;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Image inputBLocker;
    
    private List<List<Point>> pointList = new List<List<Point>>();
    
    [Header("Counters")]
    private int clicksNumber;
    private float timeSpent;
    private int playerScore;

    private List<Point> selectedPoints = new List<Point>();

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
                newPoint.Selected += TrySelect;
                newList.Add(newPoint);
            }
        }

        do {
            for (var i = 0; i < length; i++)
            {
                for (var j = 0; j < length; j++)
                {
                    CheckColumn(i, j);
                    CheckRow(i, j);
                }
            }
        } while (TryDetonate(true));
    }

    private async void TrySelect(Point newPoint)
    {
        clicksNumber++;
        selectedPoints.Add(newPoint);
        if (clicksNumber > 1)
        {
            inputBLocker.gameObject.SetActive(true);
            clicksNumber = 0;
            if (selectedPoints[0].IsNear(selectedPoints[1]))
            {
                await MarkDetonate();
            }
            foreach (var point in selectedPoints)
            {
                point.Deselect();
            }
            await Task.Delay(TimeSpan.FromSeconds(1f));
            inputBLocker.gameObject.SetActive(false);
            selectedPoints.Clear();
        }
    }

    private async Task MarkDetonate()
    {
        var sp0 = selectedPoints[0];
        var sp1 = selectedPoints[1];
        sp0.TryMove(sp1.transform.position, false);
        sp1.TryMove(sp0.transform.position, false);
        sp0.Swap(sp1);
        await Task.Delay(TimeSpan.FromSeconds(1f));
        CheckColumn(sp1.x, sp1.y);
        CheckColumn(sp0.x, sp0.y);
        CheckRow(sp1.x, sp1.y);
        CheckRow(sp0.x, sp0.y);
        if (!(sp0.IsDetonating() && sp1.IsDetonating()))
        {
            if (sp0.IsDetonating())
            {
                sp0.SetDetonate(false);
                sp1.SetDetonate(true);
            }
            else if (sp1.IsDetonating())
            {
                sp1.SetDetonate(false);
                sp0.SetDetonate(true);
            }
        }
        if (TryDetonate(false)) return;
        sp0.TryMove(sp1.transform.position, false);
        sp1.TryMove(sp0.transform.position, false);
        sp0.Swap(sp1);
    }

    private void CheckColumn(int x1, int y1)
    {
        var boomCount = 0;
        for (var i = 0; i < length; i++)
        {
            if (pointList[x1][y1].shape == pointList[i][y1].shape)
            {
                boomCount++;
                if (boomCount > 2)
                {
                    pointList[i-2][y1].SetDetonate(true);
                    pointList[i-1][y1].SetDetonate(true);
                    pointList[i][y1].SetDetonate(true);
                }
            }
            else
            {
                boomCount = 0;
            }
        }
    }

    private void CheckRow(int x1, int y1)
    {
        var boomCount = 0;
        for (var i = 0; i < length; i++)
        {
            if (pointList[x1][y1].shape == pointList[x1][i].shape)
            {
                boomCount++;
                if (boomCount > 2)
                {
                    pointList[x1][i-2].SetDetonate(true);
                    pointList[x1][i-1].SetDetonate(true);
                    pointList[x1][i].SetDetonate(true);
                }
            }
            else
            {
                boomCount = 0;
            }
        }
    }

    private bool TryDetonate(bool startingDetonation)
    {
        var hasDetonated = false;
        foreach (var points in pointList)
        {
            foreach (var point in points)
            {
                if (point.IsDetonating())
                {
                    if (!startingDetonation)
                    {
                        point.Detonate();
                    }
                    else
                    {
                        point.Setup(point.x,point.y);
                    }
                    hasDetonated = true;
                }
                point.SetDetonate(false);
            }
        }
        return hasDetonated;
    }

    private void OnDestroy()
    {
        foreach (var points in pointList)
        {
            foreach (var point in points)
            {
                point.Selected -= TrySelect;
            }
        }
    }
}
