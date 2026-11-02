using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpawner2D : MonoBehaviour
{
    [SerializeField] private EnemyChase2D enemyPrefab;
    [SerializeField] private Transform player;
    [Tooltip("World-space rectangle INSIDE the surrounding walls.")]
    [SerializeField] private Rect roomBounds = new Rect(-7.5f, -4.5f, 15f, 9f);
    [SerializeField, Min(0f)] private float minimumPlayerDistance = 3f;
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Min(0.01f)] private float minimumSpawnRadius = 0.8f;
    [SerializeField, Min(0f)] private float clearancePadding = 0.1f;
    [SerializeField, Min(1)] private int attemptsPerSpawn = 24;
    [SerializeField] private LayerMask blockingLayers = ~0;

    public float SpawnInterval => Mathf.Max(0.01f, spawnInterval);
    public bool IsConfigured => enemyPrefab != null && enemyPrefab.gameObject.activeSelf && player != null &&
        enemyPrefab.GetComponent<PolygonCollider2D>() != null && roomBounds.width > 0f && roomBounds.height > 0f;

    public bool TrySpawn(out EnemyChase2D enemy)
        => TrySpawn(enemyPrefab, null, null, out enemy);

    public bool TrySpawn(EnemyChase2D prefab, DungeonStageData2D stage, EnemyElementData2D data, out EnemyChase2D enemy)
    {
        enemy = null;
        if (prefab == null || !prefab.gameObject.activeSelf || player == null ||
            prefab.GetComponent<PolygonCollider2D>() == null || roomBounds.width <= 0f || roomBounds.height <= 0f)
            return false;

        // Enclose the prefab's required polygon, including its scale and offset.
        float radius = Mathf.Max(0.01f, minimumSpawnRadius);
        PolygonCollider2D polygon = prefab.GetComponent<PolygonCollider2D>();
        for (int path = 0; path < polygon.pathCount; path++)
        {
            foreach (Vector2 point in polygon.GetPath(path))
            {
                Vector2 scaled = Vector2.Scale(point + polygon.offset, prefab.transform.lossyScale);
                radius = Mathf.Max(radius, scaled.magnitude + Mathf.Max(0f, clearancePadding));
            }
        }

        float minX = roomBounds.xMin + radius;
        float maxX = roomBounds.xMax - radius;
        float minY = roomBounds.yMin + radius;
        float maxY = roomBounds.yMax - radius;
        if (minX > maxX || minY > maxY)
            return false;

        Physics2D.SyncTransforms();
        for (int attempt = 0; attempt < Mathf.Clamp(attemptsPerSpawn, 1, 256); attempt++)
        {
            Vector2 position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (Vector2.Distance(position, player.position) < Mathf.Max(0f, minimumPlayerDistance))
                continue;

            bool blocked = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, radius, blockingLayers))
            {
                if (collider.isTrigger || collider.GetComponentInParent<Projectile2D>() != null)
                    continue;
                blocked = true;
                break;
            }
            if (blocked)
                continue;

            enemy = Instantiate(prefab, new Vector3(position.x, position.y, player.position.z), Quaternion.identity);
            enemy.SetTarget(player);
            if (stage != null && data != null) enemy.ApplyStage(stage, data);
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(roomBounds.center, new Vector3(roomBounds.width, roomBounds.height, 0f));
    }
}
