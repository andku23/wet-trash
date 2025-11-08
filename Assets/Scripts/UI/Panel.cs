using DG.Tweening;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private const float fadeTime = 0.25f;
    private Tweener _fadeTween;
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void FadeIn(bool immediate = true, float overrideFadeTime = fadeTime)
    {
        if (_fadeTween != null)
        {
            _fadeTween.Kill();
        }
        
        gameObject.SetActive(true);

        if (immediate)
        {
            _canvasGroup.alpha = 1;
        }
        else
        {
            _fadeTween = _canvasGroup.DOFade(1, overrideFadeTime);
        }
    }
    
    public void FadeOut(bool immediate = true, float overrideFadeTime = fadeTime)
    {
        if (_fadeTween != null)
        {
            _fadeTween.Kill();
        }
        
        if (immediate)
        {
            _canvasGroup.alpha = 0;
        }
        else
        {
            _fadeTween = _canvasGroup.DOFade(0, overrideFadeTime);
            _fadeTween.onComplete = OnFadeoutComplete;
        }
    }

    private void OnFadeoutComplete()
    {
        gameObject.SetActive(false);
    }
}
