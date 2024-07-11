#nullable enable
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using CraftSharp.Control;
using CraftSharp.Resource;

namespace CraftSharp.Rendering
{
    public class PlayerRenderWidget : MonoBehaviour
    {
        [SerializeField] private Transform? _mainHandRef;
        [SerializeField] private Transform? _offHandRef;
        [SerializeField] private Transform? _spineRef;

        private Transform? _mainHandSlot; // A slot fixed to mainHandRef transform (as a child)
        private Transform? _offHandSlot; // A slot fixed to offHandRef transform (as a child)
        private Transform? _itemMountPivot, _itemMountSlot;
        private PlayerController? _player;
        private PlayerActionItem? _currentItem;
        private Animator? _playerAnimator;

        public void SetRefTransforms(Transform mainHandRef, Transform offHandRef, Transform spineRef)
        {
            this._mainHandRef = mainHandRef;
            // Create main hand slot transform
            var mainHandSlotObj = new GameObject("Main Hand Slot");
            _mainHandSlot = mainHandSlotObj.transform;
            _mainHandSlot.SetParent(mainHandRef);
            // Initialize position and rotation
            _mainHandSlot.localPosition = Vector3.zero;
            _mainHandSlot.localEulerAngles = Vector3.zero;

            this._offHandRef = offHandRef;
            // Create off hand slot transform
            var offHandSlotObj = new GameObject("Off Hand Slot");
            _offHandSlot = offHandSlotObj.transform;
            _offHandSlot.SetParent(offHandRef);
            // Initialize position and rotation
            _offHandSlot.localPosition = Vector3.zero;
            _offHandSlot.localEulerAngles = Vector3.zero;

            this._spineRef = spineRef;

            // Create weapon mount slot transform
            var itemMountPivotObj = new GameObject("Item Mount Pivot");
            _itemMountPivot = itemMountPivotObj.transform;
            _itemMountPivot.SetParent(transform);

            var itemMountSlotObj = new GameObject("Item Mount Slot");
            _itemMountSlot = itemMountSlotObj.transform;
            _itemMountSlot.SetParent(_itemMountPivot);
        }

        private void CreateActionItem(ItemStack? itemStack, ItemActionType actionType, PlayerSkillItemConfig psi)
        {
            DestroyActionItem();

            var itemObj = new GameObject($"Action Item ({itemStack?.DisplayName})")
            {
                layer = gameObject.layer
            };

            (Mesh mesh, Material material, Dictionary<DisplayPosition, float3x3> transforms)? meshData = null;

            switch (actionType)
            {
                case ItemActionType.MeleeWeaponSword:
                    _currentItem = itemObj!.AddComponent<MeleeWeapon>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummySwordItemMesh!, psi.DummyItemMaterial!, new());

                    _currentItem.slotEularAngles = new(135F, 90F, -20F);
                    _currentItem.slotPosition = new(0F, 0.2F, -0.25F);

                    _mainHandSlot!.localPosition = new(0F, -0.1F, 0.05F);
                    _mainHandSlot.localEulerAngles = new(-135F, 0F, 45F);

                    var trailObj = GameObject.Instantiate(psi.SwordTrailPrefab);
                    trailObj.transform.parent = itemObj.transform;
                    trailObj.transform.localPosition = new(0.5F, 0.65F, 0.65F);

                    var sword = _currentItem as MeleeWeapon;
                    sword!.SlashTrail = trailObj.GetComponent<TrailRenderer>();

                    itemObj.transform.localScale = new(0.5F, 0.5F, 0.5F);
                    break;
                case ItemActionType.RangedWeaponBow:
                    _currentItem = itemObj!.AddComponent<UselessActionItem>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, false);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummyBowItemMesh!, psi.DummyItemMaterial!, new());

                    _currentItem.slotEularAngles = new(-40F, 90F, 20F);
                    _currentItem.slotPosition = new(0F, -0.4F, -0.4F);
                    
                    _offHandSlot!.localPosition = new(-0.21F, 0.12F, 0.38F);
                    _offHandSlot.localEulerAngles = new(5F, -150F, 115F);

                    itemObj.transform.localScale = new(0.6F, 0.6F, 0.6F);
                    break;
                default:
                    _currentItem = itemObj!.AddComponent<UselessActionItem>();
                    meshData = ItemMeshBuilder.BuildItem(itemStack, true);
                    // Use dummy material and mesh if failed to build for item
                    meshData ??= (psi.DummySwordItemMesh!, psi.DummyItemMaterial!, new());

                    _currentItem.slotEularAngles = new(0F, 90F, 0F);
                    _currentItem.slotPosition = new(0F, 0F, -0.5F);

                    itemObj.transform.localScale = new(0.5F, 0.5F, 0.5F);
                    break;
            }

            if (meshData is not null) // In case of invalid items, meshData can be null
            {
                var mesh = itemObj.AddComponent<MeshFilter>();
                mesh.mesh = meshData.Value.mesh;

                var renderer = itemObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = meshData.Value.material;
            }

            // Set weapon slot position and rotation
            _itemMountPivot!.localPosition = new(0F, 1.3F, 0F);

            _itemMountSlot!.localPosition = _currentItem.slotPosition;
            _itemMountSlot!.localEulerAngles = _currentItem.slotEularAngles;

            // Mount weapon on start
            MoveItemToWidgetSlot(_itemMountSlot!);
        }

        /// <summary>
        /// Properly destroy a gameobject, in either editor mode, play mode, or an actual build.
        /// See https://forum.unity.com/threads/editor-and-destroyimmediate.1261745/
        /// </summary>
        /// <param name="gameObject"></param>
        private void SafeDestroy(GameObject? gameObject)
        {
            if (gameObject != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(gameObject);
                }
                else
#endif
                {
                    Destroy(gameObject);
                }
            }
        }

        private void DestroyActionItem()
        {
            if (_currentItem != null) SafeDestroy(_currentItem.gameObject);
        }

        private void MoveItemToWidgetSlot(Transform slot)
        {
            if (_currentItem != null)
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

        public void FootR() { }

        public void Hit() { }

        public void UpdateActiveItem(ItemStack? itemStack, ItemActionType actionType, PlayerSkillItemConfig? psi = null)
        {
            if (actionType == ItemActionType.None)
            {
                DestroyActionItem();
            }
            else
            {
                if ((psi != null) || (_player != null && (psi = _player.SkillItemConf) != null))
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
            if (_currentItem != null)
            {
                _currentItem.StartAction();
            }
        }

        private void EndItemAction()
        {
            if (_currentItem != null)
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
            if (_mainHandSlot != null)   SafeDestroy(_mainHandSlot.gameObject);
            if (_offHandSlot != null)    SafeDestroy(_offHandSlot.gameObject);
            if (_itemMountPivot != null) SafeDestroy(_itemMountPivot.gameObject);
            if (_itemMountSlot != null)  SafeDestroy(_itemMountSlot.gameObject);
        }

        public void Initialize()
        {
            _player = GetComponentInParent<PlayerController>();
            _playerAnimator = GetComponent<Animator>();

            // Subscribe player controller events
            if (_player != null)
            {
                _player.OnItemStateChanged += this.UpdateActionItemState;
                _player.OnCurrentItemChanged += this.UpdateActiveItem;
                _player.OnMeleeDamageStart += this.StartItemAction;
                _player.OnMeleeDamageEnd += this.EndItemAction;
            }
        }

        public void Unload()
        {
            // Unsubscribe player controller events
            if (_player != null)
            {
                _player.OnItemStateChanged -= this.UpdateActionItemState;
                _player.OnCurrentItemChanged -= this.UpdateActiveItem;
                _player.OnMeleeDamageStart -= this.StartItemAction;
                _player.OnMeleeDamageEnd -= this.EndItemAction;
            }
        }

        void Update()
        {
            if (_spineRef == null || _itemMountPivot == null)
                return;

            _itemMountPivot.localEulerAngles = new Vector3(-_spineRef.localEulerAngles.x, 0F, 0F);
        }

        void OnAnimatorMove()
        {
            if (_player != null && _player.UseRootMotion)
            {
                _player.RootMotionPositionDelta += _playerAnimator!.deltaPosition;
                _player.RootMotionRotationDelta = _playerAnimator.deltaRotation * _player.RootMotionRotationDelta;
            }
        }
    }
}