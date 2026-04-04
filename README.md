# 📄 Optik Değerlendirme Sistemi

<p align="center">
  <img src="Assets/app_logo.png" alt="Optik Değerlendirme Logo" width="120" height="120">
</p>

<p align="center">
  <strong>Modern, Hızlı ve Güvenli Optik Sınav Değerlendirme Platformu</strong>
</p>

<p align="center">
  <a href="https://github.com/eekilinc/optikdegerlendirme/releases">
    <img src="https://img.shields.io/github/v/release/eekilinc/optikdegerlendirme?style=for-the-badge&color=blue" alt="Latest Release">
  </a>
  <a href="https://github.com/eekilinc/optikdegerlendirme/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/eekilinc/optikdegerlendirme?style=for-the-badge&color=green" alt="License">
  </a>
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 9.0">
  <img src="https://img.shields.io/badge/WPF-Application-0078D4?style=for-the-badge&logo=windows" alt="WPF">
</p>

<p align="center">
  <a href="#-özellikler">Özellikler</a> •
  <a href="#-kurulum">Kurulum</a> •
  <a href="#-ekran-görüntüleri">Ekran Görüntüleri</a> •
  <a href="#-kullanım">Kullanım</a> •
  <a href="#-mimari">Mimari</a> •
  <a href="#-katkıda-bulunma">Katkıda Bulunma</a>
</p>

---

## 🎯 Proje Hakkında

**Optik Değerlendirme**, AĞLASUN MYO için geliştirilmiş, optik form okuma ve sınav değerlendirme sistemidir. WPF tabanlı modern arayüzü, SQLite veritabanı ve .NET 9.0 teknolojileriyle kurumsal düzeyde performans sunar.

> 🏆 **5000+ öğrenci kapasitesi** ile sınıf düzeyinde değerlendirmelerden, kurumsal ölçekli sınavlara kadar her ölçekte kullanıma uygundur.

---

## ✨ Özellikler

### � Optik Form İşleme
- ✅ **TXT Optik Form Okuma** - Standart optik form formatlarından otomatik veri çekme
- ✅ **Akıllı Parse Etme** - Hatalı verileri algılama ve düzeltme önerileri
- ✅ **Toplu İşleme** - Binlerce öğrencinin formlarını saniyeler içinde işleme

### 📝 Sınav Değerlendirme
- ✅ **Otomatik Net Hesaplama** - Doğru/yanlış/net hesaplaması
- ✅ **Başarı Tahmini** - ML tabanlı öğrenci başarı tahminleri
- ✅ **Soru Analizi** - Zorluk derecesi, ayırıcılık indeksi, güvenirlik analizi
- ✅ **Öğrenci Sıralaması** - Çoklu sıralama kriterleri ile detaylı liste

### 📈 Raporlama & Analiz
- ✅ **Excel Dışa Aktarım** - Analiz sonuçlarını Excel formatında kaydetme
- ✅ **CSV Dışa Aktarım** - Verileri CSV formatında dışa aktarma
- ✅ **PDF Raporlar** - Profesyonel PDF sınav raporları oluşturma
- ✅ **Grafik ve İstatistikler** - Soru bazlı başarı grafikleri

### 🎨 Kullanıcı Deneyimi
- ✅ **Modern WPF Arayüz** - Göz yormayan, sezgisel kullanım
- ✅ **Koyu/Açık Tema** - Kişiselleştirilebilir tema seçenekleri
- ✅ **Klavye Kısayolları** - Verimli iş akışı için kısayollar
- ✅ **Otomatik Güncelleme Kontrolü** - GitHub releases entegrasyonu

### 🗄️ Veri Yönetimi
- ✅ **SQLite Veritabanı** - Taşınabilir, sunucusuz veri depolama
- ✅ **Şablon Yönetimi** - Sınav şablonlarını kaydetme ve tekrar kullanma
- ✅ **Otomatik Yedekleme** - Güvenli veri saklama
- ✅ **Ders ve Sınav Yönetimi** - Organize veri yapısı

---

## 📸 Ekran Görüntüleri

> 📷 *Ekran görüntüleri yakında eklenecek*

---

## 🚀 Kurulum

### Sistem Gereksinimleri

| Bileşen | Minimum | Önerilen |
|---------|---------|----------|
| **İşletim Sistemi** | Windows 10 (64-bit) | Windows 11 |
| **.NET Runtime** | .NET 9.0 | .NET 9.0 |
| **RAM** | 4 GB | 8 GB |
| **Disk Alanı** | 500 MB | 1 GB |
| **Ekran** | 1366x768 | 1920x1080 |

### 📥 Hızlı Kurulum

#### 1. Kurulum Dosyası (Önerilen)

GitHub Releases sayfasından en son sürümü indirin ve kurulum sihirbazını takip edin.

#### 2. Taşınabilir Sürüm

Portable ZIP dosyasını indirin, çıkarın ve `OptikFormApp.exe` dosyasını çalıştırın.

### 🛠️ Geliştirici Kurulumu

```bash
# 1. Repo klonlayın
git clone https://github.com/eekilinc/optikdegerlendirme.git
cd optikdegerlendirme

# 2. Bağımlılıkları yükleyin
dotnet restore

# 3. Derleyin ve çalıştırın
dotnet build -c Release
dotnet run
```

---

## 📖 Kullanım

### 🎯 İlk Kullanım Rehberi

1. **🏫 Ders Ekleme** - "Ders Ekle" butonuna tıklayın, ders bilgilerini girin
2. **📝 Sınav Oluşturma** - Yeni sınav adı ve cevap anahtarını tanımlayın
3. **📄 Optik Form Yükleme** - TXT dosyasını sürükle-bırak yapın (Ctrl+O)
4. **📊 Sonuçları Görüntüleme** - Değerlendirme otomatik yapılır, dışa aktarabilirsiniz

### ⌨️ Klavye Kısayolları

| Kısayol | İşlem |
|---------|-------|
| `Ctrl + O` | Dosya Aç |
| `Ctrl + S` | Sınav Kaydet |
| `Ctrl + E` | Excel'e Aktar |
| `Ctrl + Shift + C` | CSV'ye Aktar |
| `Ctrl + P` | PDF Rapor |
| `F5` | Değerlendir |
| `F1` | Yardım |

---

## 🏗️ Mimari

### Üç Katmanlı Mimari

```
┌─────────────────────────────────────────────────────────┐
│                    🎨 PRESENTATION                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │
│  │   Views     │  │ ViewModels  │  │   Converters    │ │
│  │  (XAML)     │  │  (MVVM)     │  │  (Data Binding) │ │
│  └─────────────┘  └─────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────┤
│                   ⚙️ BUSINESS LOGIC                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │
│  │  Services   │  │ Validation  │  │     Parsing     │ │
│  │   (API)     │  │   Rules     │  │  (TXT Parser)   │ │
│  └─────────────┘  └─────────────┘  └─────────────────┘ │
├─────────────────────────────────────────────────────────┤
│                    💾 DATA ACCESS                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │
│  │   SQLite    │  │    JSON     │  │   File System   │ │
│  │ (Database)  │  │ (Settings)  │  │  (Temp/Cache)   │ │
│  └─────────────┘  └─────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 🛠️ Teknoloji Yığını

- **.NET 9.0 WPF** - Modern Windows uygulama geliştirme
- **SQLite** - Sunucusuz veritabanı
- **ClosedXML** - Excel işlemleri
- **QuestPDF** - PDF raporlama
- **ScottPlot** - Veri görselleştirme
- **Serilog** - Loglama
- **GitHub Actions** - CI/CD

---

## 🤝 Katkıda Bulunma

1. **🔀 Fork** oluşturun
2. **🌿 Feature branch** açın (`git checkout -b feature/yeni-ozellik`)
3. **✅ Değişiklikleri** commit edin (`git commit -m 'Yeni özellik: ...'`)
4. **📤 Push** edin (`git push origin feature/yeni-ozellik`)
5. **📝 Pull Request** açın

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## 👨‍💻 Geliştirici

**Ekrem KILINÇ** - AĞLASUN MYO

---

<p align="center">
  <strong>⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!</strong>
</p>

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
