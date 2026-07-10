using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;

namespace ChieChie.GamePass
{
    public class PassManager: IPassService, IDisposable
    {
        private readonly PassDatabase _passDatabase;
        private readonly PassEventScheduler _passSchedule = new PassEventScheduler();
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

        public PassManager(PassDatabase database, IPassSaveAdapter saveAdapter, ITimeProvider timeProvider)
        {
            _passDatabase = database;
            _passSaveAdapter = saveAdapter;
            _timeProvider = timeProvider;
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
            var autoRewards = GetAndClearAutoClaimedRewards(); 
            if (autoRewards != null && autoRewards.Count > 0)
            {
                OnAutoClaimedRewardsProcessed?.Invoke(autoRewards);
                _passModel.TriggerAutoClaimNotifications();
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
        public List<IItemReward> GetAndClearAutoClaimedRewards() => _passModel.AutoClaimedRewards;
       
        private void HandleModelRewardsClaimed(List<IItemReward> rewards, PassRewardSource source)
        {
            OnRewardsClaimed?.Invoke(rewards, source);
        }
        public void RegisterView(IPassView view)=> _passPresenter.RegisterView(view);
        public void UnregisterView(IPassView view) => _passPresenter.UnregisterView(view);
        
        public void RegisterRewardModifier(IPassRewardModifier modifier) => _passModel?.RegisterModifier(modifier);
        public void UnregisterRewardModifier(IPassRewardModifier modifier) => _passModel?.UnregisterModifier(modifier);
        public void AddExp(int amount)
        {
            AddExp(amount, false);
        }

        public void AddExp(int amount, bool delayUpdateUI)
        {
            if (_passModel == null) return;

            _passModel.AddExp(amount, delayUpdateUI);
        }

        public void FlushDelayedUIUpdate()
        {
            _passPresenter?.FlushDelayedUIUpdate();
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
    }
}
