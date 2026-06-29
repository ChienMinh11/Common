using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileService
    {
       ProfilePresenter GetProfilePresenter();
       IAvatarPresenter GetAvatarPresenter();
    }
}
