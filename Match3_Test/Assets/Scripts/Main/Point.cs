using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum Shape
{
    Heart,
    Square,
    Romb,
    Circle,
    Rectangle
}

public class Point : MonoBehaviour
{
    public event Action<Point> Selected;
    //Подписка (прогресс модели) для подсчета очков
    public event Action ScoreDetonate;

    [HideInInspector] public int x;
    [HideInInspector] public int y;

    [SerializeField] private Image image;
    [SerializeField] private List<Sprite> iconList;
    [SerializeField] private Button button;

    public Shape shape;

    private bool isSelected;
    private bool isScaled;
    private bool isToDetonate;

    private CancellationTokenSource selectedToken = new CancellationTokenSource();
    private CancellationTokenSource deselectedToken = new CancellationTokenSource();
    private CancellationTokenSource movingToken = new CancellationTokenSource();

    private Vector2 scaleVector = new Vector2(1.25f, 1.25f);

    public bool IsDetonating()
    {
        return isToDetonate;
    }

    //Пометить точку на удаление
    public void SetDetonate(bool isDetonate)
    {
        isToDetonate = isDetonate;
    }

    //Проверка точки на рядом стояющую точку
    public bool IsNear(Point point)
    {
        return (Math.Abs(point.x - x) == 1 && Math.Abs(point.y - y) == 0) 
               || (Math.Abs(point.x - x) == 0 && Math.Abs(point.y - y) == 1);
    }
    
    //Инициализация точки
    public void Setup(int x1, int y1)
    {
        var setupInt = Random.Range(0, 5);
        shape = (Shape)setupInt;
        image.color = Color.white;
        image.sprite = iconList[setupInt];
        x = x1;
        y = y1;
    }

    //Взрыв точки
    public async void Detonate()
    {
        image.color = Color.clear;
        ScoreDetonate?.Invoke();
        await Task.Delay(TimeSpan.FromSeconds(1f));
    }
    
    //Анимация снятия выделения
    public async void Deselect()
    {
        deselectedToken = new CancellationTokenSource();
        float lerp = 0;
        var oldScale = gameObject.transform.localScale;
        while (lerp < 1)
        {
            if (deselectedToken.IsCancellationRequested) return;
            selectedToken?.Cancel();
            gameObject.transform.localScale = Vector3.Lerp(oldScale, Vector3.one, lerp);
            lerp += Time.deltaTime * 2;
            await Task.Yield();
        }
        isScaled = false;
        isSelected = false;
        selectedToken?.Cancel();
        deselectedToken?.Cancel();
    }

    //Перемещение на заданную точку в пространстве
    public async void TryMove(Vector3 newPosition, bool needScaling, bool moveInstantly)
    {
        if (needScaling)
        {
            float scaleLerp = 0;
            var vectorTwo = scaleVector;
            var oldScale = gameObject.transform.localScale;
            while (scaleLerp < 1)
            {
                if (movingToken.IsCancellationRequested) return;
                selectedToken?.Cancel();
                gameObject.transform.localScale = Vector3.Lerp(oldScale, vectorTwo, scaleLerp);
                scaleLerp += Time.deltaTime * 3;
                await Task.Yield();
            }
        }
        var oldPosition = gameObject.transform.position;
        if (!moveInstantly)
        {
            float movingLerp = 0;
            while (movingLerp < 1)
            {
                if (movingToken.IsCancellationRequested) return;
                while (movingLerp < 1)
                {
                    if (movingToken.IsCancellationRequested) return;
                    gameObject.transform.position = Vector3.Lerp(oldPosition, newPosition, movingLerp);
                    movingLerp += Time.deltaTime * 2;
                    await Task.Yield();
                }
                movingLerp += Time.deltaTime;
            }
        }
        else
        {
            gameObject.transform.position = newPosition;
        }
    }
    
    //Выбор точки
    private void Select()
    {
        if (isSelected) return;
        Selected?.Invoke(this);
        isSelected = true;
        ShowSelection();
    }

    //Анимация выбора точки
    private async void ShowSelection()
    {
        float lerp = 0;
        var vectorTwo = scaleVector;
        selectedToken = new CancellationTokenSource();
        while (lerp < 1)
        {
            if (selectedToken.IsCancellationRequested) return;
            if (!isScaled)
            {
                gameObject.transform.localScale = Vector3.Lerp(Vector3.one, vectorTwo, lerp);
            }
            else
            {
                gameObject.transform.localScale = Vector3.Lerp(vectorTwo, Vector3.one,lerp);
            }
            lerp += Time.deltaTime;
            await Task.Yield();
        }
        isScaled = !isScaled;
        if (isSelected) ShowSelection();
    }

    private void Start()
    {
        button.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        selectedToken?.Cancel();
        selectedToken?.Dispose();
        deselectedToken?.Cancel();
        deselectedToken?.Dispose();
    }
}
