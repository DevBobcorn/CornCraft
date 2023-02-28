#nullable enable
using System;
using UnityEngine;

namespace MinecraftClient.Control
{
    public class PlayerAttackManager
    {
        private bool isAttacking = false;
        private int attackStage = 0;
        private float attackCooldown = 0F;

        private Action onStart, onNextStage, onStop;


        public PlayerAttackManager(Action onStart, Action onNextStage, Action onStop)
        {
            this.onStart = onStart;
            this.onNextStage = onNextStage;
            this.onStop = onStop;
        }

        public void TryStart()
        {
            if (attackCooldown <= 0F)
            {
                isAttacking = true;
                attackCooldown = 3F;
                onStart();

                attackStage = 1;
            }
        }

        public void Interrupt()
        {
            isAttacking = false;
            attackStage = 0;
        }

    }
}