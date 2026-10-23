# PixTrain

Dikey ekranda oynanan, pixel art şehirlerarası tren oyunu. Tren kendi kendine gider; sen makasları çevirerek hangi perona gireceğine, ne taşıyacağına ve nerede satacağına karar verirsin. Gece ve gündüz geçer; kasabalar ve şehirler, onlara getirdiğin seferlerle büyür.

- **Tasarım dokümanı:** [`docs/GDD.md`](docs/GDD.md)
- **Oynanabilir prototip:** [`prototype/index.html`](prototype/index.html)
- **Unity projesi (Unity 6 LTS, C#):** [`unity/PixTrain`](unity/PixTrain); yapı ve komutlar [`docs/UNITY.md`](docs/UNITY.md)
- **Mac'te yerel çalışma:** [`docs/LOCAL.md`](docs/LOCAL.md)
- **Web'de yayın ve reklamlar (AdSense, ödüllü reklam, alan adı):** [`docs/ADS.md`](docs/ADS.md)

## Repo yapısı

| Klasör | İçerik |
|---|---|
| `prototype/` | Tek dosyalık HTML5 prototip (tarayıcıda oynanır) |
| `unity/PixTrain/` | Unity projesi: `Core` (kurallar), `Render` (piksel çizici), `Game` (Unity arayüzü, girdi, ses, reklam), `WebGLTemplates/PixTrain` (yayın sayfası) |
| `dotnet/` | Core ve Render'ı Unity olmadan derleyen, test eden ve ekran görüntüsü üreten .NET projeleri |
| `tools/` | Yardımcı betikler: `.meta` üretimi, sprite aktarımı, Unity'ye karşı derleme kontrolü, WebGL tarayıcı duman testi |
| `docs/` | Tasarım dokümanı, Unity notları, yerel kurulum, yayın ve reklam rehberi |

Testler ve Unity olmadan oyun kareleri (.NET 8 SDK gerekir):

```bash
dotnet test dotnet/PixTrain.sln
dotnet run -c Release --project dotnet/PixTrain.Snapshots -- cikti-klasoru
```

## Prototipi çalıştırma

Kurulum gerekmez. Dosyayı tarayıcıda aç ya da telefondan denemek için yerel ağda sun:

```bash
npx serve prototype      # sonra telefondan http://<bilgisayar-ip>:3000
```

| Kontrol | Mobil (kaydırma) | Masaüstü |
|---|---|---|
| Makası sola / sağa çevir | Parmağı sola / sağa kaydır | ← → veya A / D |
| Düdük "düt düt" (inekleri kaçır) | Parmağı aşağı çek | ↓, S veya Boşluk |
| Turbo | Parmağı yukarı it | ↑ veya W |
| Mola | II | Esc |

İlerleme tarayıcının `localStorage`'ında tutulur (`pixtrain.v1`).

## Prototipte neler var

- 6 hat, 7 şehir; her hat ayrı biyom (ova, orman, nehir, dağ, sahil, kar), prosedürel üretim
- Üç şeritli hat, çift makaslı geçişler, vagonların rayın eğrisini takip etmesi
- Yolcu, kömür ve gıda; kasaba, maden, çiftlik, şehir, fabrika ve yakıt peronları
- Kırık ray, kaya, inek ve karşıdan gelen tren tehlikeleri; düdük
- Turbo (altın ×2, yakıt ×2) ve kazasız seri çarpanı (×1.5'e kadar)
- Dağ altından geçen tüneller, yağmurlu seferler
- 2 dakikalık gün döngüsü, gece ışık maskesi, far konisi, yanan pencereler
- Sözleşmeler, 3 yıldız, şehir büyümesi (Köy → Metropol)
- Garaj: 5 geliştirme, 3 lokomotif (buharlı, dizel, elektrikli)
- Idle hat geliri (en fazla 4 saat birikir)

Tüm grafikler kodla üretilir (Endesga 32 paleti); harici görsel dosyası yoktur.
