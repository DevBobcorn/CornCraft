#nullable enable

namespace CraftSharp.Control
{
    public class AttackStatus
    {
        // Player attack data
        public float AttackCooldown = 0F;

        public override string ToString()
        {
            var cdString = AttackCooldown > 0F ? $"<color=red>{AttackCooldown:0.00}</color>" : $"<color=green>{AttackCooldown:0.00}</color>";

            return $"Attack Cooldown: {cdString}";
        }
    }
}