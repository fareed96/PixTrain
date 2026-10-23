using System;

namespace PixTrain.Game.Ads
{
    /// <summary>
    /// Reklam sağlayıcısı. Oyun yalnızca bu arayüzü bilir; arkasında web (Google H5 Games Ads),
    /// test (sahte reklam) ve ileride Android (AdMob) uygulamaları olabilir.
    /// </summary>
    public interface IAdProvider
    {
        /// <summary>Ödüllü reklam düğmeleri gösterilsin mi (reklamlar kapalıysa düğme hiç çıkmaz).</summary>
        bool RewardedEnabled { get; }

        /// <summary>Reklam şu an ekranda mı.</summary>
        bool IsShowing { get; }

        /// <summary>Ödüllü reklam gösterir; sonuç: true = sonuna kadar izlendi, false = kapatıldı ya da reklam yok.</summary>
        void ShowRewarded(string placement, Action<bool> onDone);

        /// <summary>Sayfadaki banner reklamı gösterir ya da gizler (sefer sırasında gizlenir).</summary>
        void SetBanner(bool visible);

        /// <summary>Her karede çağrılır (zamanlayıcılı sağlayıcılar için).</summary>
        void Tick(float dt);
    }

    /// <summary>Reklamlar kapalı (sayfada reklam yapılandırılmamış): ödüllü düğmeler gösterilmez.</summary>
    public sealed class NoAdProvider : IAdProvider
    {
        public bool RewardedEnabled => false;
        public bool IsShowing => false;
        public void ShowRewarded(string placement, Action<bool> onDone) => onDone(false);
        public void SetBanner(bool visible) { }
        public void Tick(float dt) { }
    }
}
