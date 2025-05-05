using UnityEngine;
using UnityEngine.UI;

namespace CraftSharp.UI
{
    [RequireComponent(typeof (Animator))]
    public class InteractionProgressOption : InteractionOption
    {
        private static readonly int FILL_AMOUNT  = Shader.PropertyToID("_FillAmount");
        private static readonly int DELTA_AMOUNT = Shader.PropertyToID("_DeltaAmount");
        
        [SerializeField] private Image barImage;
        private Material barMaterial;
        
        private void Start()
        {
            // Create a material instance for each bar
            barMaterial = new Material(barImage.material);
            barImage.material = barMaterial;
            
            UpdateProgress(0F);
        }

        public void UpdateProgress(float progress)
        {
            barMaterial.SetFloat(FILL_AMOUNT, progress);
            barMaterial.SetFloat(DELTA_AMOUNT, progress);
        }
    }
}