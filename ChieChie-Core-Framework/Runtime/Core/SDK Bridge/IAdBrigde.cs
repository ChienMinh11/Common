using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public enum AdType
    {
        None = 0,
        Banner = 1,
        Interstitial = 2,
        Rewarded =3,
    }
    public enum AdNetwork
    {
        None = 0,
        Dummy = 1,
        UnityAds = 2,
        AdMob =3,
        IronSource = 4
    }
    public interface IAdBrigde 
    {
        AdNetwork CurrentNetwork { get; }
        bool IsInitialized { get; }
        bool IsAdsRemoved { get; }

        // --- Methods ---
        bool IsBannerVisible();
        UniTask<bool> SwitchNetwork(AdNetwork newNetwork, bool force = false);
        void ShowBanner();
        void HideBanner();
        void RemoveAds();
        void ShowInterstitial(Action onComplete = null);
        void ShowRewarded(Action onComplete = null);
        void ActivateInterstitialCooldown();
        bool IsRewardedAdReady();
    }
}
