using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace ChieChie.Core
{
    public class TransitionScreen : MonoBehaviour,IServiceInitialisable,ITransitionScreen
    {
       [Header("References")]
        [SerializeField] private TransitionEffect transitionEffect;
        [SerializeField] private Image transitionImage;
        
        [Header("Config")]
        [Tooltip("Thời gian delay mặc định (giây) trước khi bắt đầu hiệu ứng Transition Out")]
        [SerializeField] private float defaultDelayBeforeOut = 0.5f;

        private Material _materialInstance;
        private TransitionEventManager _eventManager;
        
        [Inject]
        public void Construct(TransitionEventManager eventManager)
        {
            _eventManager = eventManager;
            _eventManager.OnPlayTransitionInAsync += PlayTransitionInAsync;
            _eventManager.OnPlayTransitionOutAsync += PlayTransitionOutAsync;
        }

        public bool IsInitialized { get; set; }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (transitionEffect != null && transitionImage != null && transitionEffect.transitionMaterial != null)
            {
                _materialInstance = new Material(transitionEffect.transitionMaterial);
                transitionImage.material = _materialInstance;
                transitionEffect.SetEffectProperties(_materialInstance);
                transitionImage.gameObject.SetActive(false);
            }
           
            IsInitialized = true;
            return UniTask.FromResult(true);
        }
   
        public async UniTask PlayTransitionOutAsync(float delaySeconds = -1f)
        {
            if (!IsInitialized || transitionEffect == null || transitionImage == null) return;
          
            float actualDelay = delaySeconds >= 0f ? delaySeconds : defaultDelayBeforeOut;

            if (actualDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(actualDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            await transitionEffect.AnimateOut(transitionImage).ToUniTask(this);
            transitionImage.gameObject.SetActive(false);
        }

        public async UniTask PlayTransitionInAsync()
        {
            if (!IsInitialized || transitionEffect == null || transitionImage == null) return;
            transitionImage.gameObject.SetActive(true);
            await transitionEffect.AnimateIn(transitionImage).ToUniTask(this);
            
        }
        private void OnDestroy()
        {
            if (_materialInstance != null) Destroy(_materialInstance);
            if (_eventManager != null)
            {
                _eventManager.OnPlayTransitionInAsync -= PlayTransitionInAsync;
                _eventManager.OnPlayTransitionOutAsync -= PlayTransitionOutAsync;
            }
        }
    }
    
}
