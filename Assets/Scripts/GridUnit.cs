using UnityEngine;

public class GridUnit : MonoBehaviour
{
    [Header("Unit Profile")]
    public bool isPlayer;
    
    private PlayerStats linkedStats;

    [Header("Tactical Grid Stats")]
    public Vector2Int gridPosition;
    [HideInInspector] public int maxAP; 
    public int currentAP;

    [Header("Visual Settings")]
    [SerializeField] private float movementVisualSpeed = 5f;
    private Vector3 targetWorldPosition;
    private bool isMovingVisual = false;

    // References for Sprite Flipping
    private SpriteRenderer spriteRenderer;

    public string UnitName => linkedStats != null ? linkedStats.playerName : "Unknown Unit";

    private void Awake()
    {
        // Automatically find the SpriteRenderer on this GameObject or any of its child visuals
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(PlayerStats stats)
    {
        linkedStats = stats;
        ResetAP();
    }

    private void Update()
    {
        // Smoothly slide directly to the mathematical cell centers
        if (isMovingVisual)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, movementVisualSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
            {
                transform.position = targetWorldPosition;
                isMovingVisual = false;
            }
        }
    }

    public void ResetAP()
    {
        if (linkedStats != null)
        {
            maxAP = linkedStats.actionPoints;
        }
        currentAP = maxAP;
    }

    public bool UseAP(int amount)
    {
        if (currentAP >= amount)
        {
            currentAP -= amount;
            Debug.Log($"[GRID UNIT] {UnitName} spent {amount} AP. Remaining: {currentAP}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Flips the sprite to look left or right depending on the horizontal direction of a target cell.
    /// Supports pure horizontal, pure diagonal, and maintains facing direction on pure vertical movement.
    /// </summary>
    public void FaceTargetTile(Vector2Int targetTile)
    {
        if (spriteRenderer == null) return;

        int deltaX = targetTile.x - gridPosition.x;

        if (deltaX > 0)
        {
            spriteRenderer.flipX = false;  // Faces Right (Ideal for right + down / right + up diagonals)
        }
        else if (deltaX < 0)
        {
            spriteRenderer.flipX = true; // Faces Left (Ideal for left + down / left + up diagonals)
        }
        // If deltaX == 0, it means pure vertical movement. The sprite maintains its current facing direction.
    }

    /// <summary>
    /// Locks the parent root perfectly to the true mathematical center of the cell.
    /// </summary>
    public void SnapToGridPosition(Vector2Int newGridPos)
    {
        gridPosition = newGridPos;
        
        if (BattleGrid.Instance != null)
        {
            transform.position = BattleGrid.Instance.GridToWorld(newGridPos);
        }
    }

    /// <summary>
    /// Slides the parent root perfectly toward the true mathematical center of the target cell.
    /// Automatically spins the sprite horizontally towards its target location.
    /// </summary>
    public void MoveToGridPosition(Vector2Int newGridPos, int apCost)
    {
        // Turn to look towards our destination tile before altering position vectors
        FaceTargetTile(newGridPos);

        if (UseAP(apCost))
        {
            gridPosition = newGridPos;
            if (BattleGrid.Instance != null)
            {
                targetWorldPosition = BattleGrid.Instance.GridToWorld(gridPosition);
                isMovingVisual = true;
            }
        }
    }

    public int GetDistanceTo(GridUnit otherUnit)
    {
        if (otherUnit == null) return 999;
        
        int dx = Mathf.Abs(this.gridPosition.x - otherUnit.gridPosition.x);
        int dy = Mathf.Abs(this.gridPosition.y - otherUnit.gridPosition.y);
        int dist = Mathf.Max(dx, dy);

        // This will tell us EXACTLY which variable is lying to us
        if (dist > 5) // If the distance is magically high, report the variables
        {
            Debug.LogError($"[MATH ERROR] Dist: {dist} | Me: {this.gridPosition} | Other: {otherUnit.gridPosition} | dx: {dx} | dy: {dy}");
        }
        
        return dist;
    }
}