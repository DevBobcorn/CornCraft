using UnityEngine;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class DestroyAfterAnimation : MonoBehaviour
    {
        public static readonly int EXPIRED = Animator.StringToHash("Expired");
        
        // Called by animator after hide animation ends...
        private void Expire()
        {
            Destroy(gameObject);
        }
    }   
}
