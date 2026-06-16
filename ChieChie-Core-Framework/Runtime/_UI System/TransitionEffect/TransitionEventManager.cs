using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public class TransitionEventManager 
    {
        public event Func<UniTask> OnPlayTransitionInAsync;
        public event Func<float, UniTask> OnPlayTransitionOutAsync;

        public async UniTask RaiseTransitionInAsync()
        {
            if (OnPlayTransitionInAsync != null)
            {
                var targets = OnPlayTransitionInAsync.GetInvocationList().Cast<Func<UniTask>>();
                foreach (var target in targets) await target.Invoke();
            }
        }

        public async UniTask RaiseTransitionOutAsync(float delaySeconds = -1f)
        {
            if (OnPlayTransitionOutAsync != null)
            {
                var targets = OnPlayTransitionOutAsync.GetInvocationList().Cast<Func<float, UniTask>>();
                foreach (var target in targets)
                {
                    await target.Invoke(delaySeconds); // Truyền delay xuống Listener
                }
            }
        }
    }
}