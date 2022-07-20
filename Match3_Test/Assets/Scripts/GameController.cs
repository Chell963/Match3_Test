using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Progress;
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

    [Header("Components")] 
    [SerializeField] private ProgressModel progressModel;
    [SerializeField] private ProgressView progressView;
    
    private List<List<Point>> pointList = new List<List<Point>>();
    private List<Point> selectedPoints = new List<Point>();
    
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
                newPoint.Selected += TrySelect;
                newPoint.ScoreDetonate += progressModel.CountScore;
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
        progressModel.TimeCountdown += progressView.RenderTime;
        progressModel.ScoreEvent += progressView.RenderScore;
        progressModel.GameOver += GameOver;
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
                await DetonateRound();
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

    private async Task DetonateRound()
    {
        var sp0 = selectedPoints[0];
        var sp1 = selectedPoints[1];
        sp0.TryMove(sp1.transform.position,false,false);
        sp1.TryMove(sp0.transform.position,false,false);
        PointSwap(sp0, sp1);
        await Task.Delay(TimeSpan.FromSeconds(1f));
        CheckColumn(sp1.x, sp1.y);
        CheckColumn(sp0.x, sp0.y);
        CheckRow(sp1.x, sp1.y);
        CheckRow(sp0.x, sp0.y);
        if (TryDetonate(false))
        {
            sp0.Deselect();
            sp1.Deselect();
            await LiftDown();
            await LiftFromUp();
            UpdateBoard();
            return;
        }
        sp0.TryMove(sp1.transform.position,false,false);
        sp1.TryMove(sp0.transform.position,false,false);
        PointSwap(sp0, sp1);
    }

    private void PointSwap(Point point1, Point point2)
    {
        (pointList[point1.x][point1.y], pointList[point2.x][point2.y]) 
            = (pointList[point2.x][point2.y], pointList[point1.x][point1.y]);
        (point1.x, point2.x) = (point2.x, point1.x);
        (point1.y, point2.y) = (point2.y, point1.y);
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
                        point.SetDetonate(false);
                    }
                    hasDetonated = true;
                }
            }
        }
        return hasDetonated;
    }

    private async Task LiftDown()
    {
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                if (!pointList[i][j].IsDetonating())
                {
                    var detonationsCount = 0;
                    for (var k = i + 1; k < length; k++)
                    {
                        if (pointList[k][j].IsDetonating())
                        {
                            detonationsCount++;
                        }
                    }
                    if (detonationsCount > 0)
                    {
                        var oldPosition = pointList[i][j].transform.position;
                        var newPosition = 
                            new Vector3(oldPosition.x, oldPosition.y - 100 * detonationsCount, 0);
                        pointList[i][j].x += detonationsCount;
                        pointList[i][j].TryMove(newPosition,true,false);
                    }
                }
            }
        }
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    private async Task LiftFromUp()
    {
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                if (pointList[i][j].IsDetonating())
                {
                    var detonationsCount = 0;
                    for (var k = i-1; k > -1; k--)
                    {
                        if (!pointList[k][j].IsDetonating())
                        {
                            detonationsCount++;
                        }
                    }
                    var oldPosition = pointList[i][j].transform.position;
                    var newMiddlePosition = 
                        new Vector3(oldPosition.x, oldPosition.y + 100 * (detonationsCount + 3), 0);
                    pointList[i][j].x -= detonationsCount;
                    pointList[i][j].TryMove(newMiddlePosition,true,true);
                    var newLastPosition = newMiddlePosition + new Vector3(0,-300,0);
                    pointList[i][j].TryMove(newLastPosition,true,false);
                }
            }
        }
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                var point = pointList[i][j];
                if (point.IsDetonating())
                {
                    point.Setup(point.x,point.y);
                    point.SetDetonate(false);
                }
            }
        }
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    private async void UpdateBoard()
    {
        var updatedPointList = new List<List<Point>>();
        for (var i = 0; i < length; i++)
        {
            updatedPointList.Add(new List<Point>());
        }
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                Point newPoint = null;
                var isFound = false;
                for (var k = 0; k < length; k++)
                {
                    for (var m = 0; m < length; m++)
                    {
                        if (pointList[k][m].x == i && pointList[k][m].y == j)
                        {
                            isFound = true;
                            newPoint = pointList[k][m];
                            break;
                        }
                    }
                    if (isFound) break;
                }
                if (newPoint != null)
                {
                    updatedPointList[i].Add(newPoint);
                }
                else
                {
                    throw new ArgumentException("Point not found by X and Y.");
                }
            }
        }
        pointList = updatedPointList;
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                CheckColumn(i, j);
                CheckRow(i, j);
            }
        }
        while (TryDetonate(false))
        {
            await LiftDown();
            await LiftFromUp();
            UpdateBoard();
        }
    }

    private void GameOver()
    {
        inputBLocker.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        foreach (var points in pointList)
        {
            foreach (var point in points)
            {
                point.Selected -= TrySelect;
                point.ScoreDetonate -= progressModel.CountScore;
            }
        }
        progressModel.TimeCountdown -= progressView.RenderTime;
        progressModel.ScoreEvent -= progressView.RenderScore;
        progressModel.GameOver -= GameOver;
    }
}
