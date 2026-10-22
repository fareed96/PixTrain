# PixTrain Unity projesi

Unity sürümü: **6000.3.27f1 (Unity 6 LTS)**, 2D URP, dikey ekran.
Proje klasörü: [`unity/PixTrain`](../unity/PixTrain)

## Katmanlar

Üç derleme birimi (asmdef) vardır. İlk ikisi Unity'ye bağımlı değildir; bu yüzden Unity olmadan derlenip test edilebilir ve ekran görüntüsü üretebilir.

```
unity/PixTrain/Assets/_Project/
├── Scripts/
│   ├── Core/      PixTrain.Core    Oyun kuralları (Unity'den bağımsız)
│   │   ├── Data/                   GameBalance (tüm sayısal ayarlar), GameCatalog, DefaultCatalog
│   │   ├── Models/                 PlayerProgress (kalıcı), RunState (sefer durumu), olaylar, sonuçlar
│   │   ├── Repositories/           IProgressRepository → InMemoryProgressRepository
│   │   ├── Services/               IRunService, IWorldGenService, ICargoService, IEconomyService,
│   │   │                           IGarageService, IContractService, DayCycle
│   │   └── GameServices.cs         Kompozisyon kökü
│   ├── Render/    PixTrain.Render  Piksel çizici (Unity'den bağımsız)
│   │   ├── PixelImage, Palette     Yazılım tuvali (RGBA), Endesga 32 paleti
│   │   ├── SpriteData/Library      Prototipten aktarılan sprite tabloları + kodla çizilenler
│   │   ├── Scenery                 Bölgelerden manzara (ağaç, ev, bina, lamba, araba, tekne)
│   │   ├── RunView                 Seferin görsel durumu: duman, kıvılcım, uçan yazılar, önbellek
│   │   ├── WorldRenderer           Sahneyi 160 px genişlikte çizer: zemin, ray, tren, tünel, hava, gece ışığı
│   │   └── UiArt                   Logo, şehir silüetleri, garaj önizlemesi, hareket okları
│   └── Game/      PixTrain.Game    Unity sunum katmanı
│       ├── GameBootstrap           Sahne boş; oyun koddan kurulur
│       ├── GameController          Ekranlar, kare döngüsü, olay → ses/yazı/duyuru
│       ├── GestureInput            Kaydırma: sol/sağ makas, aşağı düdük, yukarı turbo (Input System)
│       ├── SynthAudio              Sesler açılışta sentezlenir (ses dosyası yok)
│       ├── PlayerPrefsProgressRepository   Kayıt (JSON)
│       └── UI/                     HUD, başlık, hat haritası + garaj, mola, sonuç (uGUI, koddan)
├── Resources/Fonts/                Pixelify Sans (OFL), Türkçe karakterli piksel font
├── Scenes/Main.unity               Boş açılış sahnesi
└── Tests/EditMode/                 NUnit testleri (Unity Test Runner + dotnet test)
```

Dashboard örneğindeki `IDashboardRepository → IDashboardService → Controller` düzeninin oyundaki karşılığı:

| Katman | PixTrain'de |
|---|---|
| Repository | `IProgressRepository` → `PlayerPrefsProgressRepository` (ileride bulut kaydı) |
| Service | `IRunService`, `IEconomyService`, `IGarageService`… |
| Controller / UI | `GameController` + `UI/*View`: servisleri çağırır, `RunState.Events` olaylarını ses ve efekte çevirir |

### Bir karede ne olur

1. `GestureInput` hareketi okur → `Runs.Steer / Horn / Turbo`.
2. `Runs.Tick(run, dt)` kuralları ilerletir; olaylar `run.Events` listesine düşer.
3. `RunView` olaylardan uçan yazı ve kıvılcım üretir; `GameController` ses ve duyuru çalar.
4. `WorldRenderer` sahneyi 160×(270–356) piksellik tuvale çizer; tuval tek bir dokuya yüklenip ekranı dolduracak şekilde, piksel bozulmadan büyütülür.
5. HUD ve menüler uGUI ile, Pixelify Sans fontuyla çizilir.

Sahne çizimi neden yazılımla? Görünüm prototiple birebir aynı kalır (ışık maskesi, kıvrılan vagonlar, palet), çizim Unity olmadan test edilip görüntülenebilir ve kare süresi yaklaşık 1 ms'dir.

## Komutlar

```bash
# Kurallar ve çizim testleri (69 test), Unity gerekmez
dotnet test dotnet/PixTrain.sln

# Oyun karelerini PNG olarak üret (Unity gerekmez): biyomlar, gece, tünel, varış, logo
dotnet run -c Release --project dotnet/PixTrain.Snapshots -- cikti-klasoru

# Unity katmanını gerçek Unity API'lerine karşı derle (lisans gerekmez, ayıklanmış Unity editörü gerekir)
python3 tools/unity_compile_check.py /yol/Unity/Editor

# Editör açmadan eklenen dosyalar için .meta üret
python3 tools/unity_meta.py

# Prototipteki sprite tablolarını Unity'ye aktar
python3 tools/export_sprites.py
```

## Neler doğrulandı, neler doğrulanmadı

| Kontrol | Durum |
|---|---|
| Kurallar (Core) | 57 test |
| Çizim (Render) | 12 test + PNG kareler (prototiple görsel karşılaştırma) |
| Unity katmanı derleniyor mu | Evet: Unity 6000.3.27f1 DLL'leri, uGUI 2.0.0 ve Input System 1.20.1 kaynaklarına karşı 0 hata, 0 uyarı |
| Unity'de çalışıyor mu (arayüz yerleşimi, dokunmatik, ses) | Henüz görülmedi; Unity'yi çalıştırmak lisans gerektiriyor. Adım 4'te GameCI ile WebGL derlenince tarayıcıda kontrol edilecek |

## Proje ayarları

- Temel: Unity'nin resmi **Universal 2D** şablonu (URP 2D Renderer ayarları `Assets/Settings`).
- Ürün adı ve şirket: PixTrain; paket kimliği `com.pixtrain.game` (yayından önce değiştirilebilir).
- Ekran: yalnızca dikey. Girdi: yeni Input System.
- Paket sürümleri editörün kendi paketlerinden okundu (URP 17.3.0, Test Framework 1.6.0, Input System 1.20.1, uGUI 2.0.0).

## .meta dosyaları

Unity her dosya için bir `.meta` ister. GUID'ler dosya yolundan üretilir, yani her makinede aynıdır. Var olan `.meta` dosyalarına dokunulmaz.

## Sonraki adım

4. **GitHub Actions + GameCI:** her push'ta testler, WebGL ve Android derlemesi. Unity hesabı bilgileri GitHub repo secret'larına eklenmeli.
