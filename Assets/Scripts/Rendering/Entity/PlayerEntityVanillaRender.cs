#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class PlayerEntityVanillaRender : BipedEntityRender
    {
        [SerializeField] private Renderer[] playerSkinRenderers = { };
        [SerializeField] private Transform? leftArm, rightArm;

        public override void Initialize(Entity entity, Vector3Int originOffset)
        {
            base.Initialize(entity, originOffset);
        }

        public override void UpdateAnimation(float tickMilSec)
        {
            base.UpdateAnimation(tickMilSec);

            leftArm!.localEulerAngles  = new(-currentLegAngle * currentMovFract, 0F, 0F);
            rightArm!.localEulerAngles = new( currentLegAngle * currentMovFract, 0F, 0F);
        }

        public override Dictionary<string, string>? GetControlVariables()
        {
            return new Dictionary<string, string>(){
                ["player_skin"] = $"skin:{UUID}"
            };
        }
    }
}
