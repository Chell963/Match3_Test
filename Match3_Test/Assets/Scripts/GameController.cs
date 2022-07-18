using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private int length = 8;
    
    [SerializeField] private Point basicPoint;

    [SerializeField] private GameObject pointParent;

    [SerializeField] private Canvas mainCanvas;

    private void Start()
    {
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length; j++)
            {
                var newPoint = Instantiate(basicPoint, pointParent.transform);
                newPoint.Setup(j,length-1 - i);
                var pixelRect = mainCanvas.pixelRect;
                newPoint.transform.position = 
                    new Vector2(-350 + j*100 + pixelRect.width/2,350 - i*100 + pixelRect.height/2);
            }
        }
    }
}
