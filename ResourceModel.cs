using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.MVP;
using Cysharp.Threading.Tasks;

namespace ChieChie.Resource
{
    /// <summary>
    /// Owns all resource state and business rules. UI binding belongs to ResourcePresenter;
    /// this model only publishes state changes and presentation requests.
    /// </summary>
    public sealed class ResourceModel : IModel, IDisposable
    {
        private readonly ResourceConfig _resourceConfig;
        private readonly IResourceSaveAdapter _saveAdapter;
        private readonly Dictionary<string, long> _resourceAmounts = new();

        private InfiniteResourceModel _infiniteModel;
        private ResourceRegenController _resourceRegenController;

        public bool IsInitialized { get; private set; }

        public event Action<ResourceChangeData<long>> OnResourceChanged;
        public event Action<ResourceChangeData<long>> OnResourceSpent;
        public event Action<ResourceChangeData<long>> OnResourceAdded;
        public event Action<string> OnResourceMaxStackReached;
        public event Action<string> OnResourceInsufficient;
        public event Action<string> OnInfiniteExpired;
        public event Action<string, bool> OnInfiniteAdded;

        internal event Action OnRefreshRequested;
        internal event Action<string, long> OnPendingUpdateRequested;
        internal event Action<string> OnRegenStatusChanged;

        public ResourceModel(ResourceConfig resourceConfig, IResourceSaveAdapter saveAdapter)
        {
            _resourceConfig = resourceConfig ?? throw new ArgumentNullException(nameof(resourceConfig));
            _saveAdapter = saveAdapter ?? throw new ArgumentNullException(nameof(saveAdapter));
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            InitializeAmounts();
            _infiniteModel = new InfiniteResourceModel(_saveAdapter);
            _infiniteModel.OnInfiniteDurationAdded += HandleInfiniteDurationAdded;
            _infiniteModel.OnInfiniteDurationExpired += HandleInfiniteDurationExpired;
            _infiniteModel.Initialize(_resourceConfig);
            _resourceRegenController = new ResourceRegenController();
            _resourceRegenController.Initialize(this, _resourceConfig, _saveAdapter);

            IsInitialized = true;
            OnRefreshRequested?.Invoke();
        }

        private void InitializeAmounts()
        {
            bool isFirstInitialization = _saveAdapter.IsFirstInit();

            foreach (var resourceData in _resourceConfig.GetAllResources())
            {
                if (resourceData == null || string.IsNullOrEmpty(resourceData.ResourceId)) continue;

                _saveAdapter.RegisterResource(resourceData);
                long fallbackAmount = resourceData.HasRegen
                    ? resourceData.MaxStack
                    : resourceData.DefaultAmount;
                long amount = isFirstInitialization
                    ? fallbackAmount
                    : _saveAdapter.LoadAmount(resourceData, fallbackAmount);

                _resourceAmounts[resourceData.ResourceId] = amount;

                if (isFirstInitialization)
                {
                    _saveAdapter.SaveAmount(resourceData, amount);
                }
            }

            if (isFirstInitialization)
            {
                _saveAdapter.SetFirstInitComplete();
            }
        }

        public void AddResource(string resourceKey, long amount, bool delayUpdate = false)
        {
            if (string.IsNullOrEmpty(resourceKey) || amount < 0) return;

            var resourceData = GetResourceData(resourceKey);
            if (resourceData == null) return;

            long currentAmount = GetCurrentAmount(resourceKey);
            long newAmount;
            try
            {
                newAmount = checked(currentAmount + amount);
            }
            catch (OverflowException)
            {
                newAmount = long.MaxValue;
            }

            long maxStack = resourceData.MaxStack;
            bool reachedMaxStack = maxStack > 0 && currentAmount < maxStack && newAmount >= maxStack;
            if (maxStack > 0 && newAmount > maxStack)
            {
                newAmount = maxStack;
            }

            _resourceAmounts[resourceKey] = newAmount;
            _saveAdapter.SaveAmount(resourceData, newAmount);

            var changeData = new ResourceChangeData<long>(resourceKey, currentAmount, newAmount, delayUpdate);
            OnResourceChanged?.Invoke(changeData);
            OnResourceAdded?.Invoke(changeData);
            if (reachedMaxStack)
            {
                OnResourceMaxStackReached?.Invoke(resourceKey);
            }
        }

        public bool SpendResource(string resourceKey, long amount)
        {
            if (string.IsNullOrEmpty(resourceKey) || amount < 0) return false;

            var resourceData = GetResourceData(resourceKey);
            if (resourceData == null) return false;

            long currentAmount = GetCurrentAmount(resourceKey);
            if (IsCurrentlyInfinite(resourceKey))
            {
                OnResourceSpent?.Invoke(new ResourceChangeData<long>(resourceKey, currentAmount, currentAmount));
                return true;
            }

            if (currentAmount < amount)
            {
                OnResourceInsufficient?.Invoke(resourceKey);
                return false;
            }

            long newAmount = currentAmount - amount;
            _resourceAmounts[resourceKey] = newAmount;
            _saveAdapter.SaveAmount(resourceData, newAmount);

            var changeData = new ResourceChangeData<long>(resourceKey, currentAmount, newAmount);
            OnResourceChanged?.Invoke(changeData);
            OnResourceSpent?.Invoke(changeData);
            return true;
        }

        public long GetCurrentAmount(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey)) return 0;
            return _resourceAmounts.TryGetValue(resourceKey, out long amount) ? amount : 0;
        }

        public bool IsAtMaxStack(string resourceKey)
        {
            long maxStack = GetMaxStack(resourceKey);
            return maxStack > 0 && GetCurrentAmount(resourceKey) >= maxStack;
        }

        public long GetMaxStack(string resourceKey)
        {
            return GetResourceData(resourceKey)?.MaxStack ?? 0;
        }

        public void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false)
        {
            var resourceData = GetResourceData(resourceKey);
            if (resourceData == null) return;

            resourceData.MaxStack = newMaxStack;
            long currentAmount = GetCurrentAmount(resourceKey);

            if (newMaxStack > 0 && currentAmount > newMaxStack)
            {
                SetAmount(resourceData, currentAmount, newMaxStack);
            }
            else if (fillFull && currentAmount < newMaxStack)
            {
                AddResource(resourceKey, newMaxStack - currentAmount);
            }
        }

        public void ProcessPendingUpdate(string resourceKey, long amountIncrement = 0)
        {
            if (string.IsNullOrEmpty(resourceKey)) return;
            OnPendingUpdateRequested?.Invoke(resourceKey, amountIncrement);
        }

        public void ForceUpdateAllView()
        {
            OnRefreshRequested?.Invoke();
        }

        public void AddInfiniteDuration(string resourceKey, TimeSpan duration, bool delayUpdate = false)
        {
            if (!IsInitialized || _infiniteModel == null) return;

            _infiniteModel.AddDuration(resourceKey, duration, delayUpdate);
            if (!delayUpdate)
            {
                OnRefreshRequested?.Invoke();
            }
        }

        public bool IsCurrentlyInfinite(string resourceKey)
        {
            return IsInitialized && _infiniteModel != null && _infiniteModel.IsInfinite(resourceKey);
        }

        public TimeSpan GetRemainingInfiniteTime(string resourceKey)
        {
            return IsInitialized && _infiniteModel != null
                ? _infiniteModel.GetRemainingTime(resourceKey)
                : TimeSpan.Zero;
        }

        public bool IsRegenEnabled(string resourceKey)
        {
            return _resourceRegenController != null && _resourceRegenController.IsRegenEnabled(resourceKey);
        }

        public DateTime GetNextRegenTime(string resourceKey)
        {
            return _resourceRegenController != null
                ? _resourceRegenController.GetNextRegenTime(resourceKey)
                : DateTime.UtcNow;
        }

        public void SetRegenStatus(string resourceKey, bool isEnabled)
        {
            if (_resourceRegenController == null) return;

            _resourceRegenController.SetRegenStatus(resourceKey, isEnabled);
            OnRegenStatusChanged?.Invoke(resourceKey);
        }

        public ResourceData GetResourceData(string resourceKey)
        {
            return string.IsNullOrEmpty(resourceKey) ? null : _resourceConfig.GetResourceData(resourceKey);
        }

        private void SetAmount(ResourceData resourceData, long oldAmount, long newAmount)
        {
            _resourceAmounts[resourceData.ResourceId] = newAmount;
            _saveAdapter.SaveAmount(resourceData, newAmount);
            OnResourceChanged?.Invoke(
                new ResourceChangeData<long>(resourceData.ResourceId, oldAmount, newAmount));
        }

        private void HandleInfiniteDurationAdded(string resourceKey, bool delayUpdate)
        {
            OnInfiniteAdded?.Invoke(resourceKey, delayUpdate);
        }

        private void HandleInfiniteDurationExpired(string resourceKey)
        {
            OnInfiniteExpired?.Invoke(resourceKey);
        }

        public void OnAppQuit()
        {
            _resourceRegenController?.SaveAllRegenTimes();
        }

        public void OnAppPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _resourceRegenController?.SaveAllRegenTimes();
            }
        }

        public void Dispose()
        {
            if (_infiniteModel != null)
            {
                _infiniteModel.OnInfiniteDurationAdded -= HandleInfiniteDurationAdded;
                _infiniteModel.OnInfiniteDurationExpired -= HandleInfiniteDurationExpired;
                _infiniteModel.Cleanup();
                _infiniteModel = null;
            }

            _resourceRegenController?.Dispose();
            _resourceRegenController = null;
            _resourceAmounts.Clear();
            IsInitialized = false;

            OnResourceChanged = null;
            OnResourceSpent = null;
            OnResourceAdded = null;
            OnResourceMaxStackReached = null;
            OnResourceInsufficient = null;
            OnInfiniteExpired = null;
            OnInfiniteAdded = null;
            OnRefreshRequested = null;
            OnPendingUpdateRequested = null;
            OnRegenStatusChanged = null;
        }
    }
}
