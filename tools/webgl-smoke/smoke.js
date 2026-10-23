// PixTrain WebGL duman testi: derlenmiş oyunu başsız Chromium'da telefon boyutunda açar ve temel akışı dener.
//   başlık -> (OYNA'ya dokun) hat -> garaj -> sefer -> kaydırma hareketleri -> yakıt biter -> test reklamıyla devam
// Kullanım: node smoke.js <url> <çıktı-klasörü>     (url ör. http://localhost:8080/?ads=test)
// Ekran görüntüleri çıktı klasörüne yazılır; --print-shots ile ayrıca base64 olarak log'a basılır.
// Bir adım başarısız olursa ya da konsolda hata görülürse çıkış kodu 1 olur.
const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright");

const url = process.argv[2] || "http://localhost:8080/?ads=test";
const out = process.argv[3] || "smoke";
const printShots = process.argv.includes("--print-shots");
fs.mkdirSync(out, { recursive: true });

const logs = [];
const failures = [];
const shots = [];

function check(ok, what) {
  console.log((ok ? "  ok   " : "  HATA ") + what);
  if (!ok) failures.push(what);
}

(async () => {
  const browser = await chromium.launch({
    args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--autoplay-policy=no-user-gesture-required"],
  });
  const page = await browser.newPage({ viewport: { width: 360, height: 740 }, deviceScaleFactor: 1, hasTouch: false });
  page.on("console", (m) => logs.push({ type: m.type(), text: m.text() }));
  page.on("pageerror", (e) => logs.push({ type: "pageerror", text: e.message }));
  page.on("requestfailed", (r) => logs.push({ type: "requestfailed", text: r.url() + " " + (r.failure() || {}).errorText }));

  const wait = (ms) => page.waitForTimeout(ms);
  const shot = async (name) => {
    const file = path.join(out, name + ".jpg");
    await page.screenshot({ path: file, type: "jpeg", quality: 80 });
    shots.push(file);
  };

  // Oyunun piksel dünyasını (160 genişlik, 270-356 yükseklik) sayfa koordinatına çevirir (GameController.Layout ile aynı hesap).
  async function worldToPage(wx, wyFromBottom) {
    const b = await page.locator("#unity-canvas").boundingBox();
    let u = b.width / 160;
    let h = Math.round(b.height / u);
    if (h < 270) { h = 270; u = b.height / h; }
    if (h > 356) { h = 356; u = Math.min(b.width / 160, b.height / h); }
    const cx = b.x + b.width / 2, cy = b.y + b.height / 2;
    return [cx - 80 * u + wx * u, cy + (h * u) / 2 - wyFromBottom * u];
  }
  async function tapWorld(wx, wy) {
    const [x, y] = await worldToPage(wx, wy);
    await page.mouse.click(x, y);
  }
  async function swipe(dx, dy) {
    const b = await page.locator("#unity-canvas").boundingBox();
    const x0 = b.x + b.width / 2, y0 = b.y + b.height * 0.6;
    await page.mouse.move(x0, y0);
    await page.mouse.down();
    for (let i = 1; i <= 8; i++) { await page.mouse.move(x0 + (dx * i) / 8, y0 + (dy * i) / 8); await wait(16); }
    await page.mouse.up();
    await wait(250);
  }

  async function automation(cmd) {
    await page.evaluate((c) => window.pixtrainUnity.SendMessage("PixTrain", "Automation", c), cmd);
    await wait(300);
  }
  // Oyunun son yazdığı durum satırı: "[PixTrain] durum mode=Map ads=WebAdProvider ..."
  async function state() {
    const before = logs.length;
    await automation("state");
    for (let i = 0; i < 20; i++) {
      for (let k = logs.length - 1; k >= before; k--) if (logs[k].text.includes("[PixTrain] durum")) return logs[k].text;
      await wait(100);
    }
    return "";
  }
  async function waitState(re, ms) {
    const until = Date.now() + ms;
    let s = "";
    while (Date.now() < until) { s = await state(); if (re.test(s)) return s; await wait(500); }
    return s;
  }
  const bannerOn = () => page.evaluate(() => document.body.classList.contains("banner-on"));

  try {
    console.log("Sayfa: " + url);
    const t0 = Date.now();
    await page.goto(url, { waitUntil: "load", timeout: 120000 });
    await page.waitForFunction(() => !!window.pixtrainUnity, null, { timeout: 240000, polling: 500 });
    console.log("Unity yüklendi: " + ((Date.now() - t0) / 1000).toFixed(1) + " sn");
    await wait(3000);

    let s = await state();
    console.log("  " + s);
    check(/mode=Title/.test(s), "başlık ekranı açıldı");
    check(/ads=WebAdProvider/.test(s), "sayfa reklam köprüsü bulundu (WebAdProvider)");
    check(await bannerOn(), "başlıkta banner açık (jslib -> sayfa)");
    check(await page.locator("#ad-bottom .ad-placeholder").count() === 1, "alt banner yer tutucusu var");
    await shot("01-baslik");

    // OYNA düğmesi: alttan 48 piksel, 70x16 (TitleView)
    await tapWorld(80, 56);
    s = await waitState(/mode=Map/, 5000);
    check(/mode=Map/.test(s), "OYNA'ya dokununca hat ekranı açıldı");
    await wait(800);
    await shot("02-hat");

    await automation("garage");
    await wait(800);
    await shot("03-garaj");

    await automation("run:0");
    s = await waitState(/mode=Run .*attract=False/, 5000);
    check(/mode=Run .*attract=False/.test(s), "sefer başladı");
    check(!(await bannerOn()), "seferde banner kapalı");
    await wait(2500);
    const y0 = parseFloat((s.match(/ y=(\d+)/) || [0, 0])[1]);
    await swipe(-90, 0);
    await wait(1200);
    await swipe(90, 0);
    await swipe(0, 120); // düdük
    await wait(1500);
    s = await state();
    console.log("  " + s);
    const y1 = parseFloat((s.match(/ y=(\d+)/) || [0, 0])[1]);
    check(y1 > y0 + 50, "tren ilerliyor (y " + y0 + " -> " + y1 + ")");
    await shot("04-sefer");

    await automation("empty");
    s = await waitState(/phase=Stalled/, 20000);
    check(/phase=Stalled/.test(s), "yakıt bitince tren durdu, devam teklifi geldi");
    await wait(600);
    await shot("05-devam-teklifi");

    await automation("continue");
    await wait(500);
    check(await page.locator("#test-ad.on").count() === 1, "test reklamı açıldı");
    await shot("06-test-reklami");
    await page.waitForFunction(() => !document.querySelector("#test-ad.on"), null, { timeout: 8000 });
    s = await waitState(/phase=Running .*continueUsed=True/, 5000);
    console.log("  " + s);
    check(/phase=Running .*continueUsed=True/.test(s), "reklam izlendi, sefer devam ediyor");
    await wait(1500);
    await shot("07-yola-devam");
  } catch (e) {
    failures.push("istisna: " + e.message);
    console.log("  HATA " + e.message);
    try { await shot("99-hata"); } catch (_) { /* sayfa kapanmış olabilir */ }
  }

  const errors = logs.filter((l) => l.type === "error" || l.type === "pageerror" || l.type === "requestfailed");
  check(errors.length === 0, "konsolda hata yok (" + errors.length + ")");
  fs.writeFileSync(path.join(out, "console.txt"), logs.map((l) => "[" + l.type + "] " + l.text).join("\n"));
  console.log("\n--- konsol (son 60 satır) ---");
  for (const l of logs.slice(-60)) console.log("[" + l.type + "] " + l.text);

  if (printShots) {
    console.log("\n--- ekran görüntüleri (base64) ---");
    for (const f of shots) {
      const b64 = fs.readFileSync(f).toString("base64");
      for (let i = 0; i < b64.length; i += 4000) console.log("SHOT " + path.basename(f) + " " + b64.slice(i, i + 4000));
    }
  }

  await browser.close();
  console.log(failures.length ? "\nDUMAN TESTİ BAŞARISIZ: " + failures.join("; ") : "\nDUMAN TESTİ GEÇTİ");
  process.exit(failures.length ? 1 : 0);
})();
