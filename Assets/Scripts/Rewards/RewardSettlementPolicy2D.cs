using UnityEngine;

[CreateAssetMenu(fileName = "RewardSettlementPolicy", menuName = "Prototype/Reward Settlement Policy")]
public sealed class RewardSettlementPolicy2D : ScriptableObject
{
    [SerializeField] private bool exportOnClear = true;
    [SerializeField] private bool exportOnDeath;
    public bool AllowsExport(bool cleared) => cleared ? exportOnClear : exportOnDeath;
}
