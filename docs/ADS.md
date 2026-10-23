# Yayın ve reklam rehberi

PixTrain ücretsiz bir web oyunudur: kendi alan adında, telefonda ve masaüstünde tarayıcıdan oynanır. Gelir reklamdan gelir; uygulama içi satın alma yoktur.

## Reklam ilkeleri

Oyuncunun keyfini bozmamak için:

| Kural | Uygulama |
|---|---|
| Sefer sırasında reklam yok | Banner yalnızca başlık, hat haritası ve sonuç ekranında görünür; sefer başlayınca kapanır ve oyun alanı yeniden büyür. Oyunun üstüne binmez. |
| Geçiş (zorunlu, tam ekran) reklam yok | Hiçbir ekran geçişinde kendiliğinden reklam açılmaz. |
| Ödüllü reklam her zaman isteğe bağlı | Oyuncu düğmeye basmadan açılmaz. İzlemezse hiçbir şey kaybetmez; teklif normal oyunun üstüne eklenen bir bonustur. |
| Ödül oyun içidir | Altın ve yakıt. Gerçek para değeri yoktur; "bizi destekle" gibi bir çağrı yoktur (Google politikası). |
| Sınır | Günlük altın paketi günde 3 kez. Sefer başına bir kez "yola devam". |

### Ödüllü reklam teklifleri

| Teklif | Nerede | Ödül | Kural (Core) |
|---|---|---|---|
| **Yola devam** | Yakıt bitip tren durduğunda açılan pencere | Deponun yarısı kadar yakıt, sefer kaldığı yerden sürer | `RewardType.ContinueRun`, sefer başına 1 |
| **Kazancı 2× al** | Sefer sonu ekranı | Seferde kazanılan altın kadar bonus | `RewardType.DoubleEarnings`, sefer başına 1 |
| **2× topla** | Hat haritası, hat geliri kartı | Biriken hat gelirinin iki katı | `RewardType.DoubleIdle` |
| **Günlük altın paketi** | Garaj sekmesinin en üstü | 80 + 40 × açılan hat sayısı altın | `RewardType.DailyCoins`, günde 3 (UTC) |

Miktarlar `GameBalance` içinde (`ContinueFuelFraction`, `DailyRewardLimit`, `DailyCoinsBase`, `DailyCoinsPerRoute`). Kurallar `IRewardService` / `RewardService` ve testleri `RewardServiceTests`.

## Nasıl çalışır

```
Oyun (C#)                          Sayfa (JavaScript)                    Google
GameController.RequestReward  ─►  WebAdProvider ─► PixTrainAds.jslib ─► window.PixTrainAds.showRewarded
                                                                      ─► adBreak({ type: "reward" })  (H5 Games Ads)
GameController.OnAdResult     ◄─  SendMessage("PixTrain", "OnAdResult", "viewed|dismissed|unavailable")
GameController.OnAdAudio      ◄─  SendMessage("PixTrain", "OnAdAudio", "pause|resume")
Show(ekran) → SetBanner       ─►  body.banner-on (alt banner aç/kapa) ─► AdSense görüntülü reklam birimi
```

| Ortam | Reklam sağlayıcı |
|---|---|
| Unity editörü, Android | `TestAdProvider`: 2,5 saniyelik "TEST REKLAMI" ekranı, sonra ödül |
| WebGL, `ads-config.js` mode `off` | `NoAdProvider`: ödüllü teklifler hiç görünmez, yakıt bitince sefer biter |
| WebGL, mode `test` | Sayfa sahte reklam gösterir: banner yer tutucusu + 3 saniyelik test reklamı |
| WebGL, mode `live` | AdSense banner + H5 Games Ads ödüllü reklam |

Sayfa şablonu: [`unity/PixTrain/Assets/WebGLTemplates/PixTrain`](../unity/PixTrain/Assets/WebGLTemplates/PixTrain)

| Dosya | İçerik |
|---|---|
| `index.html` | Yükleme ekranı, oyun sütunu, alt banner (telefon), sağ banner + nasıl oynanır (masaüstü) |
| `ads-config.js` | Reklam ayarı. **Derlemeden sonra sunucuda düzenlenebilir**, oyunu yeniden derlemek gerekmez |
| `TemplateData/ads.js` | `window.PixTrainAds`: AdSense yükleme, banner, ödüllü reklam, test modu |
| `privacy.html` | Gizlilik ve çerez sayfası (KVKK + GDPR). Köşeli parantezli alanları doldur |
| `ads.txt` | AdSense yayıncı satırı (onaydan sonra açılır) |
| `manifest.webmanifest` | Ana ekrana ekle: tam ekran, dikey, ikonlar |

Adres çubuğunda `?ads=test` ya da `?ads=off` ayarı geçici olarak ezer. Örnek: `https://alanadin.com/?ads=test`.

## Yayına alma adımları

### 1. Alan adı + GitHub Pages (ücretsiz barındırma)

1. Repo → **Settings → Pages → Build and deployment → Source: GitHub Actions**.
2. Repo → **Settings → Secrets and variables → Actions → Variables → New repository variable**: ad `DEPLOY_PAGES`, değer `true`.
3. CI yalnızca **varsayılan daldaki** push'ta yayınlar. Şu an varsayılan dal `claude/brave-goldberg-edu5sd`; ileride `main`'e geçersen orası olur.
4. Alan adı: **Settings → Pages → Custom domain** kutusuna `alanadin.com` yaz. Alan adı sağlayıcının DNS panelinde:
   - kök alan için `A` kayıtları: `185.199.108.153`, `185.199.109.153`, `185.199.110.153`, `185.199.111.153`
   - `www` için `CNAME` → `fareed96.github.io`
   DNS doğrulandıktan sonra **Enforce HTTPS**'i işaretle.

Not: WebGL derlemesi sıkıştırmasız üretilir (Pages, Brotli/Gzip başlıklarını ayarlamaya izin vermez). Boyut yaklaşık 17 MB; tarayıcı ilk açılıştan sonra önbellekten yükler.

### 2. Google AdSense hesabı ve site onayı

1. https://adsense.google.com adresinden hesap aç (Google hesabıyla). Ödeme bilgisi ve vergi formu, gelir 100 $ eşiğine yaklaşınca istenir; reklam göstermek için kart gerekmez.
2. **Siteler → Site ekle** → alan adını gir. AdSense'in verdiği doğrulama yöntemlerinden **ads.txt** ya da **meta etiketi** kullanılabilir:
   - `ads.txt`: şablondaki satırın başındaki `#`'i kaldır, `pub-...` kimliğini yaz.
   - ya da `index.html`'deki `<head>` içine AdSense'in verdiği `<meta name="google-adsense-account" ...>` satırını ekle.
3. Onay için site yayında, gizlilik sayfası erişilebilir ve içerik özgün olmalı. Onay birkaç gün ile birkaç hafta sürebilir.
4. **Gizlilik ve mesajlaşma → Avrupa düzenlemeleri** altında Google'ın onay mesajını (CMP) oluştur ve yayınla. Avrupa'dan gelen oyunculara onay penceresini Google gösterir; kod gerekmez.

### 3. Banner reklam birimleri

AdSense → **Reklamlar → Reklam birimine göre → Görüntülü reklamlar**:

| Birim | Boyut | ads-config.js alanı |
|---|---|---|
| PixTrain yan | Dikey, sabit 300×600 | `bannerSlotSide` |
| PixTrain alt | Yatay, duyarlı (telefonda 320×50) | `bannerSlotBottom` |

Oluşturunca verilen koddaki `data-ad-slot="1234567890"` değerini ilgili alana yaz.

### 4. Ödüllü reklam: H5 Games Ads

Ödüllü reklamlar Google'ın **Ad Placement API**'si (H5 Games Ads) ile gelir. Bu özellik AdSense'te ayrıca başvuru ister:

1. AdSense Yardım Merkezi'nde **"H5 Games Ads"** sayfasını aç ve oradaki başvuru formunu doldur (oyunun yayındaki adresi gerekir).
2. Onay beklerken `h5Test: true` ile dene: Google test reklamları gösterir, gelir oluşmaz.
3. Onay gelince `h5Test: false` yap.

H5 Games Ads onaylanmazsa ödüllü teklifler `unavailable` döner: oyuncuya "Reklam şu an gösterilemedi" denir, oyun normal devam eder. Banner reklamlar bundan etkilenmez.

### 5. ads-config.js'i canlıya al

```js
window.PIXTRAIN_ADS = {
  mode: "live",
  client: "ca-pub-1234567890123456",
  bannerSlotSide: "1111111111",
  bannerSlotBottom: "2222222222",
  h5Test: false
};
```

Bu dosyayı repoda (`unity/PixTrain/Assets/WebGLTemplates/PixTrain/ads-config.js`) değiştirip push'larsan CI yeni derlemeyle birlikte yayınlar.

## Kontrol listesi

- [ ] Alan adı alındı, DNS GitHub Pages'e yönlendirildi, HTTPS açık
- [ ] `DEPLOY_PAGES=true`, Pages kaynağı GitHub Actions
- [ ] `privacy.html` içindeki [ALAN ADI], [E-POSTA ADRESİ], [TARİH] dolduruldu
- [ ] AdSense hesabı açıldı, site eklendi, `ads.txt` ya da meta etiketi eklendi
- [ ] AdSense onayı geldi
- [ ] Google CMP (Gizlilik ve mesajlaşma) yayınlandı
- [ ] İki banner birimi oluşturuldu, slot numaraları `ads-config.js`'te
- [ ] H5 Games Ads başvurusu yapıldı (onaya kadar `h5Test: true`)
- [ ] `mode: "live"`

## Neyin nerede test edildiği

| Kontrol | Nasıl |
|---|---|
| Ödül kuralları (miktar, günlük sınır, sefer başına bir kez) | `RewardServiceTests` (dotnet + Unity EditMode) |
| Yakıt bitince durma, devam, vazgeçme | `RunServiceTests` |
| Sayfa ↔ oyun köprüsü, test reklamı, banner aç/kapa | CI'daki **Tarayıcı duman testi**: WebGL derlemesini başsız Chromium'da `?ads=test` ile açar; başlık → hat → sefer → yakıt biter → test reklamı → yola devam akışını dener ([`tools/webgl-smoke`](../tools/webgl-smoke)) |
| Gerçek Google reklamları | Yalnızca onaylı alan adında görülebilir (AdSense yerelde ve onaysız sitede reklam göstermez) |
