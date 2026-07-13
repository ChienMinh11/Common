using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassManager: IPassService, IDisposable
    {
        private readonly PassDatabase _passDatabase;
        private readonly IEventScheduler _passSchedule;
        private readonly IPassSaveAdapter _passSaveAdapter;
        private PassModel _passModel;
        private PassPresenter _passPresenter;
        public bool IsInitialized { get; set; }

        private CancellationTokenSource _countdownCts;
        public event Action<List<IItemReward>, PassRewardSource> OnRewardsClaimed;
        public event Action<List<IItemReward>> OnAutoClaimedRewardsProcessed;
        public event Action<IPassNotificationEventData> OnAutoClaimNotificationTriggered;
        public event Action<IPassNotificationEventData> OnBonusBankClaimNotificationTriggered;

        private readonly ITimeProvider _timeProvider;
        public DateTime EventEndTime => _passModel?.EventEndTime ?? DateTime.MinValue;
        public bool IsEventActive => _passModel?.IsEventActive ?? false;
        private bool _firstLaunch = false;

        public PassManager(PassDatabase database, IPassSaveAdapter saveAdapter, ITimeProvider timeProvider)
            : this(database, saveAdapter, timeProvider, new PassEventScheduler())
        {
        }

        public PassManager(PassDatabase database, IPassSaveAdapter saveAdapter, ITimeProvider timeProvider, IEventScheduler eventScheduler)
        {
            _passDatabase = database;
            _passSaveAdapter = saveAdapter;
            _timeProvider = timeProvider;
            _passSchedule = eventScheduler ?? throw new ArgumentNullException(nameof(eventScheduler));
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _passModel = new PassModel(_passDatabase, _passSaveAdapter, _passSchedule, _timeProvider);
            _passModel.OnRewardsClaimed += HandleModelRewardsClaimed;
            _passModel.OnAutoClaimNotificationTriggered += HandleModelAutoClaimNotification;
            _passModel.OnBonusBankClaimNotificationTriggered += HandleModelBonusBankClaimNotification;
            _passPresenter = new PassPresenter(_passModel,_passDatabase);
            IsInitialized = true;

            _countdownCts = new CancellationTokenSource();
            CheckAndTriggerAutoClaimEvent();

            return await UniTask.FromResult(true);
        }

        public void Dispose()
        {
            if (_countdownCts != null)
            {
                _countdownCts.Cancel();
                _countdownCts.Dispose();
            }
            if (_passModel != null)
            {
                _passModel.OnRewardsClaimed -= HandleModelRewardsClaimed;
                _passModel.OnAutoClaimNotificationTriggered -= HandleModelAutoClaimNotification;
                _passModel.OnBonusBankClaimNotificationTriggered -= HandleModelBonusBankClaimNotification;
            }

            _passModel?.Cleanup();
            _passPresenter?.Cleanup();
        }

        private void CheckAndTriggerAutoClaimEvent()
        {
            var autoRewards = _passModel != null
                ? new List<IItemReward>(_passModel.AutoClaimedRewards)
                : new List<IItemReward>();

            if (autoRewards != null && autoRewards.Count > 0)
            {
                OnAutoClaimedRewardsProcessed?.Invoke(autoRewards);
                _passModel.TriggerAutoClaimNotifications();
                _passModel.ClearAutoClaimedRewards();
            }
        }
        private void HandleModelAutoClaimNotification(IPassNotificationEventData eventData)
        {
            OnAutoClaimNotificationTriggered?.Invoke(eventData);
        }

        private void HandleModelBonusBankClaimNotification(IPassNotificationEventData eventData)
        {
            OnBonusBankClaimNotificationTriggered?.Invoke(eventData);
        }
        public List<IItemReward> GetAndClearAutoClaimedRewards()
        {
            if (_passModel == null) return new List<IItemReward>();

            var rewards = new List<IItemReward>(_passModel.AutoClaimedRewards);
            _passModel.ClearAutoClaimedRewards();
            return rewards;
        }
       
        private void HandleModelRewardsClaimed(List<IItemReward> rewards, PassRewardSource source)
        {
            OnRewardsClaimed?.Invoke(rewards, source);
        }
        public void RegisterView(IPassView view)=> _passPresenter.RegisterView(view);
        public void UnregisterView(IPassView view) => _passPresenter.UnregisterView(view);
        
        public void RegisterRewardModifier(IPassRewardModifier modifier) => _passModel?.RegisterModifier(modifier);
        public void UnregisterRewardModifier(IPassRewardModifier modifier) => _passModel?.UnregisterModifier(modifier);
      
        public void AddExp(int amount, bool delayUpdateUI)
        {
            if (_passModel == null) return;

            _passModel.AddExp(amount, delayUpdateUI);
        }

        public void ForceUpdateUIWidget(IPassView view)
        {
            if (!_firstLaunch)
            {
                _passPresenter.ForceUpdateUI(view);
                _firstLaunch = true;
            }
           
        }

        public void FlushDelayedUIUpdate(IPassView view)
        {
            _passPresenter?.FlushDelayedUIUpdate(view);
        }

        public void CheckEventUpdate()
        {
            if (!IsInitialized || _passModel == null) 
            {
                UnityEngine.Debug.LogWarning("[PassManager] Chưa khởi tạo hệ thống, không thể check event update.");
                return;
            }
            _passModel.Initialize();
            CheckAndTriggerAutoClaimEvent();
        }
        
        public void ActiveNewEvent() => _passModel.ActivateNewEventManual();
        public void UnlockPremiumPass() => _passModel.UnlockPremium();

        public void RefreshData()
        {
            _passModel.RefreshData();
        }

        public bool IsFirstOpen => _passModel?.IsFirstOpen ?? true;
        public void MarkFirstOpenCompleted() => _passModel?.MarkFirstOpenCompleted();
    }
}
