# PixTrain Unity projesi

Unity sürümü: **6000.3.27f1 (Unity 6 LTS)**, 2D URP, dikey ekran.
Proje klasörü: [`unity/PixTrain`](../unity/PixTrain)

## Katmanlar

Oyun kuralları Unity'den tamamen bağımsızdır. Unity tarafı yalnızca görüntü, ses ve dokunmatik girdiyle ilgilenir.

```
unity/PixTrain/Assets/_Project/
├── Scripts/
│   └── Core/                         PixTrain.Core.asmdef (noEngineReferences: Unity'ye bağımlı değil)
│       ├── Data/                     Sabit içerik ve denge değerleri
│       │   ├── GameBalance.cs        Tüm sayısal ayarlar (hız, yakıt, fiyat, turbo, seri…)
│       │   ├── GameCatalog.cs        Şehir, hat, biyom, peron, geliştirme, lokomotif, sözleşme listeleri
│       │   └── DefaultCatalog.cs     Prototipteki içeriğin aynısı
│       ├── Models/                   PlayerProgress (kalıcı), RunState (sefer durumu), olaylar ve sonuçlar
│       ├── Repositories/             IProgressRepository  →  InMemoryProgressRepository
│       ├── Services/                 Arayüz + uygulama çiftleri
│       │   ├── IRunService           Sefer: hareket, makas, peron, kaza, turbo, düdük, varış
│       │   ├── IWorldGenService      Prosedürel hat: bölgeler, makaslar, peronlar, karşı trenler
│       │   ├── ICargoService         Vagon yükleme, teslimat, kombo, kayıp
│       │   ├── IEconomyService       Altın, şehir seviyesi, yıldız, hat geliri
│       │   ├── IGarageService        Geliştirme ve lokomotif satın alma, tren özellikleri
│       │   ├── IContractService      Sözleşme seçimi ve değerlendirme
│       │   └── DayCycle              Gece/gündüz hesapları
│       └── GameServices.cs           Kompozisyon kökü (servisleri bağımlılıklarıyla kurar)
└── Tests/EditMode/                   NUnit testleri (Unity Test Runner + dotnet test)
```

Dashboard örneğindeki `IDashboardRepository → IDashboardService → Controller` düzeninin oyundaki karşılığı:

| Katman | PixTrain'de |
|---|---|
| Repository | `IProgressRepository` (Unity'de PlayerPrefs/JSON, ileride bulut kaydı) |
| Service | `IRunService`, `IEconomyService`, `IGarageService`… |
| Controller / UI | Unity MonoBehaviour'ları (adım 3): servisleri çağırır, `RunState.Events` olaylarını ses ve efekte çevirir |

Akış: Unity her karede `Runs.Tick(run, dt)` çağırır, `run` durumunu çizer, `run.Events` listesindeki olayları (altın, kaza, gece çöktü…) ses ve yazıya çevirir. Kaydırma hareketleri `Runs.Steer`, `Runs.Horn`, `Runs.Turbo` çağrılarına karşılık gelir.

## Testler

Kurallar katmanının testleri Unity olmadan çalışır:

```bash
dotnet test dotnet/PixTrain.sln
```

- `dotnet/PixTrain.Core` Unity'deki `Scripts/Core` dosyalarını Unity ile aynı hedefle derler (.NET Standard 2.1, C# 9). Unity'de derlenmeyecek bir dil özelliği burada da hata verir.
- `dotnet/PixTrain.Core.Tests` aynı test dosyalarını (`Tests/EditMode`) NUnit ile çalıştırır. Unity Test Runner'da da aynı testler koşar.
- Rastgele sayı üreteci ve hash fonksiyonunun prototiple birebir aynı sonuç verdiği testle doğrulanır.

## Proje ayarları

- Temel: Unity'nin resmi **Universal 2D** şablonu (URP 2D Renderer ayarları `Assets/Settings`).
- Ürün adı ve şirket: PixTrain; paket kimliği `com.pixtrain.game` (yayından önce değiştirilebilir).
- Ekran: yalnızca dikey (portrait).
- Paket sürümleri editörün kendi paketlerinden okundu (URP 17.3.0, Test Framework 1.6.0, Input System 1.20.1, uGUI 2.0.0).

## .meta dosyaları

Unity her dosya için bir `.meta` ister. Editör açılmadan eklenen dosyalar için:

```bash
python3 tools/unity_meta.py
```

GUID'ler dosya yolundan üretilir, yani her makinede aynıdır. Var olan `.meta` dosyalarına dokunulmaz.

## Sonraki adımlar

3. **Unity görsel katmanı:** tren, ray ve zemin çizimi, gece ışığı, arayüz, kaydırma kontrolleri, sesler (`Scripts/Presentation`).
4. **GitHub Actions + GameCI:** her push'ta testler, WebGL ve Android derlemesi. Unity hesabı bilgileri GitHub repo secret'larına eklenmeli.
