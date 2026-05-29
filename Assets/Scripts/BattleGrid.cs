using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BattleGrid : MonoBehaviour
{
    public static BattleGrid Instance { get; private set; }

    [Header("Unity Grid Component")]
    [SerializeField] private Grid unityGrid;

    [Header("Environment Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap objectsTilemap;

    [Header("Tactical Highlight Tilemaps")]
    [SerializeField] private Tilemap movementHighlightTilemap;
    [SerializeField] private Tilemap fightHighlightTilemap;

    [Header("Highlight Tile Assets")]
    [SerializeField] private TileBase movementHighlightTile;
    [SerializeField] private TileBase fightHighlightTile;

    private Dictionary<Vector2Int, GridUnit> occupiedTiles = new Dictionary<Vector2Int, GridUnit>();

    private readonly Vector2Int[] cardinalDirections = new Vector2Int[]
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private void Awake()
    {
        Instance = this;
        if (unityGrid == null) unityGrid = GetComponent<Grid>();
    }

    public List<Vector2Int> GetReachableTiles(Vector2Int startPos, int maxAP)
    {
        List<Vector2Int> reachableTiles = new List<Vector2Int>();
        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> apCostMap = new Dictionary<Vector2Int, int>();

        openSet.Enqueue(startPos);
        apCostMap[startPos] = 0;

        while (openSet.Count > 0)
        {
            Vector2Int currentTile = openSet.Dequeue();
            int currentCost = apCostMap[currentTile];

            foreach (Vector2Int dir in cardinalDirections)
            {
                Vector2Int neighborTile = currentTile + dir;
                int nextCost = currentCost + 1;

                if (nextCost > maxAP) continue;
                if (!IsTileWalkable(neighborTile)) continue;
                if (apCostMap.ContainsKey(neighborTile) && apCostMap[neighborTile] <= nextCost) continue;

                apCostMap[neighborTile] = nextCost;
                openSet.Enqueue(neighborTile);

                if (!reachableTiles.Contains(neighborTile)) reachableTiles.Add(neighborTile);
            }
        }
        return reachableTiles;
    }

    public bool IsTileWalkable(Vector2Int gridPos)
    {
        Vector3Int tilemapPos = new Vector3Int(gridPos.x, gridPos.y, 0);
        if (floorTilemap != null && !floorTilemap.HasTile(tilemapPos)) return false;
        if (wallTilemap != null && wallTilemap.HasTile(tilemapPos)) return false;
        if (objectsTilemap != null && objectsTilemap.HasTile(tilemapPos)) return false;
        if (IsTileOccupied(gridPos)) return false;
        return true;
    }

    /// <summary>
    /// Updated to Chebyshev distance to allow diagonal tiles to count as 1.
    /// </summary>
    public int GetManhattanDistance(Vector2Int start, Vector2Int end)
    {
        return Mathf.Max(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y));
    }

    #region HIGHLIGHT OVERLAYS

    public void HighlightMovementTiles(List<Vector2Int> tiles)
    {
        if (movementHighlightTilemap == null || movementHighlightTile == null) return;
        foreach (Vector2Int pos in tiles)
        {
            movementHighlightTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), movementHighlightTile);
        }
    }

    public void HighlightFightTiles(List<Vector2Int> tiles)
    {
        if (fightHighlightTilemap == null || fightHighlightTile == null) return;
        foreach (Vector2Int pos in tiles)
        {
            fightHighlightTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), fightHighlightTile);
        }
    }

    public void ClearAllHighlights()
    {
        if (movementHighlightTilemap != null) movementHighlightTilemap.ClearAllTiles();
        if (fightHighlightTilemap != null) fightHighlightTilemap.ClearAllTiles();
    }

    #endregion

    #region TRANSLATION FUNCTIONS

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return unityGrid.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3Int cellPos = unityGrid.WorldToCell(worldPos);
        return new Vector2Int(cellPos.x, cellPos.y);
    }

    public void RegisterUnitPosition(GridUnit unit)
    {
        RemoveUnitFromTracking(unit);
        occupiedTiles[unit.gridPosition] = unit;
    }

    public void RemoveUnitFromTracking(GridUnit unit)
    {
        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (var pair in occupiedTiles)
        {
            if (pair.Value == unit) keysToRemove.Add(pair.Key);
        }
        foreach (var key in keysToRemove) occupiedTiles.Remove(key);
    }

    public bool IsTileOccupied(Vector2Int gridPos)
    {
        return occupiedTiles.ContainsKey(gridPos);
    }

    #endregion
}