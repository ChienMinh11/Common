using UnityEngine;

namespace ChieChie.Core
{
    public interface IInfiniteResourceView
    {
        void SetInfiniteStatus(bool isInfinite);
        void UpdateRemainingTimeText(string formattedTime);
    }
}
