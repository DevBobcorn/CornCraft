using UnityEngine;

namespace Krivodeling.UI.Effects.Examples
{
    public class UIBlurWithCanvasGroup : MonoBehaviour
    {
        private UIBlur _uiBlur;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            SetComponents();

            _uiBlur.OnBeginBlur.AddListener(OnBeginBlur);
            _uiBlur.OnBlurChanged.AddListener(OnBlurChanged);
            _uiBlur.OnEndBlur.AddListener(OnEndBlur);
        }

        private void SetComponents()
        {
            _uiBlur = GetComponent<UIBlur>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnBeginBlur()
        {
            _canvasGroup.blocksRaycasts = true;
        }

        private void OnBlurChanged(float value)
        {
            _canvasGroup.alpha = value;
        }

        private void OnEndBlur()
        {
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
