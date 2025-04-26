using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int index;
    public int value;

    public SpriteRenderer spriteRenderer;
    public SpriteRenderer roomSprite;

    public void SetSpecialRoomSprite(Sprite icon)
    {
        spriteRenderer.sprite = icon;
    }

    public void SetRoomSprite(Sprite roomIcon)
    {
        roomSprite.sprite = roomIcon;
    }

    public void RotateCell(List<int> connectedCells)
    {
        connectedCells.Sort();
        index = connectedCells[0];

        if(connectedCells.Contains(index + 1) && connectedCells.Contains(index + 10))
        {
            ApplyRotation(-90);
        }    
        else if(connectedCells.Contains(index + 1) && connectedCells.Contains(index + 11))
        {
            ApplyRotation(180);
        }
        else if (connectedCells.Contains(index + 9) && connectedCells.Contains(index + 10))
        {
            ApplyRotation(90);
        }
    }

    public void ApplyRotation(float angle)
    {
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
