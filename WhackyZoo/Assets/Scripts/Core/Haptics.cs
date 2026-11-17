using UnityEngine;

namespace WhackyZoo
{
    /// <summary>Rung ngắn khi đập trúng. Handheld.Vibrate rung ~0,5 giây quá dài nên Android gọi thẳng Vibrator.</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;
#endif

        public static void Tap(int milliseconds)
        {
            if (!SaveSystem.Data.settings.vibrate || milliseconds <= 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                }
                if (sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, -1))
                        vibrator.Call("vibrate", oneShot);
                }
                else
                {
                    vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch
            {
                // Gọi Handheld.Vibrate cũng để Unity tự thêm quyền VIBRATE vào AndroidManifest
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
