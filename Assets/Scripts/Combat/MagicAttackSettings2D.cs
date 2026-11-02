using System;
using UnityEngine;

// Each future spell can own one settings instance without changing mana logic.
[Serializable]
public sealed class MagicAttackSettings2D
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0)] private int manaCost = 30;
    [SerializeField, Min(0f)] private float cooldown = 2f;

    public int Damage => Mathf.Max(1, damage);
    public int ManaCost => Mathf.Max(0, manaCost);
    public float Cooldown => Mathf.Max(0f, cooldown);
}
