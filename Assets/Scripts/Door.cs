using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public void SetDoorSprite(Sprite door)
    {
        spriteRenderer.sprite = door;
    }
}
