using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IPopupLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string popupNameId, CancellationToken cancellationToken = default);
        void ReleasePrefab(string popupNameId);
        void ReleaseAll(); 
    }
}
