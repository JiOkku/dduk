using UnityEngine;

public enum LobbyStationKind2D { Equipment, Storage, Growth, Dungeon }

[DisallowMultipleComponent]
public sealed class LobbyStation2D : MonoBehaviour
{
    [SerializeField] private LobbyStationKind2D kind;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.5f;
    public LobbyStationKind2D Kind => kind;
    public string Prompt => kind == LobbyStationKind2D.Equipment ? "E: Equipment" :
        kind == LobbyStationKind2D.Storage ? "E: Storage" : kind == LobbyStationKind2D.Growth ? "E: Permanent Growth" : "E: Dungeon Entrance";
    public bool CanInteract(Transform player)
    {
        if (!isActiveAndEnabled || player == null || Vector2.Distance(player.position, transform.position) > Mathf.Max(0.1f, interactionRange)) return false;
        Vector2 delta = transform.position - player.position;
        foreach (var hit in Physics2D.RaycastAll(player.position, delta.normalized, delta.magnitude))
            if (!hit.collider.isTrigger && !hit.collider.transform.IsChildOf(player) && !hit.collider.transform.IsChildOf(transform)) return false;
        return true;
    }
}
