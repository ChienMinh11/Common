using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassSaveAdapter
    {
        void SaveData(PassModel model);
        PassModel LoadData();
    }
}
