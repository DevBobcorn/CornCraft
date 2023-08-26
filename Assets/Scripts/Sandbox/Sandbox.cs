#nullable enable
using System;
using System.Collections;
using UnityEngine;
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
        [SerializeField] private RingValueBar? playerStaminaBar;
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

            staminaBarAnimator = playerStaminaBar!.GetComponent<Animator>();

            staminaCallback = (e) => {
                playerStaminaBar.CurValue = e.Stamina;

                if (e.IsStaminaFull)
                {
                    playerStaminaBar.MaxValue = e.Stamina;
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

            if (Input.GetKeyDown(KeyCode.F5))
            {
                cameraController?.SwitchPerspective();
            }

            // Update stamina bar position
            var targetPosition = UICamera!.ViewportToWorldPoint(
                    cameraController!.GetTargetViewportPos());

            playerStaminaBar!.transform.position = Vector3.Lerp(
                    playerStaminaBar.transform.position, targetPosition, Time.deltaTime * 10F);
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSecondsRealtime(1F);
            EventManager.Instance.Broadcast(new GameModeUpdateEvent(GameMode.Creative));
        }
    }
}