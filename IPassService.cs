using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassService 
    {
       void RegisterView(IPassView view);
       void UnregisterView(IPassView view);
    }
}
