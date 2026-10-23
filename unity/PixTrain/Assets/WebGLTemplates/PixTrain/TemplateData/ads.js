// PixTrain reklam köprüsü (sayfa tarafı).
// Oyun (Plugins/WebGL/PixTrainAds.jslib) şu üç fonksiyonu çağırır:
//   PixTrainAds.available()            reklam açık mı
//   PixTrainAds.showRewarded(placement) ödüllü reklam iste
//   PixTrainAds.setBanner(visible)      menülerde banner aç, seferde kapat
// Sonuç oyuna SendMessage("PixTrain", "OnAdResult", "viewed" | "dismissed" | "unavailable") ile döner.
// Reklam sırasında ses "OnAdAudio" ("pause" | "resume") ile susturulur.
(function () {
  "use strict";

  var cfg = window.PIXTRAIN_ADS || {};
  var mode = cfg.mode || "off";
  try {
    var q = new URLSearchParams(window.location.search).get("ads");
    if (q === "off" || q === "test" || q === "live") mode = q;
  } catch (e) { /* eski tarayıcı */ }
  if (mode === "live" && !cfg.client) {
    console.warn("[PixTrain] ads-config.js: live modu için client gerekli, reklamlar kapatıldı.");
    mode = "off";
  }

  var unity = null;
  var queue = [];
  var busy = false;
  var bannerOn = false;
  var pushed = {};

  function send(method, arg) {
    if (unity) unity.SendMessage("PixTrain", method, arg);
    else queue.push([method, arg]);
  }

  function finish(result) {
    busy = false;
    send("OnAdAudio", "resume");
    send("OnAdResult", result);
    var canvas = document.getElementById("unity-canvas");
    if (canvas) canvas.focus();
  }

  function isDesktop() {
    return window.matchMedia("(min-width: 900px) and (min-aspect-ratio: 1/1)").matches;
  }

  // ---------------- canlı: AdSense + H5 Games Ads ----------------

  function loadAdSense() {
    window.adsbygoogle = window.adsbygoogle || [];
    window.adBreak = window.adConfig = function (o) { window.adsbygoogle.push(o); };

    var s = document.createElement("script");
    s.async = true;
    s.crossOrigin = "anonymous";
    s.src = "https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=" + encodeURIComponent(cfg.client);
    // Geçiş reklamı kullanmıyoruz; yine de iki reklam arası en az 2 dakika olsun.
    s.setAttribute("data-ad-frequency-hint", "120s");
    if (cfg.h5Test) s.setAttribute("data-adbreak-test", "on");
    document.head.appendChild(s);

    window.adConfig({ preloadAdBreaks: "on", sound: "on" });
  }

  function fillSlot(slotId, adSlot, format) {
    var el = document.getElementById(slotId);
    if (!el || pushed[slotId] || !adSlot) return;
    pushed[slotId] = true;
    el.classList.add("ready");
    var ins = document.createElement("ins");
    ins.className = "adsbygoogle";
    ins.style.display = "block";
    ins.style.width = "100%";
    ins.style.height = "100%";
    ins.setAttribute("data-ad-client", cfg.client);
    ins.setAttribute("data-ad-slot", adSlot);
    if (format) ins.setAttribute("data-ad-format", format);
    el.appendChild(ins);
    try { window.adsbygoogle.push({}); } catch (e) { console.warn("[PixTrain] banner:", e); }
  }

  function showRewardedLive(placement) {
    var result = null;
    window.adBreak({
      type: "reward",
      name: placement,
      beforeAd: function () { send("OnAdAudio", "pause"); },
      afterAd: function () { send("OnAdAudio", "resume"); },
      // Oyuncu ödül teklifini oyunun içinde zaten kabul etti; reklamı hemen göster.
      beforeReward: function (showAdFn) { showAdFn(); },
      adDismissed: function () { result = "dismissed"; },
      adViewed: function () { result = "viewed"; },
      adBreakDone: function (info) {
        if (!result && info && info.breakStatus !== "viewed") console.info("[PixTrain] reklam yok:", info.breakStatus);
        finish(result || "unavailable");
      }
    });
  }

  // ---------------- test: sahte reklam ----------------

  function placeholder(slotId, text) {
    var el = document.getElementById(slotId);
    if (!el || pushed[slotId]) return;
    pushed[slotId] = true;
    el.classList.add("ready");
    var div = document.createElement("div");
    div.className = "ad-placeholder";
    div.textContent = text;
    el.appendChild(div);
  }

  function showRewardedTest() {
    var box = document.getElementById("test-ad");
    if (!box) {
      box = document.createElement("div");
      box.id = "test-ad";
      box.innerHTML = '<b>TEST REKLAMI</b><div class="count"></div><div>Süre dolunca ödül verilir.</div>' +
        '<button type="button">Kapat (ödül yok)</button>';
      document.body.appendChild(box);
    }
    var count = box.querySelector(".count");
    var btn = box.querySelector("button");
    var left = 3;
    var timer = null;

    function close(result) {
      clearInterval(timer);
      btn.onclick = null;
      box.classList.remove("on");
      finish(result);
    }

    send("OnAdAudio", "pause");
    count.textContent = left;
    box.classList.add("on");
    btn.onclick = function () { close("dismissed"); };
    timer = setInterval(function () {
      left -= 1;
      count.textContent = left;
      if (left <= 0) close("viewed");
    }, 1000);
  }

  // ---------------- dışa açık arayüz ----------------

  function setupSlots() {
    if (mode === "test") {
      placeholder("ad-side", "REKLAM ALANI 300x600 (test)");
      placeholder("ad-bottom", "REKLAM ALANI 320x50 (test)");
    } else if (mode === "live") {
      // Gizli bir kutuya reklam yüklenemez: masaüstünde yan, telefonda (ilk açılışta) alt banner.
      if (isDesktop()) fillSlot("ad-side", cfg.bannerSlotSide, null);
      else if (bannerOn) fillSlot("ad-bottom", cfg.bannerSlotBottom, "horizontal");
    }
  }

  window.PixTrainAds = {
    mode: mode,

    attach: function (instance) {
      unity = instance;
      var pending = queue;
      queue = [];
      for (var i = 0; i < pending.length; i++) send(pending[i][0], pending[i][1]);
    },

    available: function () { return mode !== "off"; },

    showRewarded: function (placement) {
      if (busy) { send("OnAdResult", "unavailable"); return; }
      if (mode === "off") { send("OnAdResult", "unavailable"); return; }
      busy = true;
      if (mode === "test") showRewardedTest();
      else showRewardedLive(placement || "reward");
    },

    setBanner: function (visible) {
      bannerOn = !!visible;
      if (mode === "off") return;
      document.body.classList.toggle("banner-on", bannerOn);
      setupSlots();
    }
  };

  if (mode === "live") loadAdSense();
  if (mode !== "off") {
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", setupSlots);
    else setupSlots();
    window.addEventListener("resize", setupSlots);
  }
})();
