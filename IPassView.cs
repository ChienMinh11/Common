namespace ChieChie.GamePass
{
    public interface IPassView 
    {
        void RefreshPassUI(PassModel model, PassDatabase database, string remainingTimeStr);
        void ShowRewardClaimedEffect(PassRewardData reward);
    }
}
