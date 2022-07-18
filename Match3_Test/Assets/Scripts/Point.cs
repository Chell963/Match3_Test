using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Point : MonoBehaviour
{
    public int x;
    
    public int y;
    public enum Shape
    {
        Heart,
        Square,
        Romb,
        Circle,
        Rectangle
    }

    [SerializeField] private Image image;

    [SerializeField] private List<Sprite> iconList;

    private Shape pointShape;

    public Shape GetShape()
    {
        return pointShape;
    }

    public void Setup(int x1, int y1)
    {
        var setupInt = Random.Range(0, 5);
        pointShape = (Shape) setupInt;
        image.sprite = iconList[setupInt];
        x = x1;
        y = y1;
    }

    public bool IsEqual(Point point)
    {
        return point.x == x && point.y == y;
    }
}
