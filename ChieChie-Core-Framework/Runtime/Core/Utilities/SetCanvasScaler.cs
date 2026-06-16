using UnityEngine;
using UnityEngine.UI;

namespace ChieChie.Core
{
    public class SetCanvasScaler : MonoBehaviour
    {
        public CanvasScaler canvasScaler;

        private void Awake()
        {
            if (!canvasScaler)
            {
                canvasScaler = GetComponent<CanvasScaler>();
            }
        }

        void Start()
        {
            float ratio = (float)(Screen.width / (float)Screen.height);

            if (ratio >= 0.6f)
            {
                canvasScaler.matchWidthOrHeight = 1;

            }
            else
            {
                canvasScaler.matchWidthOrHeight = 0;
            }
        }

    }
}
