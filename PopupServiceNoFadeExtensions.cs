using System.Linq;
using System.Reflection;
using ChieChie.Core;
using Cysharp.Threading.Tasks;

namespace Game.GamePlay
{
    public static class PopupServiceNoFadeExtensions
    {
        public static void ShowPopup(this IPopupService popupService, string popupNameId, bool noFade)
        {
            if (popupService == null) return;

            if (TryInvokeShowPopupWithNoFade(popupService, popupNameId, noFade)) return;

            popupService.ShowPopup(popupNameId);
        }

        private static bool TryInvokeShowPopupWithNoFade(IPopupService popupService, string popupNameId, bool noFade)
        {
            var serviceType = popupService.GetType();
            var method = serviceType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != nameof(IPopupService.ShowPopup)) return false;

                    var parameters = m.GetParameters();
                    return parameters.Length == 2
                           && parameters[0].ParameterType == typeof(string)
                           && parameters[1].ParameterType == typeof(bool);
                });

            if (method == null) return false;

            var result = method.Invoke(popupService, new object[] { popupNameId, noFade });
            if (result is UniTask task) task.Forget();

            return true;
        }
    }
}
