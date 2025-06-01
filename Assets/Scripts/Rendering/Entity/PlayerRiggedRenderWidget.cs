using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Control;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    [RequireComponent(typeof (Animator))]
    public class PlayerRiggedRenderWidget : MonoBehaviour
    {
        private Transform _spineRef;

        public Vector3 m_VisualOffset = new(0F, 0.1F, 0F);
        public Vector3 m_FollowOffset = new(0F, 1.5F, 0F);
        public Vector2 m_ClimbOverOffset = new(0F, 0F);
        public Transform m_AimingRef;

        public bool m_UseAuxOffhandTransform = false;

        private Transform _mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform _offHandSlot; // A slot fixed to offHandRef transform (as a child)
        private Transform _itemMountPivot, _itemMountSlot;
        private PlayerController _player;
        private PlayerActionItem _currentItem;
        private Animator _playerAnimator;

        public void SetRefTransforms(Transform mainHandRef, Transform offHandRef, Transform spineRef)
        {
            transform.localPosition = m_VisualOffset;

            // Create main hand slot transform
            var mainHandSlotObj = new GameObject("Main Hand Slot");
            _mainHandSlot = mainHandSlotObj.transform;
            _mainHandSlot.SetParent(mainHandRef);
            // Initialize position and rotation
            _mainHandSlot.localPosition = Vector3.zero;
            _mainHandSlot.localEulerAngles = Vector3.zero;

            // Create off hand slot transform
            var offHandSlotObj = new GameObject("Off Hand Slot");
            _offHandSlot = offHandSlotObj.transform;

            if (m_UseAuxOffhandTransform) {
                // Use an extra aux transform to fix hand orientation
                var auxObj = new GameObject("Aux");
                var auxRef = auxObj.transform;
                auxRef.SetParent(offHandRef);
                auxRef.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0F, 0F, 180F));

                _offHandSlot.SetParent(auxRef);
            } else {
                _offHandSlot.SetParent(offHandRef);
            }

            // Initialize position and rotation
            _offHandSlot.localPosition = Vector3.zero;
            _offHandSlot.localEulerAngles = Vector3.zero;

            _spineRef = spineRef;

            // Create weapon mount slot transform
            var itemMountPivotObj = new GameObject("Item Mount Pivot");
            _itemMountPivot = itemMountPivotObj.transform;
            _itemMountPivot.SetParent(transform);

            var itemMountSlotObj = new GameObject("Item Mount Slot");
            _itemMountSlot = itemMountSlotObj.transform;
            _itemMountSlot.SetParent(_itemMountPivot);
        }

        private void CreateActionItem(ItemStack itemStack, ItemActionType actionType, PlayerSkillItemConfig psi)
        {
            DestroyActionItem();

            var itemObj = new GameObject($"Action Item ({itemStack?.DisplayName})")
            {
                layer = gameObject.layer
            };

            (Mesh mesh, Material material, Dictionary<DisplayPosition, float3x3> transforms)? meshData = null;

            switch (actionType)
            {
                case ItemActionType.Sword:
                    _currentItem = itemObj.AddComponent<MeleeWeapon>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummySwordItemMesh!, psi.DummyItemMaterial!, new());

                    _itemMountSlot!.localEulerAngles = psi.SwordMountEulerAngles;
                    _itemMountSlot!.localPosition = psi.SwordMountPosition;

                    _mainHandSlot!.localEulerAngles = psi.SwordMainHandEulerAngles;
                    _mainHandSlot!.localPosition = psi.SwordMainHandPosition;

                    /*
                    var trailObj = GameObject.Instantiate(psi.SwordTrailPrefab);
                    trailObj.transform.parent = itemObj.transform;
                    trailObj.transform.localPosition = new(0.5F, 0.65F, 0.65F);

                    var sword = _currentItem as MeleeWeapon;
                    sword!.SlashTrail = trailObj.GetComponent<TrailRenderer>();
                    */

                    itemObj.transform.localScale = psi.SwordLocalScale;

                    break;
                case ItemActionType.Bow:
                    _currentItem = itemObj.AddComponent<UselessActionItem>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummyBowItemMesh!, psi.DummyItemMaterial!, new());

                    _itemMountSlot!.localEulerAngles = psi.BowMountEulerAngles;
                    _itemMountSlot!.localPosition = psi.BowMountPosition;
                    
                    _offHandSlot!.localEulerAngles = psi.BowOffHandEulerAngles;
                    _offHandSlot!.localPosition = psi.BowOffHandPosition;
                    
                    itemObj.transform.localScale = psi.BowLocalScale;
                    break;
                default:
                    // No visual
                    break;
            }

            if (meshData is not null) // In case of invalid items, meshData can be null
            {
                var mesh = itemObj.AddComponent<MeshFilter>();
                mesh.mesh = meshData.Value.mesh;

                var renderer = itemObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = meshData.Value.material;
            }

            // Set weapon mount pivot position
            _itemMountPivot!.localPosition = new(0F, 1.3F, 0F);

            // Mount weapon on start
            MoveItemToWidgetSlot(_itemMountSlot!);
        }

        /// <summary>
        /// Properly destroy a gameobject, in either editor mode, play mode, or an actual build.
        /// See https://forum.unity.com/threads/editor-and-destroyimmediate.1261745/
        /// </summary>
        /// <param name="targetGameObject"></param>
        private static void SafeDestroy(GameObject? targetGameObject)
        {
            if (targetGameObject)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(targetGameObject);
                }
                else
#endif
                {
                    Destroy(targetGameObject);
                }
            }
        }

        private void DestroyActionItem()
        {
            if (_currentItem) SafeDestroy(_currentItem.gameObject);
        }

        private void MoveItemToWidgetSlot(Transform slot)
        {
            if (_currentItem)
            {
                _currentItem.transform.SetParent(slot, false);
                _currentItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        // Called by animator event
        public void DamageStart() { }

        // Called by animator event
        public void DamageStop() { }

        // Called by animator event
        public void StageEnding() { }

        // Called by animator event
        public void FootL() { }

        // Called by animator event
        public void FootR() { }

        // Called by animator event
        public void Hit() { }

        public void UpdateActiveItem(ItemStack itemStack, ItemActionType actionType, PlayerSkillItemConfig psi = null)
        {
            if (actionType == ItemActionType.None)
            {
                DestroyActionItem();
            }
            else
            {
                if (psi || (_player && (psi = _player.SkillItemConfig)))
                {
                    CreateActionItem(itemStack, actionType, psi);
                }
                else
                {
                    Debug.LogWarning("Player skill item config is neither passed in nor present in player controller!");
                }

                // Mount weapon on start
                MoveItemToWidgetSlot(_itemMountSlot!);
            }
        }

        public void UpdateActionItemState(PlayerController.CurrentItemState state)
        {
            switch (state)
            {
                case PlayerController.CurrentItemState.HoldInMainHand:
                    MoveItemToWidgetSlot(_mainHandSlot!);
                    break;
                case PlayerController.CurrentItemState.HoldInOffhand:
                    MoveItemToWidgetSlot(_offHandSlot!);
                    break;
                case PlayerController.CurrentItemState.Mount:
                    MoveItemToWidgetSlot(_itemMountSlot!);
                    break;
            }
        }

        private void StartItemAction()
        {
            if (_currentItem)
            {
                _currentItem.StartAction();
            }
        }

        private void EndItemAction()
        {
            if (_currentItem)
            {
                _currentItem.EndAction();
            }
        }

        /// <summary>
        /// Used by skill editor to clean up temporary slots attached to chara preview.
        /// SetRefTransforms() needs to be called again before using these slots.
        /// </summary>
        public void CleanUpSlots()
        {
            if (_mainHandSlot)   SafeDestroy(_mainHandSlot.gameObject);
            if (_offHandSlot)    SafeDestroy(_offHandSlot.gameObject);
            if (_itemMountPivot) SafeDestroy(_itemMountPivot.gameObject);
            if (_itemMountSlot)  SafeDestroy(_itemMountSlot.gameObject);
        }

        public void Initialize()
        {
            _player = GetComponentInParent<PlayerController>();
            _playerAnimator = GetComponent<Animator>();

            // Subscribe player controller events
            if (_player)
            {
                _player.OnItemStateChanged += UpdateActionItemState;
                _player.OnCurrentItemChanged += UpdateActiveItem;
                _player.OnMeleeDamageStart += StartItemAction;
                _player.OnMeleeDamageEnd += EndItemAction;
            }
        }

        public void Unload()
        {
            // Unsubscribe player controller events
            if (_player)
            {
                _player.OnItemStateChanged -= UpdateActionItemState;
                _player.OnCurrentItemChanged -= UpdateActiveItem;
                _player.OnMeleeDamageStart -= StartItemAction;
                _player.OnMeleeDamageEnd -= EndItemAction;
            }
        }

        public void ManagedUpdate()
        {
            if (!_spineRef || !_itemMountPivot)
                return;

            _itemMountPivot.localEulerAngles = new Vector3(-_spineRef.localEulerAngles.x, 0F, 0F);
        }

        private void OnAnimatorMove()
        {
            if (_player && _player.UseRootMotion)
            {
                if (_player.IgnoreAnimatorScale)
                {
                    _player.RootMotionPositionDelta += _playerAnimator!.deltaPosition / transform.localScale.x;
                }
                else
                {
                    // Only uniform scale is supported
                    _player.RootMotionPositionDelta += _playerAnimator!.deltaPosition;
                }
                
                _player.RootMotionRotationDelta = _playerAnimator.deltaRotation * _player.RootMotionRotationDelta;
            }
        }
    }
}