using System;
using System.Runtime.InteropServices;

namespace PixTrain.Game.Ads
{
    /// <summary>
    /// WebGL: reklamı oyunu barındıran sayfa gösterir (Plugins/WebGL/PixTrainAds.jslib → sayfadaki window.PixTrainAds).
    /// Sayfa Google H5 Games Ads (adBreak) ile ödüllü reklamı ve AdSense banner'ı yönetir; sonucu
    /// SendMessage("PixTrain", "OnAdResult", "viewed|dismissed|unavailable") ile geri bildirir.
    /// </summary>
    public sealed class WebAdProvider : IAdProvider
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int PixTrainAds_IsAvailable();
        [DllImport("__Internal")] private static extern void PixTrainAds_ShowRewarded(string placement);
        [DllImport("__Internal")] private static extern void PixTrainAds_SetBanner(int visible);
#else
        private static int PixTrainAds_IsAvailable() => 0;
        private static void PixTrainAds_ShowRewarded(string placement) { }
        private static void PixTrainAds_SetBanner(int visible) { }
#endif

        private Action<bool> _done;
        private float _timeout;

        /// <summary>Sayfa reklam yapılandırılmış mı (ads-config.js'te mode "off" değilse).</summary>
        public static bool PageHasAds()
        {
            try { return PixTrainAds_IsAvailable() == 1; }
            catch (Exception) { return false; }
        }

        public bool RewardedEnabled => true;
        public bool IsShowing => _done != null;

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (_done != null) { onDone(false); return; }
            _done = onDone;
            _timeout = 90f;
            PixTrainAds_ShowRewarded(placement);
        }

        public void SetBanner(bool visible) => PixTrainAds_SetBanner(visible ? 1 : 0);

        /// <summary>Sayfanın gönderdiği sonuç.</summary>
        public void OnResult(string result)
        {
            if (_done == null) return;
            var done = _done;
            _done = null;
            done(result == "viewed");
        }

        public void Tick(float dt)
        {
            // Sayfa hiç cevap vermezse oyun kilitlenmesin.
            if (_done == null) return;
            _timeout -= dt;
            if (_timeout <= 0f) OnResult("timeout");
        }
    }
}
