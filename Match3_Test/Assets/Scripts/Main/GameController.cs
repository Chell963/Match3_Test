using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Progress;
using UnityEngine;

namespace Main
{
    public class GameController : MonoBehaviour
    {
        private int length = 8;
    
        //Объекты взятые со сцены
        [Header("GameObjects")]
        [SerializeField] private Point basicPoint;
        [SerializeField] private GameObject pointParent;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject inputBlocker;
        [SerializeField] private GameObject gameOverOverlay;

        //Объекты следящие за прогрессом игрока (время, очки)
        [Header("Components")] 
        [SerializeField] private ProgressModel progressModel;
        [SerializeField] private ProgressView progressView;
    
        //Массив всех точек
        private List<List<Point>> pointList = new List<List<Point>>();
        //Массив двух точек выбранных игроком
        private List<Point> selectedPoints = new List<Point>();
    
        private int clicksNumber;
    
        //Инициализация
        private void Start()
        {
            //Инициализация массива точек и подписка на ивенты
            inputBlocker.SetActive(false);
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
            //Проверка на совпадение точек и их смена если успешно
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
        
        //Отписка от событий
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

        //Обработка выбора двух точек игроком
        private async void TrySelect(Point newPoint)
        {
            clicksNumber++;
            selectedPoints.Add(newPoint);
            if (clicksNumber > 1)
            {
                inputBlocker.SetActive(true);
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
                inputBlocker.SetActive(false);
                selectedPoints.Clear();
            }
        }

        //Жизненный цикл раунда, если смена точек привела к результату
        private async Task DetonateRound()
        {
            var sp0 = selectedPoints[0];
            var sp1 = selectedPoints[1];
            //Перемещение
            sp0.TryMove(sp1.transform.position,true,false);
            sp1.TryMove(sp0.transform.position,true,false);
            PointSwap(sp0, sp1);
            await Task.Delay(TimeSpan.FromSeconds(1f));
            //Проверка совпадений в четырех направлениях от двух точек
            CheckColumn(sp1.x, sp1.y);
            CheckColumn(sp0.x, sp0.y);
            CheckRow(sp1.x, sp1.y);
            CheckRow(sp0.x, sp0.y);
            //Попытка детонирования
            if (TryDetonate(false))
            {
                sp0.Deselect();
                sp1.Deselect();
                await LiftDown();
                await LiftFromUp();
                UpdateBoard();
                return;
            }
            //Перемещение обратно
            sp0.TryMove(sp1.transform.position,true,false);
            sp1.TryMove(sp0.transform.position,true,false);
            PointSwap(sp0, sp1);
        }

        //Поменять местами точки в массиве и их Х и У
        private void PointSwap(Point point1, Point point2)
        {
            (pointList[point1.x][point1.y], pointList[point2.x][point2.y]) 
                = (pointList[point2.x][point2.y], pointList[point1.x][point1.y]);
            (point1.x, point2.x) = (point2.x, point1.x);
            (point1.y, point2.y) = (point2.y, point1.y);
        }

        //Проверка столбика на наличие >= 3 одинаковых точек
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

        //Проверка ряда на наличие >= 3 одинаковых точек
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

        //Попытка взорвать точки помеченные для взрыва
        /// <summary>
        /// Важно, точки не уничтожаются, а просто теряют свой цвет
        /// </summary>
        private bool TryDetonate(bool startingDetonation)
        {
            var hasDetonated = false;
            foreach (var points in pointList)
            {
                foreach (var point in points)
                {
                    if (point.IsDetonating())
                    {
                        //Проверка на стартовое событие замены точек
                        if (startingDetonation)
                        {
                            point.Setup(point.x,point.y);
                            point.SetDetonate(false);
                        }
                        else
                        {
                            point.Detonate();
                        }
                        hasDetonated = true;
                    }
                }
            }
            return hasDetonated;
        }

        //Опускание элементов вниз
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
                            pointList[i][j].TryMove(newPosition,false,false);
                        }
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        //Поднятие взорванных элементов наверх и их переинициализация
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
                        pointList[i][j].TryMove(newMiddlePosition,false,true);
                        var newLastPosition = newMiddlePosition + new Vector3(0,-300,0);
                        pointList[i][j].TryMove(newLastPosition,false,false);
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

        //Обновление массива точек и проверка на новые совпадения
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

        //Завершение игры
        private void GameOver()
        {
            inputBlocker.SetActive(true);
            gameOverOverlay.SetActive(true);
        }
    }
}
