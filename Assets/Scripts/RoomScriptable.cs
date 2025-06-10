using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "Scriptable Objects/Room")]
public class RoomScriptable : ScriptableObject
{
    public RoomType roomType;
    public RoomShape roomShape;

    public int[] occupiedTiles;
    public Sprite[] roomVariations;
}
