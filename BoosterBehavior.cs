using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Booster
{
    public abstract class BoosterBehavior : MonoBehaviour
    {
        private bool _isSelected;
        private bool _isBusy;
        private bool _isDirty = true;
        private BoosterSetting settings;
        
        // Thêm flag theo dõi lượt dùng Free khi có Infinite
        private bool _hasUsedInfiniteFreePass;

        public BoosterSetting Settings => settings;
        public bool IsSelected => _isSelected;
        public bool HasUsedInfiniteFreePass => _hasUsedInfiniteFreePass;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                _isDirty = true;
            }
        }

        public bool IsDirty => _isDirty;

        public void InitialiseSettings(BoosterSetting settings)
        {
            this.settings = settings;
        }

        public abstract UniTask InitialiseAsync(CancellationToken cancellationToken = default);

        public async UniTask<bool> ActivateAsync(CancellationToken cancellationToken = default)
        {
            if (IsBusy || !BoosterCondition()) 
            {
                return false; 
            }

            IsBusy = true;
            try
            {
                return await OnActivateAsync(cancellationToken);
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected abstract UniTask<bool> OnActivateAsync(CancellationToken cancellationToken);

        public virtual string GetFloatingMessage()
        {
            return settings.FloatingMessage;
        }

        public virtual UniTask ResetBehaviorAsync(CancellationToken cancellationToken = default)
        {
            _hasUsedInfiniteFreePass = false;
            return UniTask.CompletedTask;
        }

        public void SetDirty()
        {
            _isDirty = true;
        }

        public abstract bool BoosterCondition();

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
        }
       
        public void ConsumeInfiniteFreePass()
        {
            _hasUsedInfiniteFreePass = true;
        }
    }
}