# PixTrain

Dikey ekranda oynanan, pixel art şehirlerarası tren oyunu. Tren kendi kendine gider; sen makasları çevirerek hangi perona gireceğine, ne taşıyacağına ve nerede satacağına karar verirsin. Gece ve gündüz geçer; kasabalar ve şehirler, onlara getirdiğin seferlerle büyür.

- **Tasarım dokümanı:** [`docs/GDD.md`](docs/GDD.md)
- **Oynanabilir prototip:** [`prototype/index.html`](prototype/index.html)

## Prototipi çalıştırma

Kurulum gerekmez. Dosyayı tarayıcıda aç ya da telefondan denemek için yerel ağda sun:

```bash
npx serve prototype      # sonra telefondan http://<bilgisayar-ip>:3000
```

| Kontrol | Mobil | Masaüstü |
|---|---|---|
| Makası sola / sağa çevir | Ekranın sol / sağ yarısına dokun | ← → veya A / D |
| Düdük (inekleri kaçır) | DÜDÜK düğmesi | Boşluk |
| Mola | II | Esc |

İlerleme tarayıcının `localStorage`'ında tutulur (`pixtrain.v1`).

## Prototipte neler var

- 6 hat, 7 şehir; her hat ayrı biyom (ova, orman, nehir, dağ, sahil, kar), prosedürel üretim
- Üç şeritli hat, çift makaslı geçişler, vagonların rayın eğrisini takip etmesi
- Yolcu, kömür ve gıda; kasaba, maden, çiftlik, şehir, fabrika ve yakıt peronları
- Kırık ray, kaya ve inek tehlikeleri; düdük
- 2 dakikalık gün döngüsü, gece ışık maskesi, far konisi, yanan pencereler
- Sözleşmeler, 3 yıldız, şehir büyümesi (Köy → Metropol)
- Garaj: 5 geliştirme, 3 lokomotif (buharlı, dizel, elektrikli)
- Idle hat geliri (en fazla 4 saat birikir)

Tüm grafikler kodla üretilir (Endesga 32 paleti); harici görsel dosyası yoktur.
