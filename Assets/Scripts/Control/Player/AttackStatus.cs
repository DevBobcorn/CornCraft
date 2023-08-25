#nullable enable

namespace CraftSharp.Control
{
    public class AttackStatus
    {
        // Player attack data
        public float AttackCooldown = 0F;
        public int AttackStage = 0;

        public bool CausingDamage = false;

        public override string ToString()
        {
            return $"Attack Stage: [{AttackStage}]\tCD: {AttackCooldown:0.00}";
        }
    }
}