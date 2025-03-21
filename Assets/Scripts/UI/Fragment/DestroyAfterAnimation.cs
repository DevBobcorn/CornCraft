using UnityEngine;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class DestroyAfterAnimation : MonoBehaviour
    {
        // Called by animator after hide animation ends...
        private void Expire()
        {
            Destroy(gameObject);
        }
    }   
}
