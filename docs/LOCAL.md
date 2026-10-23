# Mac'te yerel çalışma

## 1. Repoyu çek

```bash
git clone https://github.com/fareed96/PixTrain.git
cd PixTrain
git checkout claude/brave-goldberg-edu5sd   # şu an varsayılan dal
```

## 2. Unity'yi kur

1. **Unity Hub**'ı aç (lisans için zaten kurdun).
2. **Installs → Install Editor → Archive** sekmesinden ya da https://unity.com/releases/editor/archive adresinden **6000.3.27f1** sürümünü seç.
   Proje bu sürüme sabitlenmiştir (`unity/PixTrain/ProjectSettings/ProjectVersion.txt`); farklı sürümle açarsan Unity yükseltme ister.
3. Modüller:
   - **WebGL Build Support** (web sürümü için, gerekli)
   - **Android Build Support** + OpenJDK + Android SDK & NDK (APK için, isteğe bağlı)
   - **Mac Build Support (Mono)** zaten gelir
4. Hub → **Projects → Add → Add project from disk** → `PixTrain/unity/PixTrain` klasörünü seç.

İlk açılış birkaç dakika sürer (Library klasörü oluşturulur, Git'e girmez).

## 3. Oyunu çalıştır

- `Assets/_Project/Scenes/Main.unity` sahnesini aç ve **Play**'e bas. Sahne boştur; oyun koddan kurulur (`GameBootstrap`).
- Game penceresinde dikey bir çözünürlük seç (ör. 1080×1920 ya da "iPhone 15"). Fare sürükleme parmak kaydırma gibi çalışır; klavye: ←/→ makas, ↓/boşluk düdük, ↑ turbo, Esc mola.
- Editörde reklamlar **test reklamı** olarak çalışır (2,5 saniyelik "TEST REKLAMI" ekranı, sonra ödül).

## 4. Testler

- Unity içinde: **Window → General → Test Runner → EditMode → Run All**.
- Unity olmadan (terminal, .NET 8 SDK gerekir: `brew install --cask dotnet-sdk`):
  ```bash
  dotnet test dotnet/PixTrain.sln
  dotnet run -c Release --project dotnet/PixTrain.Snapshots -- kareler   # oyun karelerini PNG üretir
  ```

## 5. Web sürümünü yerelde dene

**File → Build Profiles → Web → Switch Platform → Build** (çıktı klasörünü ör. `build/WebGL` seç). Sonra:

```bash
cd build/WebGL && python3 -m http.server 8080
```

Tarayıcıda http://localhost:8080 aç. Sayfa şablonu `Assets/WebGLTemplates/PixTrain` (Player Settings'te seçili). Reklam ayarı `ads-config.js` içinde; varsayılanı `off` (reklam yok). Sahte reklamlarla denemek için http://localhost:8080/?ads=test aç. Ayrıntılar: [`docs/ADS.md`](ADS.md).

Tarayıcı duman testini yerelde çalıştırmak için (Node 22 gerekir):

```bash
cd tools/webgl-smoke && npm install && npx playwright install chromium
node smoke.js "http://localhost:8080/?ads=test" ../../smoke
```

## 6. Kod düzeni

Ayrıntılar: [`docs/UNITY.md`](UNITY.md). Kısaca:

| Klasör | Ne var | Unity'ye bağımlı mı |
|---|---|---|
| `Scripts/Core` | Kurallar: Repository + Service katmanı | Hayır |
| `Scripts/Render` | Piksel çizici | Hayır |
| `Scripts/Game` | Unity: ekranlar, girdi, ses, reklam | Evet |
| `Tests/EditMode` | NUnit testleri | Hayır |

Editör dışında dosya eklersen `python3 tools/unity_meta.py` ile `.meta` üret (editörde eklersen Unity kendisi üretir).

## 7. Derleme hattı

Her push'ta GitHub Actions testleri çalıştırır, WebGL ve Android derler: https://github.com/fareed96/PixTrain/actions
Çıktılar her çalıştırmanın **Artifacts** bölümündedir (`PixTrain-WebGL`, `PixTrain-Android`).
