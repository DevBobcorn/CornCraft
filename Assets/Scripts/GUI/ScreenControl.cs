#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftSharp.UI
{
    public class ScreenControl : MonoBehaviour
    {
        [SerializeField] private BaseCornClient? client;
        private readonly Stack<BaseScreen> screenStack = new();

        public bool IsTopScreen(BaseScreen screen)
        {
            return GetTopScreen() == screen;
        }

        public BaseScreen? GetTopScreen()
        {
            if (screenStack.Count <= 0)
            {
                //Debug.LogError("Trying to peek an already empty screen stack!");
                return null;
            }

            return screenStack.Peek();
        }

        public void PushScreen(BaseScreen screen)
        {
            if (screen != null)
            {
                screen.EnsureInitialized();

                // Deactive previous top screen if present
                if (screenStack.Count > 0)
                    screenStack.Peek().IsActive = false;
                
                // Push and activate new top screen
                screenStack.Push(screen);
                screen.IsActive = true;

                // Move this screen to the top
                screen.transform.SetAsLastSibling();

                UpdateScreenStates();
            }
        }

        public void TryPopScreen()
        {
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
            bool shouldPauseInput = false;
            foreach (var w in screenStack)
                shouldPauseInput = shouldPauseInput || w.ShouldPause();
            
            //Debug.Log($"In window stack: {string.Join(' ', screenStack)}");

            // Update States
            if (client!.InputPaused != shouldPauseInput)
            {
                if (shouldPauseInput)
                {
                    client.EnableInput(false);
                }
                else
                {
                    client.EnableInput(true);
                }
            }

            Cursor.lockState = releaseCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }

        void Update()
        {
            var topScreen = GetTopScreen();
            if (topScreen != null)
            {
                topScreen.UpdateScreen();
            }
        }
    }
}
