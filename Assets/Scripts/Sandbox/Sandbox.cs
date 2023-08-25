#nullable enable
using System.Collections;
using UnityEngine;
using TMPro;

using CraftSharp.Control;
using CraftSharp.Rendering;
using CraftSharp.Event;

namespace CraftSharp.Sandbox
{
    public class Sandbox : MonoBehaviour
    {
        [SerializeField] private PlayerController? playerController;
        [SerializeField] private CameraController? cameraController;
        [SerializeField] private GameObject? playerRenderPrefab;
        [SerializeField] private TMP_Text? debugText, fpsText;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            if (playerRenderPrefab != null) // Specified player render
            {
                // Create dummy entity
                var dummyEntity = new Entity(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
                dummyEntity.SetHeadYawFromByte(127);
                dummyEntity.MaxHealth = 20F;

                GameObject renderObj;
                if (playerRenderPrefab.GetComponent<Animator>() != null) // Model prefab, wrap it up
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
            }
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
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSecondsRealtime(1F);
            EventManager.Instance.Broadcast(new GameModeUpdateEvent(GameMode.Creative));
        }
    }
}