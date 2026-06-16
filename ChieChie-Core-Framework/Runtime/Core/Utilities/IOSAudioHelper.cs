// IOSAudioHelper.cs



// Added this namespace for DllImport

namespace ChieChie.Core
{
    public static class IOSAudioHelper
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RestoreAudioSession();
        
        [DllImport("__Internal")]
        private static extern void PauseAudioSession();
#else
        private static void RestoreAudioSession() { }
        private static void PauseAudioSession() { }
#endif

        public static void RestoreAudio()
        {
#if UNITY_IOS && !UNITY_EDITOR
    RestoreAudioSession();
#endif
           
         
        }
        
        public static void PauseAudio()
        {
#if UNITY_IOS && !UNITY_EDITOR
            PauseAudioSession();
#endif
            
        }
    }
    
}