using System.Collections.Generic;
using UnityEngine;

public static class MagicCombat2D
{
    public static bool IsObstacle(Collider2D collider, Collider2D[] owner)
    {
        if (collider.isTrigger || collider.GetComponentInParent<EnemyChase2D>() != null ||
            collider.GetComponentInParent<Projectile2D>() != null) return false;
        if (owner != null) foreach (Collider2D own in owner) if (collider == own) return false;
        return true;
    }
    public static Vector2 ClampTarget(Vector2 origin, Vector2 desired, Collider2D[] owner)
    {
        Vector2 delta = desired - origin;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude))
            if (IsObstacle(hit.collider, owner)) return hit.point - delta.normalized * 0.06f;
        return desired;
    }
    public static void Area(Vector2 center, MagicData2D magic, Collider2D[] owner)
    {
        DamageRegion(center, Vector2.right, magic, owner, false);
        ShowArea(center, magic);
    }
    public static void Cone(Vector2 origin, Vector2 direction, MagicData2D magic, Collider2D[] owner)
    {
        DamageRegion(origin, direction, magic, owner, true);
        LineRenderer line = CreateOutline(origin, magic);
        const int segments = 32;
        line.positionCount = segments + 2;
        line.SetPosition(0, Vector3.zero);
        float start = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - magic.ConeAngle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (start + magic.ConeAngle * i / segments) * Mathf.Deg2Rad;
            line.SetPosition(i + 1, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * magic.Range);
        }
    }
    private static void DamageRegion(Vector2 center, Vector2 direction, MagicData2D magic, Collider2D[] owner, bool cone)
    {
        var hitEnemies = new HashSet<EnemyChase2D>();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(center, cone ? magic.Range : magic.AreaRadius))
        {
            if (collider.isTrigger) continue;
            EnemyChase2D enemy = collider.GetComponentInParent<EnemyChase2D>();
            if (enemy == null || enemy.IsDefeated || hitEnemies.Contains(enemy)) continue;
            if (cone && !magic.ContainsConePoint(center, direction, collider.bounds.center)) continue;
            Vector2 delta = (Vector2)collider.bounds.center - center;
            bool blocked = false;
            foreach (RaycastHit2D obstacle in Physics2D.RaycastAll(center, delta.normalized, delta.magnitude))
                if (IsObstacle(obstacle.collider, owner)) { blocked = true; break; }
            if (blocked) continue;
            hitEnemies.Add(enemy);
            enemy.TryTakeDamage(magic.Damage, out _);
        }
    }
    private static void ShowArea(Vector2 center, MagicData2D magic)
    {
        LineRenderer line = CreateOutline(center, magic);
        line.positionCount = 48;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * magic.AreaRadius);
        }
    }
    private static LineRenderer CreateOutline(Vector2 center, MagicData2D magic)
    {
        GameObject effect = new GameObject("Magic Area: " + magic.Name);
        effect.transform.position = center;
        LineRenderer line = effect.AddComponent<LineRenderer>();
        line.useWorldSpace = false; line.loop = true; line.positionCount = 48;
        line.widthMultiplier = 0.045f; line.sortingOrder = 8;
        line.startColor = line.endColor = magic.EffectColor;
        if (magic.EffectMaterial != null) line.sharedMaterial = magic.EffectMaterial;
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material material = new Material(shader); line.sharedMaterial = material;
                Object.Destroy(material, 0.3f);
            }
        }
        Object.Destroy(effect, 0.25f);
        return line;
    }
}
