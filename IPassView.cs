using System;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    // DTO chứa toàn bộ dữ liệu cần để vẽ UI vẽ lên, View chỉ việc đọc thông tin này ra hiển thị
    public class PassViewData
    {
        public string RemainingTimeStr;
        public int CurrentExp;
        public int CurrentMilestoneIndex;
        public bool IsPremiumUnlocked;
        public List<MilestoneUIData> Milestones;
        public int AvailableBonusClaims;
        public int BonusProgressCurrent;
        public int BonusProgressMax;
    }

    public class MilestoneUIData
    {
        public int Index;
        public int RequiredExp;
        public List<PassRewardData> FreeRewards;
        public List<PassRewardData> PremiumRewards;
        public MilestoneState FreeState;
        public MilestoneState PremiumState;
    }

    public interface IPassView 
    {
        // View cung cấp các Event click từ UI để Presenter lắng nghe thông qua interface
        event Action<int, bool> OnClaimRewardClicked; // Index, IsPremium
        event Action OnClaimBonusClicked;
        event Action OnBuyPremiumClicked;

        // Hàm để Presenter đẩy dữ liệu mới nhất bắt View cập nhật giao diện
        void RefreshUI(PassViewData viewData);
        void ShowRewardAnimation(List<PassRewardData> rewards);
    }
}