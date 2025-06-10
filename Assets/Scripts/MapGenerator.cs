using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class MapGenerator : MonoBehaviour
{
    private int[] floorPlan;
    public int[] getFloorPlan => floorPlan;

    private int floorPlanCount;
    private int minRooms;
    private int maxRooms;
    private List<int> endRooms;

    private int bossRoomIndex;
    private int secretRoomIndex;
    private int shopRoomIndex;
    private int itemRoomIndex;

    public Cell cellPrefab;
    private float cellSize;
    private Queue<int> cellQueue;
    private List<Cell> spawnedCells;

    public List<Cell> getSpawnedCells => spawnedCells;

    private List<int> bigRoomIndexes;

    [Header("Sprite References")]
    [SerializeField] private Sprite item;
    [SerializeField] private Sprite shop;
    [SerializeField] private Sprite boss;
    [SerializeField] private Sprite secret;

    [Header("Room Variations")]
    [SerializeField] private Sprite largeRoom;
    [SerializeField] private Sprite verticalRoom;
    [SerializeField] private Sprite horizontalRoom;
    [SerializeField] private Sprite lShapeRoom;

    public static MapGenerator instance;

    private static readonly List<int[]> roomShapes = new()
    {
        new int[]{-1 },
        new int[]{1 },

        new int[]{10 },
        new int[]{-10 },

        new int[] {1,10 },
        new int[] {1,11 },
        new int[] {10,11 },

        new int[] {9,10 },
        new int[] {-1, 9},
        new int[] {-1,10 },

        new int[] {1, -10 },
        new int[] {1, -9 },
        new int[] {-9, -10 },

        new int[] {-1, -10},
        new int[] {-1, -11 },
        new int[]{-10,-11 },

        new int[] { 1,10,11 },
        new int[] {1,-9,-10 },
        new int[] {-1, 9, 10},
        new int[] {-1, -10, -11}
    };

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        minRooms = 7;
        maxRooms = 15;
        cellSize = 0.5f;
        spawnedCells = new();

        SetupDungeon();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetupDungeon();
        }
    }

    void SetupDungeon() 
    {
        for(int i = 0; i < spawnedCells.Count; i++)
        {
            Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();

        floorPlan = new int[100];
        floorPlanCount = default;
        cellQueue = new Queue<int>();
        endRooms = new List<int>();
        bigRoomIndexes = new List<int>();

        VisitCell(45);

        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        while(cellQueue.Count > 0)
        {
            int index = cellQueue.Dequeue();
            int x = index % 10;

            bool created = false;

            if (x > 1) created |= VisitCell(index - 1);
            if (x < 9) created |= VisitCell(index + 1);
            if (index > 20) created |= VisitCell(index - 10);
            if (index < 70) created |= VisitCell(index + 10);

            if (created == false)
                endRooms.Add(index);
        }

        if(floorPlanCount < minRooms)
        {
            SetupDungeon();
            return;
        }

        CleanEndRoomsList();

        SetupSpecialRooms();
    }

    void CleanEndRoomsList()
    {
        endRooms.RemoveAll(item => bigRoomIndexes.Contains(item) || GetNeighbourCount(item) > 1);
    }

    void SetupSpecialRooms() 
    {
        bossRoomIndex = endRooms.Count > 0 ? endRooms[endRooms.Count - 1] : -1;

        if(bossRoomIndex != -1)
        {
            endRooms.RemoveAt(endRooms.Count - 1);
        }

        itemRoomIndex = RandomEndRoom();
        shopRoomIndex = RandomEndRoom();
        secretRoomIndex = PickSecretRoom();

        if (itemRoomIndex == -1 || shopRoomIndex == -1 || bossRoomIndex == -1 || secretRoomIndex == -1)
        {
            SetupDungeon();
            return;
        }

        SpawnRoom(secretRoomIndex);

        UpdateSpecialRoomVisuals();
        RoomManager.instance.SetupRooms(spawnedCells);
    }

    void UpdateSpecialRoomVisuals() 
    { 
        foreach(var cell in spawnedCells)
        {
            if(cell.index == itemRoomIndex)
            {
                cell.SetSpecialRoomSprite(item);
                cell.SetRoomType(RoomType.Item);
            }

            if(cell.index == shopRoomIndex)
            {
                cell.SetSpecialRoomSprite(shop);
                cell.SetRoomType(RoomType.Shop);
            }

            if(cell.index == bossRoomIndex)
            {
                cell.SetSpecialRoomSprite(boss);
                cell.SetRoomType(RoomType.Boss);
            }

            if(cell.index == secretRoomIndex)
            {
                cell.SetSpecialRoomSprite(secret);
                cell.SetRoomType(RoomType.Secret);
            }
        }
    }

    int RandomEndRoom()
    {
        if (endRooms.Count == 0) return -1;

        int randomRoom = Random.Range(0, endRooms.Count);
        int index = endRooms[randomRoom];

        endRooms.RemoveAt(randomRoom);

        return index;
    }

    int PickSecretRoom()
    {
        for(int attempt = 0; attempt < 900; attempt++)
        {
            int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
            int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;

            int index = y * 10 + x;

            if (floorPlan[index] != 0)
            {
                continue;
            }

            if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 || bossRoomIndex == index + 10 || bossRoomIndex == index - 10)
            {
                continue;
            }

            if (index - 1 < 0 || index + 1 > floorPlan.Length || index - 10 < 0 || index + 10 > floorPlan.Length)
            {
                continue;
            }

            int neighbours = GetNeighbourCount(index);

            if (neighbours >= 3 || (attempt > 300 && neighbours >= 2) || (attempt > 600 && neighbours >= 1))
            {
                return index;
            }
        }

        return -1;
    }

    private int GetNeighbourCount(int index)
    {
        return floorPlan[index - 10] + floorPlan[index - 1] + floorPlan[index + 1] + floorPlan[index + 10];
    }

    private bool VisitCell(int index)
    {
        if (floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || floorPlanCount > maxRooms || Random.value < 0.5f)
            return false;

        if(Random.value < 0.3f && index != 45)
        {
            foreach(var shape in roomShapes.OrderBy(_ => Random.value))
            {
                if(TryPlaceRoom(index, shape))
                {
                    return true;
                }
            }
        }

        cellQueue.Enqueue(index);
        floorPlan[index] = 1;
        floorPlanCount++;

        SpawnRoom(index);

        return true;
    }

    private void SpawnRoom(int index)
    {
        int x = index % 10;
        int y = index / 10;
        Vector2 position = new Vector2(x * cellSize, -y * cellSize);

        Cell newCell = Instantiate(cellPrefab, position, Quaternion.identity);
        newCell.value = 1;
        newCell.index = index;
        newCell.SetRoomShape(RoomShape.OneByOne);
        newCell.SetRoomType(RoomType.Regular);

        newCell.cellList.Add(index);

        spawnedCells.Add(newCell);
    }

    private bool TryPlaceRoom(int origin, int[] offsets)
    {
        List<int> currentRoomIndexes = new List<int>() { origin };

        foreach(var offset in offsets)
        {
            int currentIndexChecked = origin + offset;

            if(currentIndexChecked - 10 < 0 || currentIndexChecked + 10 >= floorPlan.Length)
            {
                return false;
            }

            if (floorPlan[currentIndexChecked] != 0)
            {
                return false;
            }

            if (currentIndexChecked == origin) continue;
            if (currentIndexChecked % 10 == 0) continue;

            currentRoomIndexes.Add(currentIndexChecked);
        }

        if (currentRoomIndexes.Count == 1) return false;

        foreach(int index in currentRoomIndexes)
        {
            floorPlan[index] = 1;
            floorPlanCount++;
            cellQueue.Enqueue(index);

            bigRoomIndexes.Add(index);
        }

        SpawnLargeRoom(currentRoomIndexes);

        return true;
    }

    private void SpawnLargeRoom(List<int> largeRoomIndexes)
    {
        Cell newCell = null;

        int combinedX = default;
        int combinedY = default;
        float offset = cellSize / 2f;

        for(int i = 0; i < largeRoomIndexes.Count; i++)
        {
            int x = largeRoomIndexes[i] % 10;
            int y = largeRoomIndexes[i] / 10;
            combinedX += x;
            combinedY += y;
        }

        if(largeRoomIndexes.Count == 4)
        {
            Vector2 position = new Vector2(combinedX / 4 * cellSize + offset, -combinedY / 4 * cellSize - offset);

            newCell = Instantiate(cellPrefab, position, Quaternion.identity);
            newCell.SetRoomSprite(largeRoom);
            newCell.SetRoomShape(RoomShape.TwoByTwo);
        }

        if(largeRoomIndexes.Count == 3)
        {
            Vector2 position = new Vector2(combinedX / 3 * cellSize + offset, -combinedY / 3 * cellSize - offset);
            newCell = Instantiate(cellPrefab, position, Quaternion.identity);
            newCell.SetRoomSprite(lShapeRoom);
            newCell.RotateCell(largeRoomIndexes);
            newCell.SetRoomShape(RoomShape.LShape);
        }

        if (largeRoomIndexes.Count == 2)
        {
            if (largeRoomIndexes[0] + 10 == largeRoomIndexes[1] || largeRoomIndexes[0] - 10 == largeRoomIndexes[1])
            {
                Vector2 position = new Vector2(combinedX / 2 * cellSize, -combinedY / 2 * cellSize - offset);
                newCell = Instantiate(cellPrefab, position, Quaternion.identity);
                newCell.SetRoomSprite(verticalRoom);
                newCell.SetRoomShape(RoomShape.OneByTwo);
            }
            else if (largeRoomIndexes[0] + 1 == largeRoomIndexes[1] || largeRoomIndexes[0] - 1 == largeRoomIndexes[1])
            {
                Vector2 position = new Vector2(combinedX / 2 * cellSize + offset, -combinedY / 2 * cellSize);
                newCell = Instantiate(cellPrefab, position, Quaternion.identity);
                newCell.SetRoomSprite(horizontalRoom);
                newCell.SetRoomShape(RoomShape.TwoByOne);
            }
        }

        newCell.cellList = largeRoomIndexes;
        newCell.cellList.Sort();
        spawnedCells.Add(newCell);
    }
}
