using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MinecraftClient.UI
{
    public class ScreenControl : MonoBehaviour
    {
        private bool isPaused;
        public bool IsPaused { get { return isPaused; } }

        private CornClient game;

        private void PauseGame()
        {
            isPaused = true;
        }

        private void ResumeGame()
        {
            isPaused = false;
        }

        private Stack<BaseScreen> screenStack = new Stack<BaseScreen>();
        private float screenChangeCooldown = 0F;

        private TMP_Text DebugText;

        public bool IsTopScreen(BaseScreen screen)
        {
            return GetTopScreen() == screen;
        }

        public BaseScreen GetTopScreen()
        {
            if (screenStack.Count <= 0)
                Debug.LogError("Trying to peek an already empty screen stack!");

            return screenStack.Peek();
        }

        public void PushScreen(BaseScreen screen)
        {
            if (screenChangeCooldown > 0F)
                return;

            // Deactive previous top screen if present
            if (screenStack.Count > 0)
                screenStack.Peek().IsActive = false;
            
            // Push and activate new top screen
            screenStack.Push(screen);
            screen.IsActive = true;

            // Move this screen to the top
            screen.transform.SetAsLastSibling();

            UpdateScreenStates();

            screenChangeCooldown = 0.1F;

        }

        public void TryPopScreen()
        {
            if (screenChangeCooldown > 0F)
                return;
            
            if (screenStack.Count <= 0)
                Debug.LogError("Trying to pop an already empty screen stack!");

            // Deactive and pop previous top screen
            BaseScreen screen2Pop = screenStack.Peek();

            screen2Pop.IsActive = false;
            screenStack.Pop();

            // Push and activate new top screen
            if (screenStack.Count > 0)
                screenStack.Peek().IsActive = true;

            UpdateScreenStates();

            screenChangeCooldown = 0.1F;

        }

        // Called before exiting the main scene
        public void ClearScreens()
        {
            screenStack.Clear();
        }

        private void UpdateScreenStates()
        {
            // Get States
            bool releaseCursor = (screenStack.Count > 0) ? screenStack.Peek().ReleaseCursor() : false;
            bool shouldPauseGame = false;
            foreach (var w in screenStack)
            {
                //Debug.Log("In window stack: " + w.ScreenName());
                shouldPauseGame = shouldPauseGame || w.ShouldPause();
            }
            // Update States
            if (isPaused != shouldPauseGame)
            {
                if (shouldPauseGame)
                    PauseGame();
                else
                    ResumeGame();
            }

            Cursor.lockState = releaseCursor ? CursorLockMode.None : CursorLockMode.Locked;

        }

        void Start()
        {
            // Initialize controls
            DebugText = GameObject.Find("Debug Text").GetComponent<TMP_Text>();
            DebugText.text = "Initializing...";

            if (CornClient.Instance is null)
            {
                Debug.LogWarning("No Minecraft Client connected");
            }
            else
            {
                game = CornClient.Instance;
            }

        }

        void Update()
        {
            screenChangeCooldown -= Time.deltaTime;

            if (game is null || !CornClient.Connected) return;

            DebugText.text = $"FPS: {((int)(1F / Time.deltaTime)).ToString().PadLeft(4, ' ')}\n{game.GetPlayerController()?.GetDebugInfo()}\n{game.GetWorldRender()?.GetDebugInfo()}";

        }

    }

}
