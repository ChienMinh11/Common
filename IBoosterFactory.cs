using UnityEngine;

namespace ChieChie.Booster
{
    public interface IBoosterFactory
    {
        BoosterBehavior CreateBooster(BoosterSetting setting, Transform parent);
    }
}
