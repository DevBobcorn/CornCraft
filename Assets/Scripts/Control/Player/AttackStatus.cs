#nullable enable

namespace CraftSharp.Control
{
    public class AttackStatus
    {
        // Player attack data
        public PlayerMeleeAttack? CurrentMeleeAttack = null;
        public PlayerRangedAttack? CurrentRangedAttack = null;

        public float AttackCooldown   = 0F;
        public float StageDamageStart = 0F;
        public float StageDamageEnd   = 0F;
        public float StageTime = 0F;
        public int AttackStage =  0;

        public bool CausingDamage = false;

        public override string ToString()
        {
            string cdString;
            if (AttackCooldown > 0F)
            {
                cdString = $"<color=red>{AttackCooldown:0.00}</color>";
            }
            else
            {
                cdString = $"<color=green>{AttackCooldown:0.00}</color>";
            }

            return $"Attack Stage: [{AttackStage}]\tCD: {cdString}";
        }
    }
}