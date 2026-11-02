using UnityEngine;

[CreateAssetMenu(fileName = "EnemyElement", menuName = "Prototype/Enemy Element")]
public sealed class EnemyElementData2D : ScriptableObject
{
    [SerializeField] private MagicElement2D element;
    [SerializeField] private Color color = Color.white;
    public MagicElement2D Element => element;
    public Color Color => color;
}
