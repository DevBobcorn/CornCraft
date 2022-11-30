// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;
using UnityEngine.UI;

namespace MagicaCloth
{
    public class UIPartsItem : MonoBehaviour
    {
        public Text text;
        public Button prefButton;
        public Button nextButton;

        private int id;

        void Start()
        {
        }

        public void Init(string title, int id, System.Action<int, int> onClick)
        {
            text.text = title;
            this.id = id;

            prefButton.onClick.AddListener(() =>
            {
                onClick(this.id, -1);
            });
            nextButton.onClick.AddListener(() =>
            {
                onClick(this.id, 1);
            });
        }
    }
}
