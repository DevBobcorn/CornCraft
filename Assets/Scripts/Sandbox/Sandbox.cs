#nullable enable
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

using CraftSharp.Control;
using CraftSharp.Event;
using CraftSharp.Rendering;
using CraftSharp.UI;

namespace CraftSharp.Sandbox
{
    public class Sandbox : MonoBehaviour
    {
        private static readonly int SHOW = Animator.StringToHash("Show");

        [SerializeField] private PlayerController? playerController;
        [SerializeField] private CameraController? cameraController;
        [SerializeField] private Camera? UICamera;
        [SerializeField] private GameObject? playerRenderPrefab;
        [SerializeField] private TMP_Text? debugText, fpsText;
        [SerializeField] private RingValueBar? staminaBar;
        private Animator? staminaBarAnimator;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Create dummy entity
            var dummyEntity = new Entity(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
            dummyEntity.SetHeadYawFromByte(127);
            dummyEntity.MaxHealth = 20F;

            GameObject renderObj;
            if (playerRenderPrefab!.GetComponent<Animator>() != null) // Model prefab, wrap it up
            {
                renderObj = AnimatorEntityRender.CreateFromModel(playerRenderPrefab);
            }
            else // Player render prefab, just instantiate
            {
                renderObj = GameObject.Instantiate(playerRenderPrefab);
            }
            renderObj!.name = $"Player Entity ({playerRenderPrefab.name})";
            playerController!.UpdatePlayerRender(dummyEntity, renderObj);

            StartCoroutine(DelayedInit());

            staminaBarAnimator = staminaBar!.GetComponent<Animator>();

            staminaCallback = (e) => {
                staminaBar.CurValue = e.Stamina;

                if (staminaBar.MaxValue != e.MaxStamina)
                {
                    staminaBar.MaxValue = e.MaxStamina;
                }

                if (e.Stamina >= e.MaxStamina) // Stamina is full
                {
                    staminaBarAnimator.SetBool(SHOW, false);
                }
                else
                {
                    staminaBarAnimator.SetBool(SHOW, true);
                }
            };

            EventManager.Instance.Register(staminaCallback);
        }

        private Action<StaminaUpdateEvent>? staminaCallback;

        void OnDestroy()
        {
            if (staminaCallback is not null)
                EventManager.Instance.Unregister(staminaCallback);
        }

        void Update()
        {
            if (debugText is not null && playerController is not null)
            {
                debugText.text = playerController.GetDebugInfo();
            }

            if (fpsText is not null)
            {
                fpsText.text = $"FPS: {(int)(1F / Time.deltaTime), 4}";
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                cameraController?.SwitchPerspective();
            }

            // Update stamina bar position
            var targetPosition = UICamera!.ViewportToWorldPoint(
                    cameraController!.GetTargetViewportPos());

            staminaBar!.transform.position = Vector3.Lerp(
                    staminaBar.transform.position, targetPosition, Time.deltaTime * 10F);
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSecondsRealtime(1F);
            EventManager.Instance.Broadcast(new GameModeUpdateEvent(GameMode.Creative));
        }
    }
}