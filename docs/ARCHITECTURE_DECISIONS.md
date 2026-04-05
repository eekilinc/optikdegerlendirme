# Architecture Decision Records (ADRs)

Bu belge, Optik Değerlendirme Sistemi'nin temel mimari kararlarını ve gerekçelerini içerir.

---

## ADR-001: SQLite Veritabanı Seçimi

### Durum
**Kabul Edildi** - 2024

### Bağlam
Sistem için yerel bir veritabanına ihtiyaç vardı. Kurulum gerektirmeden çalışmalı, taşınabilir olmalı ve performanslı olmalıydı.

### Karar
SQLite veritabanı seçildi.

### Gerekçeler

| Kriter | SQLite | SQL Server | PostgreSQL |
|--------|--------|------------|------------|
| Kurulum Gereksinimi | ❌ Yok | ✅ Gerekli | ✅ Gerekli |
| Taşınabilirlik | ✅ Yüksek | ❌ Düşük | ❌ Düşük |
| Performans | ✅ İyi | ✅ İyi | ✅ İyi |
| Dosya Tabanlı | ✅ Evet | ❌ Hayır | ❌ Hayır |
| .NET Entegrasyonu | ✅ Mükemmel | ✅ Mükemmel | ✅ İyi |

### Sonuçlar
- ✅ Bağımsız çalışan uygulama
- ✅ Basit yedekleme (tek dosya kopyalama)
- ❌ Çok kullanıcılı ortamlarda sınırlamalar
- ❌ Gelişmiş raporlama için dışa aktarma gerekiyor

### Referanslar
- [Microsoft.Data.Sqlite](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [SQLite WAL Mode](https://www.sqlite.org/wal.html)

---

## ADR-002: WPF ve MVVM Mimarisi

### Durum
**Kabul Edildi** - 2024

### Bağlam
Masaüstü uygulaması için UI framework seçimi gerekiyordu. Windows odaklı geliştirme yapılacaktı.

### Karar
WPF (Windows Presentation Foundation) + MVVM (Model-View-ViewModel) deseni seçildi.

### Gerekçeler

#### Neden WPF?
- Windows entegrasyonu mükemmel
- XAML ile zengin UI tasarımı
- Data binding desteği güçlü
- Modern .NET 9.0 desteği

#### Neden MVVM?
- Test edilebilirlik artar
- UI ve iş mantığı ayrılır
- Bakım kolaylığı
- Kod tekrarı azalır

### Alternatifler Değerlendirildi

| Framework | Değerlendirme | Sonuç |
|-----------|---------------|-------|
| WinForms | Eski teknoloji, sınırlı özellikler | ❌ Reddedildi |
| WinUI 3 | Windows 10+ sınırlaması | ❌ Reddedildi |
| Avalonia | Cross-platform ama öğrenme eğrisi | ❌ Reddedildi |
| MAUI | Mobil odaklı, masaüstü ikincil | ❌ Reddedildi |

### Sonuçlar
- ✅ Modern UI bileşenleri
- ✅ XAML tabanlı tasarım
- ❌ Sadece Windows platformu
- ❌ XAML öğrenme eğrisi

---

## ADR-003: Async/Await Deseni

### Durum
**Kabul Edildi** - 2024

### Bağlum
UI donmalarını önlemek ve responsive bir uygulama sağlamak gerekiyordu. Özellikle büyük dosya okuma/yazma ve veritabanı işlemleri.

### Karar
Tüm IO-bound operasyonlarda `async/await` deseni kullanılacak.

### Gerekçeler
- UI thread bloklamasını önler
- Kullanıcı deneyimi artar
- Modern C# standartlarına uygun
- Hata yönetimi kolaylaşır

### Uygulama Alanları

```csharp
// DatabaseService
public async Task<List<ExamEntry>> GetAllExamsAsync()

// OpticalParserService  
public async Task<ParseResult> ParseFileAsync(string filePath)

// ExcelExportService
public async Task<bool> ExportToExcelAsync(string filePath, List<StudentResult> data)
```

### Sonuçlar
- ✅ Responsive UI
- ✅ Daha iyi performans algısı
- ❌ Kod karmaşıklığı artar
- ❌ Deadlock riskleri (dikkat gerektirir)

### Best Practices
- `ConfigureAwait(false)` kullanımı servis katmanında
- `async void` kullanımından kaçınılması (sadece event handlers)
- CancellationToken desteği

---

## ADR-004: AppData'da Kullanıcı Verisi

### Durum
**Kabul Edildi** - 2024

### Bağlam
Uygulama Program Files'a kuruluyor, ancak kullanıcı verilerine yazma izni yok.

### Karar
Tüm kullanıcı verileri (`%APPDATA%\OptikDegerlendirme`) klasöründe saklanacak.

### Saklanan Veriler

| Veri Tipi | Konum |
|-----------|-------|
| SQLite DB | `%APPDATA%\OptikDegerlendirme\optik.db` |
| Şablonlar | `%APPDATA%\OptikDegerlendirme\Templates\` |
| Ayarlar | `%APPDATA%\OptikDegerlendirme\appsettings.json` |
| Loglar | `%APPDATA%\OptikDegerlendirme\app_debug.log` |

### Uygulama

```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "OptikDegerlendirme");
```

### Sonuçlar
- ✅ Windows UAC uyumluluğu
- ✅ Her kullanıcı için ayrı veri
- ✅ Yedekleme kolaylığı
- ❌ Birden fazla kullanıcı verisi senkronize değil

---

## ADR-005: Dependency Injection (DI)

### Durum
**Kabul Edildi** - 2024

### Bağlam
Servis bağımlılıklarını yönetmek ve test edilebilirliği artırmak gerekiyordu.

### Karar
Microsoft.Extensions.DependencyInjection kullanılacak.

### Servis Kayıtları

```csharp
services.AddSingleton<DatabaseService>();
services.AddSingleton<OpticalParserService>();
services.AddSingleton<ExcelExportService>();
// ... diğer servisler
```

### Yaşam Döngüleri

| Servis | Yaşam Döngüsü | Gerekçe |
|--------|---------------|---------|
| DatabaseService | Singleton | Bağlantı paylaşımı |
| OpticalParserService | Singleton | Durumsuz servis |
| AppSettingsService | Singleton | Cache gereksinimi |
| ExcelExportService | Transient | Thread güvenliği |

### Sonuçlar
- ✅ Bağımlılık yönetimi
- ✅ Test edilebilirlik
- ✅ Tekrar kullanılabilirlik
- ❌ Başlangıç karmaşıklığı

---

## ADR-006: Self-Contained Deployment

### Durum
**Kabul Edildi** - 2024

### Bağlam
Kullanıcıların .NET Runtime kurmasını gerektirmeden uygulamayı çalıştırabilmeleri istendi.

### Karar
Self-contained deployment (SCD) kullanılacak.

### Yapılandırma

```xml
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

### Avantajlar
- ✅ .NET Runtime bağımsız
- ✅ Tek dosyada dağıtım
- ✅ Sürüm çakışması yok

### Dezavantajlar
- ❌ Dosya boyutu artışı (~50MB)
- ❌ Güncelleme gerektiğinde tam yeniden dağıtım

### Sonuçlar
- ✅ Kolay kurulum
- ✅ Bağımsız çalışma
- ❌ Büyük kurulum dosyası

---

## ADR-007: GitHub Actions CI/CD

### Durum
**Kabul Edildi** - 2024

### Bağlam
Otomatik build, test ve release sürecine ihtiyaç var.

### Karar
GitHub Actions ile CI/CD pipeline kuruldu.

### Pipeline Adımları

1. **Build**: `dotnet build`
2. **Test**: `dotnet test`
3. **Publish**: `dotnet publish --self-contained`
4. **Installer**: Inno Setup ile kurulum oluşturma
5. **Release**: GitHub Releases'e otomatik yükleme

### Trigger'lar

| Event | İşlem |
|-------|-------|
| Push to `main` | Build + Test |
| Tag `v*` | Full Release |
| Pull Request | Build + Test |

### Sonuçlar
- ✅ Otomatik kalite kontrol
- ✅ Tutarlı release süreci
- ✅ Hızlı geri bildirim

---

## ADR-008: Serilog ile Yapılandırılmış Logging

### Durum
**Kabul Edildi** - 2024

### Bağlam
Uygulama hatalarını ve performans metriklerini izlemek gerekiyordu.

### Karar
Serilog kütüphanesi kullanılacak.

### Log Seviyeleri

| Seviye | Kullanım |
|--------|----------|
| Verbose | Debug detayları |
| Debug | Geliştirme bilgisi |
| Information | Genel işlem bilgisi |
| Warning | Uyarılar |
| Error | Hatalar |
| Fatal | Kritik hatalar |

### Sonuçlar
- ✅ Yapılandırılmış loglar
- ✅ Dosya ve konsol çıktısı
- ✅ Hata ayıklama kolaylığı

---

## Karar Geçmişi

| Tarih | ADR | Durum | Notlar |
|-------|-----|-------|--------|
| 2024-Q1 | ADR-001 | Kabul Edildi | SQLite seçimi |
| 2024-Q1 | ADR-002 | Kabul Edildi | WPF + MVVM |
| 2024-Q1 | ADR-003 | Kabul Edildi | Async/Await |
| 2024-Q2 | ADR-004 | Kabul Edildi | AppData kullanımı |
| 2024-Q2 | ADR-005 | Kabul Edildi | DI implementasyonu |
| 2024-Q2 | ADR-006 | Kabul Edildi | Self-contained deploy |
| 2024-Q3 | ADR-007 | Kabul Edildi | GitHub Actions |
| 2024-Q3 | ADR-008 | Kabul Edildi | Serilog logging |

---

**Not**: Bu ADR'ler proje ekibi tarafından periyodik olarak gözden geçirilir ve güncellenir.
