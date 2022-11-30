// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth
{
    public class ModelController : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> characterList = new List<GameObject>();

        [SerializeField]
        private float slowTime = 0.1f;

        private bool slow;

        void Start()
        {
        }

        void Update()
        {
        }

        private void AnimatorAction(System.Action<Animator> act)
        {
            foreach (var chara in characterList)
            {
                if (chara && chara.activeInHierarchy)
                {
                    var animator = chara.GetComponent<Animator>();
                    if (animator)
                    {
                        act(animator);
                    }
                }
            }
        }

        private void ClothAction(System.Action<BaseCloth> act)
        {
            foreach (var chara in characterList)
            {
                if (chara && chara.activeInHierarchy)
                {
                    var clothList = chara.GetComponentsInChildren<BaseCloth>(true);
                    if (clothList != null)
                    {
                        foreach (var cloth in clothList)
                        {
                            act(cloth);
                        }
                    }
                }
            }
        }

        public void OnNextButton()
        {
            AnimatorAction((ani) => ani.SetTrigger("Next"));
        }

        public void OnBackButton()
        {
            AnimatorAction((ani) => ani.SetTrigger("Back"));
        }

        public void OnSlowButton()
        {
            slow = !slow;

            float timeScale = slow ? slowTime : 1.0f;

            AnimatorAction((ani) => ani.speed = timeScale);
            ClothAction((cloth) => cloth.SetTimeScale(timeScale));
        }

        public void OnActiveButton()
        {
            ClothAction((cloth) => cloth.gameObject.SetActive(!cloth.gameObject.activeSelf));
        }
    }
}
