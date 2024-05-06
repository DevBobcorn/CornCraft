using UnityEngine;

namespace CraftSharp.Rendering
{
    [CreateAssetMenu(fileName = "Player Skill Item Config", menuName = "CornCraft/Player Skill Item Config")]
    public class PlayerSkillItemConfig : ScriptableObject
    {
        [SerializeField] public Material DummyItemMaterial;
        [SerializeField] public Mesh DummySwordItemMesh;
        [SerializeField] public Mesh DummyBowItemMesh;
        [SerializeField] public GameObject SwordTrailPrefab;
    }
}