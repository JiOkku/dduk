using System.Collections.Generic;
using UnityEngine;

// Existing equip/save path reused by the equipment station; no always-visible panel.
[DefaultExecutionOrder(10)]
[DisallowMultipleComponent]
public sealed class LobbyMagicUI2D : MonoBehaviour
{
    [SerializeField] private MagicCatalog2D catalog;
    [SerializeField] private PermanentGrowthData2D growthData;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool unlockAllForTesting;
    private bool testingUnlockPending;
#endif
    private bool refreshPending;
    public MagicCatalog2D Catalog => catalog;
    public RewardProfile2D Profile => RewardBank2D.GrowthSnapshot(growthData);
    private void OnEnable() => RewardBank2D.Changed += QueueRefresh;
    private void OnDisable() => RewardBank2D.Changed -= QueueRefresh;
    private void QueueRefresh() => refreshPending = true;
    private void Start()
    {
        if (catalog == null || !catalog.Validate(out _))
        { Debug.LogError("Connect a valid Magic Catalog to LobbyMagicUI2D.", this); enabled = false; return; }
        RefreshEquipment();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        testingUnlockPending = unlockAllForTesting;
#endif
    }
    private void Update()
    {
        if (refreshPending) { refreshPending = false; RefreshEquipment(); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (testingUnlockPending && SceneFlow2D.InLobby) { testingUnlockPending = false; UnlockAll(); }
#endif
    }
    private void RefreshEquipment()
    {
        var lobby = GetComponent<LobbyController2D>();
        if (lobby != null && catalog != null) lobby.SetMagicName(catalog.Equipped(Profile).Name);
    }
    public bool Equip(string id)
    {
        if (!isActiveAndEnabled || !SceneFlow2D.InLobby || catalog.Find(id) == null) return false;
        bool saved = RewardBank2D.TrySelectMagic(id, growthData);
        if (!saved) Debug.LogWarning(RewardBank2D.Error, this);
        RefreshEquipment(); return saved;
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Development/Unlock All Test Magic Scrolls")]
    private void UnlockAll()
    {
        if (!SceneFlow2D.InLobby || catalog == null || !catalog.Validate(out _)) return;
        var ids = new List<string>(); foreach (var magic in catalog.All) ids.Add(magic.Id);
        if (!RewardBank2D.TryUnlockMagics(ids)) Debug.LogWarning(RewardBank2D.Error, this);
        RefreshEquipment();
    }
#endif
}
