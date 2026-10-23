// Oyun (C#) ile sayfa (JavaScript) arasındaki reklam köprüsü.
// Sayfadaki window.PixTrainAds nesnesi WebGL şablonunda (Assets/WebGLTemplates/PixTrain/ads.js) tanımlıdır.
mergeInto(LibraryManager.library, {
  PixTrainAds_IsAvailable: function () {
    return (typeof window !== 'undefined' && window.PixTrainAds && window.PixTrainAds.available()) ? 1 : 0;
  },

  PixTrainAds_ShowRewarded: function (placementPtr) {
    var placement = UTF8ToString(placementPtr);
    if (window.PixTrainAds) window.PixTrainAds.showRewarded(placement);
  },

  PixTrainAds_SetBanner: function (visible) {
    if (window.PixTrainAds) window.PixTrainAds.setBanner(!!visible);
  }
});
