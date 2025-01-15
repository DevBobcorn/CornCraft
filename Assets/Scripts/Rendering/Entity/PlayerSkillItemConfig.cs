using UnityEngine;

namespace CraftSharp.Rendering
{
    [CreateAssetMenu(fileName = "Player Skill Item Config", menuName = "Config/Player Skill Item Config")]
    public class PlayerSkillItemConfig : ScriptableObject
    {
        public Material DummyItemMaterial;

        public Mesh DummySwordItemMesh;
        public Mesh DummyBowItemMesh;

        public GameObject SwordTrailPrefab;

        // Sword ==========================================
        [Header("Sword")]
        public Vector3 SwordLocalScale = new(0.5F, 0.5F, 0.5F);
        [Header("Sword Mount")]
        public Vector3 SwordMountPosition;
        public Vector3 SwordMountEularAngles;
        [Header("Sword Weld (Mainhand)")]
        public Vector3 SwordMainHandPosition;
        public Vector3 SwordMainHandEularAngles;

        // Bow ============================================
        [Header("Bow")]
        public Vector3 BowLocalScale = new(0.5F, 0.5F, 0.5F);
        [Header("Bow Mount")]
        public Vector3 BowMountPosition;
        public Vector3 BowMountEularAngles;
        [Header("Bow Weld (Offhand)")]
        public Vector3 BowOffHandPosition;
        public Vector3 BowOffHandEularAngles;
    }
}