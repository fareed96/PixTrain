// PixTrain reklam ayarları. Derlemeden sonra bu dosyayı sunucuda düzenleyebilirsin (oyunu yeniden derlemek gerekmez).
// Ayrıntılar: docs/ADS.md
//
// mode:
//   "off"  - reklam yok. Ödüllü teklifler (devam, 2x kazanç, günlük paket) oyunda görünmez.
//   "test" - sahte reklam: banner yer tutucu + 2,5 saniyelik "TEST REKLAMI" ekranı. Geliştirme için.
//   "live" - Google AdSense banner + H5 Games Ads ödüllü reklam (client ve slot değerleri gerekir).
//
// Adres çubuğunda ?ads=test ya da ?ads=off yazarak bu ayarı geçici olarak ezebilirsin.
window.PIXTRAIN_ADS = {
  mode: "off",

  // AdSense yayıncı kimliği, ör. "ca-pub-1234567890123456"
  client: "",

  // AdSense > Reklamlar > Reklam birimine göre > Görüntülü reklam birimlerinin slot numaraları
  bannerSlotSide: "",   // masaüstü sağ sütun (300x600)
  bannerSlotBottom: "", // telefon alt şerit (320x50)

  // true: H5 Games Ads test modu (data-adbreak-test="on"). Onay beklerken ya da yerelde dene.
  h5Test: false
};
