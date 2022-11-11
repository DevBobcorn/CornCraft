// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// MagicaClothの状態表示モニター
    /// </summary>
    public class ClothMonitorMenu : EditorWindow
    {
        public static ClothMonitorMenu Monitor { get; set; }

        [SerializeField]
        private ClothMonitorUI ui = new ClothMonitorUI();

        //=========================================================================================
        [MenuItem("Tools/Magica Cloth/Cloth Monitor", false)]
        public static void InitWindow()
        {
            GetWindow<ClothMonitorMenu>();
        }

        //=========================================================================================
        public ClothMonitorUI UI
        {
            get
            {
                return ui;
            }
        }

        //=========================================================================================
        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            ui.Enable();
            Monitor = this; // ギズモ表示登録
        }

        private void OnDisable()
        {
            Monitor = null; // ギズモ表示解除
            EditorApplication.update -= OnUpdate;
            ui.Disable();
        }

        private void OnDestroy()
        {
            ui.Destroy();
        }

        private void OnGUI()
        {
            ui.OnGUI();
        }

        void OnUpdate()
        {
            if (EditorApplication.isPlaying == false)
                return;

            if ((Time.frameCount % 30) == 0)
                Repaint();
        }

        //=========================================================================================
        void Init()
        {
            this.titleContent.text = "Cloth Monitor";

            ui.Init(this);
        }
    }
}
