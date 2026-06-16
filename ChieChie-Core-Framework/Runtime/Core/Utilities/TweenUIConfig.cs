using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Core
{
    [CreateAssetMenu(fileName = "TweenUIConfig", menuName = "CORE/UI/Tween UI Config")]
    public class TweenUIConfig : ScriptableObject
    {
        public TweenUI.AnimType animationType = TweenUI.AnimType.Scale;
        public AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public  AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float duration = 0.3f;
        public float delay = 0f;
    }

}
