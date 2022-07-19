using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
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
    private Vector2 startingPosition;
    
    public bool IsEqual(Point point)
    {
        return point.x == x && point.y == y;
    }

    public bool IsSelected()
    {
        return isSelected;
    }
    
    public bool IsDetonating()
    {
        return isToDetonate;
    }

    public void SetDetonate(bool isDetonate)
    {
        isToDetonate = isDetonate;
    }

    public bool IsNear(Point point)
    {
        return (Math.Abs(point.x - x) == 1 && Math.Abs(point.y - y) == 0) 
               || (Math.Abs(point.x - x) == 0 && Math.Abs(point.y - y) == 1);
    }
    
    public void Setup(int x1, int y1)
    {
        var setupInt = Random.Range(0, 5);
        shape = (Shape)setupInt;
        image.sprite = iconList[setupInt];
        x = x1;
        y = y1;
        startingPosition = transform.position;
    }

    public void Swap(Point pointToSwap)
    {
        (x, pointToSwap.x) = (pointToSwap.x, x);
        (y, pointToSwap.y) = (pointToSwap.y, y);
        (shape, pointToSwap.shape) = (pointToSwap.shape, shape);
    }

    public async void Detonate()
    {
        image.color = Color.clear;
        await Task.Delay(TimeSpan.FromSeconds(1f));
        ScoreDetonate?.Invoke();
    }
    
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

    public async void TryMove(Vector3 newPosition, bool isNewStartingPosition)
    {
        if (isNewStartingPosition)
        {
            startingPosition = newPosition;
        }
        else
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
        float movingLerp = 0;
        var oldPosition = gameObject.transform.position;
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
    
    private void Select()
    {
        if (isSelected) return;
        Selected?.Invoke(this);
        isSelected = true;
        ShowSelection();
    }

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
