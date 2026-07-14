using System;
using ChieChie.Constracts;
using ChieChie.MVP;
using UnityEngine;

namespace ChieChie.Resource
{
    /// <summary>
    /// Presents one resource on one view. Each view owns and disposes its presenter
    /// through BaseView rather than registering itself with the model.
    /// </summary>
    public sealed class ResourcePresenter : BasePresenter<IResourceView, ResourceModel>
    {
        private readonly ResourceUpdateQueue _updateQueue = new();
        private readonly string _resourceKey;

        private long _currentVisualAmount;
        private bool _isInfiniteUpdateDelayed;
        private bool _isCurrentlyShowingInfinite;

        public string ResourceKey => _resourceKey;
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;

        public ResourcePresenter(IResourceView view, ResourceModel model)
            : base(view, model)
        {
            _resourceKey = view.ResourceKey;
        }

        protected override void OnInitialize()
        {
            Model.OnResourceChanged += HandleResourceChanged;
            Model.OnResourceInsufficient += HandleResourceInsufficient;
            Model.OnResourceMaxStackReached += HandleMaxStackReached;
            Model.OnInfiniteAdded += HandleInfiniteAdded;
            Model.OnInfiniteExpired += HandleInfiniteExpired;
            Model.OnRefreshRequested += HandleRefreshRequested;
            Model.OnPendingUpdateRequested += HandlePendingUpdateRequested;
            Model.OnRegenStatusChanged += HandleRegenStatusChanged;

            _currentVisualAmount = Model.GetCurrentAmount(ResourceKey);
            RefreshView();
        }

        protected override void OnDispose()
        {
            Model.OnResourceChanged -= HandleResourceChanged;
            Model.OnResourceInsufficient -= HandleResourceInsufficient;
            Model.OnResourceMaxStackReached -= HandleMaxStackReached;
            Model.OnInfiniteAdded -= HandleInfiniteAdded;
            Model.OnInfiniteExpired -= HandleInfiniteExpired;
            Model.OnRefreshRequested -= HandleRefreshRequested;
            Model.OnPendingUpdateRequested -= HandlePendingUpdateRequested;
            Model.OnRegenStatusChanged -= HandleRegenStatusChanged;
            _updateQueue.Clear();
        }

        public void ProcessPendingUpdates(long amountIncrement = 0)
        {
            _isInfiniteUpdateDelayed = false;
            var queuedUpdate = _updateQueue.ProcessNextUpdate(ResourceKey);
            long modelAmount = Model.GetCurrentAmount(ResourceKey);
            bool isInfinite = Model.IsCurrentlyInfinite(ResourceKey);

            if (isInfinite)
            {
                _currentVisualAmount = modelAmount;
            }
            else if (amountIncrement > 0)
            {
                _currentVisualAmount = AddWithoutOverflow(_currentVisualAmount, amountIncrement);
            }
            else
            {
                _currentVisualAmount = queuedUpdate?.Amount ?? modelAmount;
            }

            if (_currentVisualAmount > modelAmount)
            {
                _currentVisualAmount = modelAmount;
            }

            PresentState(_currentVisualAmount, isInfinite, true);
        }

        public void ForceUpdateView()
        {
            RefreshView();
        }

        private void HandleResourceChanged(ResourceChangeData<long> changeData)
        {
            if (changeData.ResourceId != ResourceKey) return;

            if (changeData.DelayUpdate)
            {
                _updateQueue.EnqueueUpdate(changeData.OldAmount, changeData.NewAmount);
                RefreshRegenStatus();
                return;
            }

            _updateQueue.Clear();
            _currentVisualAmount = changeData.NewAmount;

            if (!Model.IsCurrentlyInfinite(ResourceKey))
            {
                View.SetResourceAmountWithoutAnimation(changeData.NewAmount);
            }

            RefreshView();
        }

        private void HandleResourceInsufficient(string resourceKey)
        {
            if (resourceKey == ResourceKey)
            {
                View.ShowInsufficientMessage();
            }
        }

        private void HandleMaxStackReached(string resourceKey)
        {
            if (resourceKey == ResourceKey)
            {
                View.OnMaxStackReached(resourceKey);
            }
        }

        private void HandleInfiniteAdded(string resourceKey, bool delayUpdate)
        {
            if (resourceKey != ResourceKey) return;

            if (delayUpdate)
            {
                long currentAmount = Model.GetCurrentAmount(ResourceKey);
                _isInfiniteUpdateDelayed = true;
                _updateQueue.EnqueueUpdate(
                    _isCurrentlyShowingInfinite ? currentAmount : 0,
                    currentAmount);
                RefreshRegenStatus();
                return;
            }

            _isInfiniteUpdateDelayed = false;
            RefreshView();
        }

        private void HandleInfiniteExpired(string resourceKey)
        {
            if (resourceKey != ResourceKey) return;

            _isInfiniteUpdateDelayed = false;
            RefreshView();
        }

        private void HandleRefreshRequested()
        {
            RefreshView();
        }

        private void HandlePendingUpdateRequested(string resourceKey, long amountIncrement)
        {
            if (resourceKey == ResourceKey)
            {
                ProcessPendingUpdates(amountIncrement);
            }
        }

        private void HandleRegenStatusChanged(string resourceKey)
        {
            if (resourceKey == ResourceKey)
            {
                RefreshRegenStatus();
            }
        }

        private void RefreshView()
        {
            _currentVisualAmount = Model.GetCurrentAmount(ResourceKey);
            bool isInfinite = Model.IsCurrentlyInfinite(ResourceKey) && !_isInfiniteUpdateDelayed;
            PresentState(_currentVisualAmount, isInfinite, false);
        }

        private void PresentState(long displayAmount, bool isInfinite, bool processAnimation)
        {
            ResourceData resourceData = Model.GetResourceData(ResourceKey);
            if (resourceData == null) return;

            _isCurrentlyShowingInfinite = isInfinite;
            View.SetResourceIcon(GetIcon(resourceData, isInfinite));
            View.SetResourceName(resourceData.DisplayName);

            if (!isInfinite || !processAnimation)
            {
                View.SetResourceAmount(displayAmount);
            }

            DateTime expirationTime = isInfinite
                ? DateTime.UtcNow.Add(Model.GetRemainingInfiniteTime(ResourceKey))
                : DateTime.MinValue;
            View.UpdateInfinityStatus(isInfinite, expirationTime);
            RefreshRegenStatus();

        }

        private void RefreshRegenStatus()
        {
            bool isInfinite = Model.IsCurrentlyInfinite(ResourceKey) && !_isInfiniteUpdateDelayed;
            if (isInfinite)
            {
                View.UpdateRegenStatus(false, false, DateTime.MinValue);
                return;
            }

            View.UpdateRegenStatus(
                Model.IsRegenEnabled(ResourceKey),
                Model.IsAtMaxStack(ResourceKey),
                Model.GetNextRegenTime(ResourceKey));
        }

        private static Sprite GetIcon(ResourceData resourceData, bool isInfinite)
        {
            return isInfinite && resourceData.InfinityIcon != null
                ? resourceData.InfinityIcon
                : resourceData.Icon;
        }

        private static long AddWithoutOverflow(long currentAmount, long increment)
        {
            try
            {
                return checked(currentAmount + increment);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }
    }
}
