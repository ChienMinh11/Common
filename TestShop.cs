using System;
using ChieChie.Constracts;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class TestShop : MonoBehaviour
    {
        private IShopService _shopService;
        private VirtualTimeProvider _virtualTimeProvider;
        
        [Title("Virtual Time Control")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Giờ Giả Lập Hiện Tại (UTC)")]
        private string CurrentVirtualTime => _virtualTimeProvider != null 
            ? _virtualTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") 
            : "Chưa Initialize";

        [Inject]
        private void Construct(IShopService shopService, ITimeProvider timeProvider)
        {
            _shopService = shopService;
            _virtualTimeProvider = timeProvider as VirtualTimeProvider;
        }

        [HorizontalGroup("TimeJump")] [Button("+5 Phút")]
        private void Add5Minutes() => JumpTime(TimeSpan.FromMinutes(5));

        [HorizontalGroup("TimeJump")] [Button("+1 Giờ")]
        private void Add1Hour() => JumpTime(TimeSpan.FromHours(1));

        [HorizontalGroup("TimeJump")] [Button("+1 Ngày")]
        private void Add1Day() => JumpTime(TimeSpan.FromDays(1));

        [Button("Reset Về Giờ Thực Tế")]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void ResetToRealTime()
        {
            if (_virtualTimeProvider == null) return;
            _virtualTimeProvider.Reset();
            NotifyShopRefresh();
        }

        private void JumpTime(TimeSpan duration)
        {
            if (_virtualTimeProvider == null) return;
            _virtualTimeProvider.AddTime(duration);
            NotifyShopRefresh();
        }

        private void NotifyShopRefresh()
        {
            if (_shopService != null)
            {
                _shopService.RefreshShopItems(); 
            }
        }
    }
}