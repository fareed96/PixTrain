using System;

namespace PixTrain.Game.Ads
{
    /// <summary>
    /// Editörde ve henüz reklam ağı bağlanmamış platformlarda kullanılan sahte reklam:
    /// birkaç saniyelik "TEST REKLAMI" ekranı gösterir, sonra ödülü verir.
    /// </summary>
    public sealed class TestAdProvider : IAdProvider
    {
        public const float Duration = 2.5f;

        private Action<bool> _done;
        private float _remaining;

        /// <summary>Test ekranını göstermek/gizlemek ve kalan süreyi yazmak için (sunum katmanı bağlar).</summary>
        public Action<bool, float> Overlay;

        public bool RewardedEnabled => true;
        public bool IsShowing => _done != null;

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (_done != null) { onDone(false); return; }
            _done = onDone;
            _remaining = Duration;
            Overlay?.Invoke(true, _remaining);
        }

        public void SetBanner(bool visible) { }

        public void Tick(float dt)
        {
            if (_done == null) return;
            _remaining -= dt;
            Overlay?.Invoke(_remaining > 0f, _remaining);
            if (_remaining > 0f) return;
            var done = _done;
            _done = null;
            done(true);
        }

        /// <summary>Test reklamını kapat (oyuncu vazgeçti): ödül verilmez.</summary>
        public void Skip()
        {
            if (_done == null) return;
            var done = _done;
            _done = null;
            Overlay?.Invoke(false, 0f);
            done(false);
        }
    }
}
