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
        private PassModel _passModel;
        private PassPresenter _passPresenter;
        public bool IsInitialized { get; set; }

        private CancellationTokenSource _countdownCts;

        public PassManager(PassDatabase database, IPassSaveAdapter saveAdapter)
        {
            _passDatabase = database;
            _passSaveAdapter = saveAdapter;
        }

        public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _passModel = new PassModel(_passDatabase, _passSaveAdapter, _passSchedule);
            _passPresenter = new PassPresenter(_passModel,_passDatabase);
            IsInitialized = true;

            _countdownCts = new CancellationTokenSource();
        

            return await UniTask.FromResult(true);
        }

        public void Dispose()
        {
            if (_countdownCts != null)
            {
                _countdownCts.Cancel();
                _countdownCts.Dispose();
            }

            _passModel?.Cleanup();
            _passPresenter?.Cleanup();
        }

        public void RegisterView(IPassView view)=> _passPresenter.RegisterView(view);
        public void UnregisterView(IPassView view) => _passPresenter.UnregisterView(view);
        public void AddExp(int amount)
        {
           _passModel.AddExp(amount);
        }
    }
}