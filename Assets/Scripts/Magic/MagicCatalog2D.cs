using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagicCatalog", menuName = "Prototype/Magic Catalog")]
public sealed class MagicCatalog2D : ScriptableObject
{
    [SerializeField] private MagicData2D[] magics;
    public IEnumerable<MagicData2D> All => magics ?? new MagicData2D[0];
    public MagicData2D Find(string id)
    {
        foreach (MagicData2D magic in All) if (magic != null && magic.IsValid && magic.Id == id) return magic;
        return null;
    }
    public bool Validate(out string error)
    {
        var ids = new HashSet<string>();
        foreach (MagicData2D magic in All)
            if (magic == null || !magic.IsValid || !ids.Add(magic.Id))
            { error = "Missing, invalid or duplicate magic ID in catalog."; return false; }
        if (Find(MagicOwnership2D.DefaultId) == null)
        { error = "Catalog requires the default fire_bolt."; return false; }
        error = null; return true;
    }
    public MagicData2D Equipped(RewardProfile2D profile)
    {
        MagicData2D selected = profile != null && profile.ownedMagicIds != null && profile.ownedMagicIds.Contains(profile.equippedMagicId)
            ? Find(profile.equippedMagicId) : null;
        return selected != null ? selected : Find(MagicOwnership2D.DefaultId);
    }
}
