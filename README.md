# Optik Değerlendirme Sistemi

AĞLASUN MYO için geliştirilmiş, optik form okuma ve sınav değerlendirme sistemidir.

## 📋 Proje Özeti

**Optik Değerlendirme**, sınav optik formlarını otomatik okuyan, değerlendiren ve detaylı raporlar üreten bir WPF uygulamasıdır. SQLite veritabanı kullanır ve modern .NET 9.0 teknolojileriyle geliştirilmiştir.

### Özellikler

- ✅ Optik form okuma ve parse etme
- ✅ Otomatik sınav değerlendirme
- ✅ Excel, CSV ve PDF raporlama
- ✅ Soru analizi ve istatistikler
- ✅ Başarı tahmini (ML tabanlı)
- ✅ Şablon yönetimi
- ✅ Çoklu tema desteği
- ✅ 5000+ öğrenci kapasitesi

## 🚀 Kurulum

### Gereksinimler

- Windows 10/11 (64-bit)
- .NET 9.0 Runtime
- Minimum 4GB RAM
- 500MB disk alanı

### Kurulum Seçenekleri

#### 1. Kurulum Dosyası (Önerilen)

1. [GitHub Releases](https://github.com/eekilinc/optikdegerlendirme/releases) sayfasından `OptikDegerlendirme-vX.X.X-Setup.exe` indirin
2. Kurulum dosyasını çalıştırın
3. Sihirbazı takip edin

#### 2. Taşınabilir Sürüm

1. `OptikDegerlendirme-vX.X.X-portable.zip` indirin
2. Zip dosyasını çıkarın
3. `OptikFormApp.exe` çalıştırın

#### 3. Kaynak Koddan Derleme

```bash
# Repo klonlayın
git clone https://github.com/eekilinc/optikdegerlendirme.git
cd optikdegerlendirme

# Bağımlılıkları yükleyin
dotnet restore

# Derleyin
dotnet build -c Release

# Çalıştırın
dotnet run
```

## 📖 Kullanım

### İlk Kullanım

1. Uygulamayı başlatın
2. Ders ekleyin ("Ders Ekle" butonu)
3. Sınav oluşturun
4. TXT optik form dosyasını yükleyin (Sürükle-bırak veya "Dosya Aç")
5. Sonuçları görüntüleyin ve raporlayın

### Kısayollar

| Kısayol | İşlem |
|---------|-------|
| `Ctrl+O` | Dosya aç |
| `Ctrl+S` | Sınav kaydet |
| `Ctrl+E` | Excel'e aktar |
| `Ctrl+Shift+C` | CSV'ye aktar |
| `Ctrl+P` | PDF raporu oluştur |
| `F5` | Değerlendirme başlat |
| `F1` | Kısayol yardımı |

## 🏗️ Mimari

### Katmanlı Mimari

```
┌─────────────────────────────────────┐
│         Presentation (WPF)          │
│    Views, ViewModels, Converters     │
├─────────────────────────────────────┤
│         Business Logic              │
│    Services, Validation, Parsing   │
├─────────────────────────────────────┤
│         Data Access                 │
│    SQLite, JSON, File System       │
└─────────────────────────────────────┘
```

### Teknoloji Yığını

- **Framework**: .NET 9.0 WPF
- **Database**: SQLite (Microsoft.Data.Sqlite)
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Excel**: ClosedXML
- **PDF**: QuestPDF
- **Charts**: ScottPlot
- **Logging**: Serilog

## 📁 Proje Yapısı

```
optikdegerlendirme/
├── Assets/                 # Görseller ve ikonlar
├── Models/               # Veri modelleri
├── ViewModels/           # MVVM ViewModels
├── Views/                # WPF kontrolleri
├── Services/             # İş mantığı servisleri
├── installer/            # Inno Setup kurulum scripti
└── .github/workflows/    # GitHub Actions CI/CD
```

## 🔧 Geliştirme

### Gereksinimler

- Visual Studio 2022 veya VS Code
- .NET 9.0 SDK
- Inno Setup 6 (kurulum oluşturmak için)

### Debug

```bash
dotnet run --configuration Debug
```

### Test

```bash
dotnet test
```

### Release Oluşturma

```bash
# Sürüm oluştur
git tag v1.2.5
git push origin v1.2.5

# GitHub Actions otomatik build edecek
```

## 📝 Veri Formatı

### Optik Form TXT Formatı

```
TCKN|SINIF|AD SOYAD|OPTİK FORM VERİSİ
```

Örnek:
```
12345678901|11-A|Ahmet YILMAZ|ABCDABCD...
```

### Şablon JSON Formatı

```json
{
  "Id": "guid",
  "Name": "Şablon Adı",
  "AnswerKeys": [...],
  "QuestionSettings": [...],
  "NetCoefficient": 1.0,
  "WrongDeductionFactor": 0.25
}
```

## 🐛 Hata Ayıklama

### Log Konumu

```
%APPDATA%\OptikDegerlendirme\app_debug.log
```

### Sık Karşılaşılan Sorunlar

| Sorun | Çözüm |
|-------|-------|
| "Access denied" | Uygulamayı yönetici olarak çalıştırın |
| "Database locked" | Uygulamayı yeniden başlatın |
| "Invalid optical format" | TXT dosya formatını kontrol edin |

## 🤝 Katkıda Bulunma

1. Fork oluşturun
2. Feature branch açın (`git checkout -b feature/yeni-ozellik`)
3. Değişiklikleri commit edin (`git commit -m 'Yeni özellik: ...'`)
4. Push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request açın

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## 👨‍💻 Geliştirici

**Ekrem KILINÇ** - AĞLASUN MYO

---

**Not**: Bu uygulama AĞLASUN MYO sınav değerlendirme sistemi için özel olarak geliştirilmiştir.
