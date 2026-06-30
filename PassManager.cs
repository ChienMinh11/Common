using System;
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
        
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            
            IsInitialized = true;
            return UniTask.FromResult(true);
        }
        
        public void Dispose()
        {
           
        }
    }
}