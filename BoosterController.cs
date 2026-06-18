using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Booster
{
    public class BoosterController : IBoosterService
    {
        private readonly BoosterDatabase _database;
        private readonly IBoosterResourceContext _resourceContext;
        private readonly IBoosterFactory _boosterFactory;

        private BoosterBehavior[] _activeBoosters;
        private Dictionary<string, BoosterBehavior> _boostersLink;
        private Transform _behaviorsContainer;

        private BoosterBehavior _currentAwaitingBooster;
        private CancellationTokenSource _awaitingCancelTokenSource;
        
        public event Action<string?> OnAwaitingStatusChanged;
        public event Action<string> OnPreBoosterStateChanged;
        public event Action<string> OnBoosterInfinitePassConsumed;

        public BoosterController(
            BoosterDatabase database,
            IBoosterResourceContext resourceContext, 
            IBoosterFactory boosterFactory)
        {
            _database = database;
            _resourceContext = resourceContext;
            _boosterFactory = boosterFactory;
        }

        public bool IsInitialized { get; private set; }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _behaviorsContainer = new GameObject("[BOOSTER]").transform;
            _behaviorsContainer.gameObject.isStatic = true;

            BoosterSetting[] boosterSettings = _database.Boosters;
            _activeBoosters = new BoosterBehavior[boosterSettings.Length];
            _boostersLink = new Dictionary<string, BoosterBehavior>();

            for (int i = 0; i < _activeBoosters.Length; i++)
            {
                boosterSettings[i].Initialise();
            
                var boosterBehaviorObj =_boosterFactory.CreateBooster(boosterSettings[i], _behaviorsContainer);
                
                var boosterBehavior = boosterBehaviorObj.GetComponent<BoosterBehavior>();
                boosterBehavior.InitialiseSettings(boosterSettings[i]);

                await boosterBehavior.InitialiseAsync(cancellationToken);

                _activeBoosters[i] = boosterBehavior;
                _boostersLink.Add(_activeBoosters[i].Settings.BoosterId, _activeBoosters[i]);
            }

            IsInitialized = true;
            SubscribeInfinityEvents();
            return true;
        }

        private void SubscribeInfinityEvents()
        {
            if (_resourceContext == null) return;
            
            _resourceContext.OnInfiniteDurationExpired += UpdatePreBoosterOnInfinityChanged;
            _resourceContext.OnInfiniteDurationAdded += UpdatePreBoosterOnInfinityChanged;
        }

        private void UpdatePreBoosterOnInfinityChanged(string boosterType)
        {
            if (_boostersLink.TryGetValue(boosterType, out var boosterBehavior))
            {
                if (boosterBehavior.Settings.BoosterType == BoosterType.PreBooster)
                {
                    bool isInfinite = _resourceContext.IsCurrentlyInfinite(boosterType);
                    boosterBehavior.SetSelected(isInfinite);
                    OnPreBoosterStateChanged?.Invoke(boosterType);
                }
            }
        }

        public async UniTask<bool> UseBooster(string boosterType, CancellationToken cancellationToken = default)
        {
            if (!_boostersLink.TryGetValue(boosterType, out var boosterBehavior))
            {
                return false;
            }

            switch (boosterBehavior.Settings.BoosterType)
            {
                case BoosterType.Instant:
                    CancelCurrentAwaitingBooster();
                    return await ExecuteBoosterDirectly(boosterBehavior, cancellationToken);

                case BoosterType.AwaitInput:
                    return await HandleAwaitInputBoosterFlow(boosterBehavior, cancellationToken);

                case BoosterType.PreBooster:
                    return HandlePreBoosterToggle(boosterBehavior);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTask<bool> ExecuteBoosterDirectly(BoosterBehavior behavior, CancellationToken token)
        {
            if (behavior.IsBusy || !behavior.BoosterCondition() || !HasEnoughResource(behavior)) 
                return false;
            SpendBoosterResource(behavior);
            bool success = await behavior.ActivateAsync(token);
            return success;
        }

        private async UniTask<bool> HandleAwaitInputBoosterFlow(BoosterBehavior behavior, CancellationToken token)
        {
            if (_currentAwaitingBooster == behavior)
            {
                CancelCurrentAwaitingBooster();
                return false;
            }

            if (!HasEnoughResource(behavior)) 
                return false;

            CancelCurrentAwaitingBooster();
            
            SetAwaitingBooster(behavior);
            _awaitingCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            try
            {
                bool success = await behavior.ActivateAsync(_awaitingCancelTokenSource.Token);
                if (success)
                {
                    SpendBoosterResource(behavior);
                }
                return success;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[BOOSTER] Đã hủy trạng thái chờ của {behavior.Settings.BoosterId}.");
                return false;
            }
            finally
            {
                if (_currentAwaitingBooster == behavior)
                {
                    ClearAwaitingState();
                }
            }
        }

        private bool HandlePreBoosterToggle(BoosterBehavior behavior)
        {
            string type = behavior.Settings.BoosterId;
            bool isInfinite = _resourceContext.IsCurrentlyInfinite(type);

            if (behavior.IsSelected)
            {
                behavior.SetSelected(false);
                if (!isInfinite) 
                {
                    _resourceContext.AddResource(type, behavior.Settings.Cost);
                }
                OnPreBoosterStateChanged?.Invoke(type);
                return true;
            }

            if (!HasEnoughResource(behavior)) 
                return false;

            if (!isInfinite)
            {
                _resourceContext.SpendResource(type, behavior.Settings.Cost);
            }

            behavior.SetSelected(true);
            OnPreBoosterStateChanged?.Invoke(type);
            return true;
        }

        private bool HasEnoughResource(BoosterBehavior behavior)
        {
            string type = behavior.Settings.BoosterId;
            if (_resourceContext.IsCurrentlyInfinite(type) && !behavior.HasUsedInfiniteFreePass) 
                return true;
            return _resourceContext.HasEnoughResource(type, behavior.Settings.Cost);
        }

        private void SpendBoosterResource(BoosterBehavior behavior)
        {
            string type = behavior.Settings.BoosterId;
            if (_resourceContext.IsCurrentlyInfinite(type) && !behavior.HasUsedInfiniteFreePass)
            {
                behavior.ConsumeInfiniteFreePass();
                OnBoosterInfinitePassConsumed?.Invoke(type);
            }
            else
            {
                _resourceContext.SpendResource(type, behavior.Settings.Cost);
            }
        }

        private void CancelCurrentAwaitingBooster()
        {
            if (_awaitingCancelTokenSource != null)
            {
                _awaitingCancelTokenSource.Cancel();
                _awaitingCancelTokenSource.Dispose();
                _awaitingCancelTokenSource = null;
            }
            SetAwaitingBooster(null);
        }

        private void ClearAwaitingState()
        {
            _awaitingCancelTokenSource?.Dispose();
            _awaitingCancelTokenSource = null;
            SetAwaitingBooster(null);
        }

        public void ResetBooster(string boosterType)
        {
            if (_boostersLink.TryGetValue(boosterType, out var behavior))
            {
                behavior.ResetBehaviorAsync(CancellationToken.None).Forget();
            }
        }

        public BoosterBehavior GetBoosterBehavior(string powerUpType) => _boostersLink.GetValueOrDefault(powerUpType);

        public async UniTask<bool> ActivateAllSelectedPreBoosters(CancellationToken cancellationToken = default)
        {
            if (_activeBoosters == null || _activeBoosters.Length == 0) return true;

            var selectedPreBoosters = new List<BoosterBehavior>();
            foreach (var booster in _activeBoosters)
            {
                if (booster != null && booster.Settings.BoosterType == BoosterType.PreBooster && booster.IsSelected)
                {
                    selectedPreBoosters.Add(booster);
                }
            }

            if (selectedPreBoosters.Count == 0) return true;

            var activateTasks = new List<UniTask<bool>>();
            foreach (var booster in selectedPreBoosters)
            {
                activateTasks.Add(booster.ActivateAsync(cancellationToken));
            }

            bool[] results = await UniTask.WhenAll(activateTasks);

            for (int i = 0; i < results.Length; i++)
            {
                if (!results[i])
                {
                    Debug.LogError($"[BOOSTER] Kích hoạt thất bại tại Booster: {selectedPreBoosters[i].Settings.BoosterId}");
                }
            }

            return true;
        }

        public async UniTask ResetBehaviorsAsync(CancellationToken cancellationToken = default)
        {
            if (_activeBoosters == null) return;
            foreach (var booster in _activeBoosters)
            {
                if (booster != null) await booster.ResetBehaviorAsync(cancellationToken);
            }
        }

        private void SetAwaitingBooster(BoosterBehavior behavior)
        {
            _currentAwaitingBooster = behavior;
            OnAwaitingStatusChanged?.Invoke(behavior?.Settings.BoosterId);
        }

        public void CleanUp()
        {
            if (_resourceContext != null)
            {
                _resourceContext.OnInfiniteDurationExpired -= UpdatePreBoosterOnInfinityChanged;
                _resourceContext.OnInfiniteDurationAdded -= UpdatePreBoosterOnInfinityChanged;
            }
            _awaitingCancelTokenSource?.Dispose();
            if (_behaviorsContainer != null)
            {
                UnityEngine.Object.Destroy(_behaviorsContainer.gameObject);
            }
        }
    }
}