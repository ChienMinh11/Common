using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ChieChie.GamePass
{
    public class PassManager: IPassService, IDisposable
    {
        private readonly PassDatabase _passDatabase;
        private readonly PassEventScheduler _passSchedule = new PassEventScheduler();
        private readonly IPassSaveAdapter _passSaveAdapter;
        
        public bool IsInitialized { get; set; }
        public PassModel Model { get; private set; }
        public PassEventScheduler Scheduler => _passSchedule;
        public PassDatabase Database => _passDatabase;


        public static event Action OnPassDataChanged;

        public PassManager(PassDatabase database, IPassSaveAdapter saveAdapter)
        {
            _passDatabase = database;
            _passSaveAdapter = saveAdapter;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _passSchedule.UpdateMonthlySchedule(DateTime.UtcNow);

            Model = _passSaveAdapter.LoadData() ?? new PassModel();

            if (Model.currentEventId != _passSchedule.eventId)
            {
                Model.Reset(_passSchedule.eventId);
                _passSaveAdapter.SaveData(Model);
            }

            IsInitialized = true;
            return UniTask.FromResult(true);
        }

       
        public void AddPoints(int amount)
        {
            if (!IsInitialized || _passSchedule.IsExpired(DateTime.UtcNow)) return;

            int maxTierCount = _passDatabase.PassItems.Count;
         
            if (Model.currentTierIndex >= maxTierCount)
            {
                Model.bonusPoints += amount;
                SaveAndNotify();
                return;
            }

            Model.currentPoints += amount;

            while (Model.currentTierIndex < maxTierCount)
            {
                PassData currentTierData = _passDatabase.PassItems[Model.currentTierIndex];
                if (Model.currentPoints >= currentTierData.requiredAmount)
                {
                    Model.currentPoints -= currentTierData.requiredAmount;
                    Model.currentTierIndex++;
                }
                else
                {
                    break;
                }
            }

            if (Model.currentTierIndex >= maxTierCount)
            {
                Model.bonusPoints += Model.currentPoints;
                Model.currentPoints = 0;
            }

            SaveAndNotify();
        }

        public bool CanClaimReward(int tierIndex, bool isPremium)
        {
            if (Model.currentTierIndex < tierIndex) return false;

            if (isPremium)
            {
                return Model.isPremiumUnlocked && !Model.claimedPremiumTiers.Contains(tierIndex);
            }
            else
            {
                return !Model.claimedFreeTiers.Contains(tierIndex);
            }
        }

        public void ClaimReward(int tierIndex, bool isPremium)
        {
            if (!CanClaimReward(tierIndex, isPremium)) return;

            PassData tierData = _passDatabase.PassItems[tierIndex];
            List<PassRewardData> rewards = isPremium ? tierData.PremiumPassrewards : tierData.freePassrewards;
        

            if (isPremium)
                Model.claimedPremiumTiers.Add(tierIndex);
            else
                Model.claimedFreeTiers.Add(tierIndex);

            SaveAndNotify();
        }

        public bool CanClaimBonus()
        {
            if (Model.currentTierIndex < _passDatabase.PassItems.Count) return false;
            if (_passDatabase.BonusPassItems.Count == 0) return false;

            PassBonusData bonusData = _passDatabase.BonusPassItems[0];
            return Model.bonusPoints >= bonusData.requiredAmount;
        }

        public void ClaimBonus()
        {
            if (!CanClaimBonus()) return;

            PassBonusData bonusData = _passDatabase.BonusPassItems[0];
            Model.bonusPoints -= bonusData.requiredAmount;
            Model.claimedBonusCount++;

            SaveAndNotify();
        }

        public void BuyPremium()
        {
            if (Model.isPremiumUnlocked) return;

            Model.isPremiumUnlocked = true;
            SaveAndNotify();
            
        }

        private void SaveAndNotify()
        {
            _passSaveAdapter.SaveData(Model);
            OnPassDataChanged?.Invoke();
        }

        public void Dispose()
        {
            
        }
    }
}