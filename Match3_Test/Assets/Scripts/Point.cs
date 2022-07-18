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
    public event Action Selected;

    [HideInInspector] public int x;
    [HideInInspector] public int y;

    [SerializeField] private Image image;

    [SerializeField] private List<Sprite> iconList;

    [SerializeField] private Button button;

    private Shape pointShape;

    private bool isSelected;

    private bool isScaled;

    private CancellationTokenSource selectedToken = new CancellationTokenSource();
    private CancellationTokenSource deselectedToken = new CancellationTokenSource();

    public Shape GetShape()
    {
        return pointShape;
    }

    public void Setup(int x1, int y1)
    {
        var setupInt = Random.Range(0, 5);
        pointShape = (Shape)setupInt;
        image.sprite = iconList[setupInt];
        x = x1;
        y = y1;
    }

    public bool IsEqual(Point point)
    {
        return point.x == x && point.y == y;
    }

    private void Select()
    {
        if (isSelected) return;
        Selected?.Invoke();
        isSelected = true;
        ShowSelection();
    }

    public async void Deselect()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5f));
        selectedToken?.Cancel();
        deselectedToken = new CancellationTokenSource();
        float lerp = 0;
        var oldScale = gameObject.transform.localScale;
        while (lerp < 1)
        {
            if (deselectedToken.IsCancellationRequested)
            {
                return;
            }
            gameObject.transform.localScale = Vector3.Lerp(oldScale, Vector3.one, lerp);
            lerp += Time.deltaTime * 2;
            await Task.Yield();
        }
        isScaled = false;
        isSelected = false;
        selectedToken?.Cancel();
        deselectedToken?.Cancel();
    }

    private async void ShowSelection()
    {
        float lerp = 0;
        var vectorTwo = new Vector3(1.5f, 1.5f, 1.5f);
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
