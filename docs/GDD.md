# PixTrain: Oyun Tasarım Dokümanı (v0.1)

> **Tek cümle:** Dikey ekranda, pixel art bir trenin makaslarını sen değiştirirsin. Hangi rayı seçtiğine göre yolcu mu, kömür mü, gıda mı taşıyacağın, nerede satacağın, ne kadar kazanacağın ve hangi kasabanın şehre dönüşeceği belirlenir.

Oynanabilir prototip: [`prototype/index.html`](../prototype/index.html) (tek dosya, tarayıcıda açılır).

---

## 1. Oyuncunun amacı (neden oynuyorum?)

Oyuncu, terk edilmiş bir bölgesel demiryolu şirketini devralır. Elinde eski bir buharlı lokomotif ("Kara Duman") ve iki vagon vardır.

| Ölçek | Hedef | Oyuncu ne görür? |
|---|---|---|
| **Anlık (5–10 sn)** | Sıradaki makası doğru yöne çevir | Sarı ray, trenin gideceği yolu gösterir |
| **Sefer (1,5–3 dk)** | Şehirden şehre yakıt bitmeden var, sözleşmeleri tamamla | 3 yıldız, kazanç dökümü |
| **Oturum (10–20 dk)** | Garajda treni geliştir, yeni hattı aç | Tren büyür, vagonlar dolar |
| **Uzun vade** | 7 şehri bağla, her hattı 3 yıldızla, Başkent'e ulaş | Şehirler **Köy → Kasaba → İlçe → Şehir → Metropol** olarak görsel büyür |

**Duygusal ödül:** Hizmet ettiğin şehrin büyüdüğünü görmek. Her varışta, hat haritasındaki şehir silüeti kazandığın yıldızlarla büyür. Oyunun "amacı" bu görsel dönüşüm.

---

## 2. Çekirdek döngü

```
 ┌──────────── SEFER (arcade, karar) ────────────┐
 │ makas seç → peron/tehlike → yük al/sat → yakıt │
 └───────────────┬────────────────────────────────┘
                 ▼
     Varış: yük ×1.5, yıldızlar, şehir büyür
                 ▼
 ┌──────── GARAJ (geliştirme) ────────┐     ┌──── HAT GELİRİ (idle) ────┐
 │ motor, depo, vagon, far, lokomotif │◄────┤ tamamlanan hatlar dakika  │
 └───────────────┬────────────────────┘     │ başına altın üretir       │
                 ▼                           └───────────────────────────┘
          yeni / daha zor hat
```

- **Arcade katmanı:** Sefer sırasında sürekli küçük kararlar (hangi şerit, düdük ne zaman).
- **Strateji katmanı:** Vagon kapasitesi sınırlı. Doluyken yolcu peronuna girmek boşa gider, boşken şehre girmek kazandırmaz. Yakıtla kazanç arasında denge kurmak gerekir.
- **Idle katmanı:** Tamamlanan her hat, oyuncu yokken de *hat geliri* üretir (en fazla 4 saat birikir). Yıldız sayısı ve lokomotif tipi geliri artırır.

---

## 3. Kontroller (tek el, dikey)

Ekranda düğme yok; her şey parmak hareketiyle yapılır.

| Hareket | Etki |
|---|---|
| Parmağı **sola / sağa kaydır** | Sıradaki makas bir adım sola / sağa döner. Kaydırmaya devam edersen bir adım daha döner |
| Parmağı **aşağı çek** | **Düt düt!** Düdük çalar, önündeki inekler raydan kaçar (6 sn bekleme) |
| Parmağı **yukarı it** | **Turbo**: 2,6 sn hızlanma (10 sn bekleme) |
| Klavye (masaüstü) | ← → / A D makas, ↓ / S / Boşluk düdük, ↑ / W turbo, Esc mola |

Tren kendi kendine gider. Oyuncu yönü, düdüğün zamanını ve turbonun ne zaman kullanılacağını seçer. Bu, idle hissini korurken her 3–4 saniyede bir karar üretir. Ekranın alt köşelerindeki DÜDÜK ve TURBO göstergeleri bekleme süresini gösterir.

---

## 4. Sistemler

### 4.1 Hat ve makaslar
- 3 paralel şerit. Her ~180 px'de bir **makas bölgesi** (çift makaslı geçiş) gelir.
- Makas yalnızca bir şerit kaydırır, yani 2 şerit ötedeki fırsat **iki makas sonrası** planlanmalıdır.
- Sıradaki makasın yönü sarı rayla ve ok işaretiyle gösterilir; geçildikten sonra makas düze döner.

### 4.2 Peronlar
| Peron | Renk | Ne yapar |
|---|---|---|
| Kasaba | Mavi | Yolcu verir (yolcu vagonuna) |
| Maden | Gri | Kömür verir (yük vagonuna) |
| Çiftlik | Yeşil | Gıda verir (yük vagonuna) |
| Şehir | Pembe | Yolcu + gıda satın alır |
| Fabrika | Turuncu | Kömür satın alır |
| Yakıt | Kırmızı | +45 yakıt |
| Varış garı | Kırmızı büyük kanopi | Kalan **tüm** yükü ×1.5'e alır |

Her peronun üstünde **tahmini değer** yazar (`+3`, `DOLU`, `+54`). Oyuncu kararını görerek verir.

### 4.3 Vagonlar ve yük
- Yolcu vagonu (mavi): yalnızca yolcu. Yük vagonu (kahverengi, açık): kömür ve gıda karışık.
- Başlangıç: 1 yolcu + 1 yük vagonu, vagon başına 3 kapasite.
- Yük **görünür**: kömür kara yığın, gıda saman balyası; yolcular gece pencerelerde ışık olarak yanar.
- Tek seferde 6+ birim teslimat = **KOMBO ×1.25**. Kapasite yükseltmesinin asıl ödülü budur.

### 4.4 Yakıt
- Mesafeyle azalır. Buharlı lokomotifle 100 yakıt ≈ 38 km.
- Biterse tren durur ve sefer başarısız olur; o ana kadar kazanılan para korunur (cezalandırmayan başarısızlık).
- Yakıt %40'ın altındaysa üretici, yakın bir yakıt peronu yerleştirir (görünmez merhamet).

### 4.5 Tehlikeler
| Tehlike | Kaçınma | Gece |
|---|---|---|
| Kırık ray | Şerit değiştir | Kırmızı flaşör yanar, uzaktan görünür |
| Kaya | Şerit değiştir | Yalnızca far ışığında görünür |
| İnek | Şerit değiştir **ya da düdük** | Yalnızca far ışığında görünür |
| Karşıdan gelen tren | Şerit değiştir (ekran üstünde yanıp sönen **TREN** uyarısı + sesli uyarı) | Kendi farı yanar |

Çarpma: 2 yük kaybı, −8 yakıt, kısa süreli yavaşlama, ekran sarsıntısı. Oyun bitmez. Karşı trenle çarpışma daha ağırdır: 4 yük kaybı, −18 yakıt, daha uzun yavaşlama.

### 4.6 Gece ve gündüz
- Gün döngüsü 2 dakikadır, yani her sefer en az bir gün dönümü görür.
- **Gece:** yolcu ücreti ×1.5, peronlar ve şehir pencereleri ışıl ışıl, ama tehlikeler karanlıkta. **Güçlü Far** geliştirmesi gece görüş mesafesini artırır.
- Görsel olarak: alacakaranlıkta turuncu, gece mavi ton; lambalar, pencereler ve far gerçek ışık maskesiyle açılır.

### 4.7 Sözleşmeler ve yıldızlar
Her seferde 2 sözleşme (örnek: "14 yolcu taşı", "250 altın kazan", "Gece 80 altın", "Kazasız varış").
- ★ Varış
- ★ Sözleşme 1
- ★ Sözleşme 2

Yıldızlar: şehir seviyesini, hat gelirini ve yeni lokomotiflerin kilidini belirler.

### 4.8 Şehir büyümesi (meta hedef)
`seviye = taban + varış hattındaki en iyi yıldız + (çıkış hattı tamamlandıysa 1)`, en fazla 5.
Seviye arttığında sonuç ekranında **önce/sonra silüeti** gösterilir. Varış garına girerken görülen şehir de bu seviyeye göre daha yüksek binalarla üretilir.

### 4.9 Garaj
| Geliştirme | Etki | Seviye |
|---|---|---|
| Motor Gücü | +%8 hız | 5 |
| Yakıt Deposu | +25 yakıt | 5 |
| Geniş Vagon | Her vagona +1 kapasite | 4 |
| Yeni Vagon | Yük, yolcu, yük sırasıyla | 3 |
| Güçlü Far | Gece görüşü | 3 |

| Lokomotif | Koşul | Hız | Yakıt | Hat geliri |
|---|---|---|---|---|
| Kara Duman (buharlı) | Başlangıç | ×1.0 | ×1.0 | ×1 |
| Turuncu Kaplan (dizel) | 6★ + 1.500 altın | ×1.15 | ×0.8 | ×1.5 |
| Mavi Şimşek (elektrikli) | 12★ + 4.000 altın | ×1.3 | ×0.6 | ×2 |

Lokomotif değişimi **görsel bir ödüldür**: buhar dumanı yerine dizel egzozu, elektrikli lokomotifte pantograf kıvılcımı.

### 4.10 Turbo
Yukarı itince 2,6 saniye boyunca tren %60 hızlanır. Bu sürede toplanan altınlar ×2, peron teslimatları ×1.25 değerindedir, ama yakıt iki kat hızlı biter. Risk: hızlıyken makas kararları için daha az zaman kalır.

### 4.11 Seri çarpanı
Kazasız art arda başarılı peron (yük alma, teslimat, yakıt) seriyi büyütür. Her adım +%5, en fazla ×1.5. Çarpan altınlara ve teslimatlara uygulanır; herhangi bir kaza seriyi sıfırlar. 5. ve 10. adımda ekranda "SERİ" bildirimi çıkar.

### 4.12 Tüneller ve hava
- **Tünel** (orman, dağ, kar hatları): Tren dağın altına girer ve görünmez. Tünelin içinde makas ve peron yoktur; tünelin hemen ardından gelen makası önceden düşünmek gerekir.
- **Yağmur** (ova, orman, nehir, sahil): Bazı seferler yağmurlu başlar ve sahne mavimsi bir ton alır.

---

## 5. Hat haritası (dünya)

| # | Hat | Biyom | Uzunluk | Kimlik |
|---|---|---|---|---|
| 1 | Gölbaşı → Çamlıca | Ova | 42 km | Buğday tarlaları, yel değirmeni, inekler |
| 2 | Çamlıca → Taşköprü | Orman | 50 km | Yoğun çam, kütük yığınları |
| 3 | Taşköprü → Kayalar | Nehir | 56 km | Sık köprüler, nehir kasabaları |
| 4 | Kayalar → Mavikent | Dağ | 62 km | Kaya, maden, fabrika ekonomisi |
| 5 | Mavikent → Karlıdağ | Sahil | 68 km | Deniz, fener, tekneler |
| 6 | Karlıdağ → Başkent | Kar | 74 km | Kar yağışı, uzun gece |

Her sefer prosedürel üretilir: kır, kasaba, şehir ve nehir bölgeleri sırayla gelir. Sonra varış şehri yükselir ve gar kanopisi görünür.

---

## 6. Görsel stil rehberi

- **Çözünürlük:** 160 px genişlik, telefona göre 270–356 px yükseklik; tamsayı-piksel ölçekleme.
- **Palet:** Endesga 32 (32 renk). Tüm sprite'lar ve zemin bu paletten.
- **Perspektif:** 3/4 yukarıdan bakış. Çatılar ve güneye bakan cepheler görünür, gölgeler sağ-alta düşer.
- **Tren:** 12 px genişlik; vagonlar rayın eğrisini satır satır takip eder (makasta tren kıvrılır).
- **Işık:** Gece katmanı + "ışık deliği" maskesi (far konisi, lamba, pencere). Pikselli, kademeli alfa.
- **Animasyon:** Buhar puf'ları, dönen değirmen, sallanan tekneler, yanıp sönen kırık ray flaşörü, kar yağışı, bulut gölgeleri.
- **Arayüz:** Pixelify Sans; koyu lacivert paneller, altın vurgu. Sahne içi etiketler 3×5 bitmap yazı.

---

## 7. Mobil iş modeli (öneri)

- Ücretsiz + reklam: "Hat gelirini 2× topla" ödüllü reklamı; sefer sonu "yükünü kurtar" (çarpışmada kaybı geri al).
- Kozmetik: lokomotif boyaları, vagon desenleri, şehir temaları (bayram ışıkları vb.).
- Sezonluk hatlar: yeni biyom + sınırlı süreli lokomotif.
- Kaçınılacak: enerji/bekleme duvarı. Idle zaten doğal bir "geri gel" sebebi.

---

## 8. Teknik yol haritası

### Faz 0: Prototip (bu repo) ✅
HTML5 Canvas, tek dosya. Amaç: görsel dil, makas mekaniği ve ekonomi hissini test etmek.

### Faz 1: Dikey dilim (Unity 6, C#), devam ediyor
**Unity 6 LTS (6000.3.27f1), 2D URP, dikey ekran.** Ayrıntılar: [`docs/UNITY.md`](UNITY.md)

- ✅ Unity proje iskeleti (`unity/PixTrain`): Unity'nin Universal 2D şablonu, editörle gelen paket sürümleri, dikey ekran.
- ✅ Kurallar katmanı (`PixTrain.Core`, Unity'den bağımsız): prototipteki bütün kurallar C#'a taşındı.
- ✅ 57 NUnit testi; `dotnet test dotnet/PixTrain.sln` ile Unity olmadan, Unity Test Runner'da da çalışır.
- ⬜ Görsel katman (tren, ray, ışık, arayüz, kaydırma, ses).
- ⬜ GitHub Actions + GameCI: WebGL ve Android derlemesi.

Katmanlı (N-tier) yapı:

```
PixTrain.Core.Data          GameBalance, GameCatalog (şehir, hat, biyom, peron, geliştirme, lokomotif, sözleşme)
PixTrain.Core.Repositories  IProgressRepository  ← kayıt (bellek / PlayerPrefs-JSON / bulut)
PixTrain.Core.Services      IRunService, IWorldGenService, ICargoService, IEconomyService,
                            IGarageService, IContractService
PixTrain.Presentation       MonoBehaviour'lar: TrainView, TrackRenderer, LightingOverlay, HUD, Garage UI (sıradaki adım)
```

### Faz 2: Backend (opsiyonel, canlı operasyon)
- **ASP.NET Core Web API:** `IPlayerRepository → IPlayerService → PlayerController` (bulut kayıt), `ILeaderboardRepository → ILeaderboardService → LeaderboardController` (haftalık "en kazançlı sefer"), `IEconomyConfigService` (fiyat ve sözleşme tablolarını sunucudan ayarlama).
- **Angular 18 yönetim paneli:** ekonomi tablolarını düzenleme, sezonluk hat yayınlama, oyuncu metrikleri.

---

## 9. Sonraki denemeler (prototipte yok)

- **Sinyal:** Tek hatlı kesimde sinyal; yanlış şeritte kalırsan kırmızı ışıkta beklersin.
- **Yolcu varış noktaları:** "Mavikent yolcusu" gibi hedefli yolcu, daha yüksek ücret.
- **Hava durumu oynanışı:** Yağmurda fren mesafesi, siste görüş kısıtı.
- **Hat sahipliği:** Ara istasyonları satın alıp geliştirmek (idle çarpanı).
- **Günlük görev ve sefer serisi:** Art arda 3 yıldız bonusu.
