#nullable enable
using UnityEngine;

using CraftSharp.Control;
using CraftSharp.Rendering;
using CraftSharp.Event;
using System.Collections;

namespace CraftSharp.Sandbox
{
    public class Sandbox : MonoBehaviour
    {
        [SerializeField] private PlayerController? playerController;
        [SerializeField] private GameObject? playerRenderPrefab;

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
                
                playerController?.UpdatePlayerRender(dummyEntity, renderObj);

                StartCoroutine(DelayedInit());
            }
        }

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSecondsRealtime(1F);
            EventManager.Instance.Broadcast(new GameModeUpdateEvent(GameMode.Creative));
        }

        void Update()
        {
            
        }
    }
}