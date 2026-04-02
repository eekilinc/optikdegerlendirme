# Katkıda Bulunma Rehberi (Contributing Guide)

Bu belge, Optik Değerlendirme Sistemi'ne katkıda bulunmak isteyen geliştiriciler için rehber niteliğindedir.

---

## 🚀 Başlangıç

### Geliştirme Ortamı Kurulumu

1. **Gereksinimler**
   - Windows 10/11
   - Visual Studio 2022 veya VS Code
   - .NET 9.0 SDK
   - Git

2. **Repo Klonlama**
   ```bash
   git clone https://github.com/eekilinc/optikdegerlendirme.git
   cd optikdegerlendirme
   ```

3. **Bağımlılıkları Yükleme**
   ```bash
   dotnet restore
   ```

4. **Build Etme**
   ```bash
   dotnet build
   ```

---

## 📝 Katkı Süreci

### 1. Issue Açma

Yeni bir özellik veya hata düzeltmesi için önce issue açın:

- **Bug Report**: Hatayı detaylı açıklayın, repro adımları ekleyin
- **Feature Request**: Özelliği ve kullanım senaryosunu açıklayın
- **Question**: Soruları Discussion bölümünde sorun

### 2. Fork ve Branch

```bash
# Fork edin (GitHub UI)
# Sonra local'de:

git checkout -b feature/amazing-feature
# veya
git checkout -b fix/bug-description
```

**Branch İsimlendirme:**
- `feature/ozellik-adi` - Yeni özellikler
- `fix/hata-adi` - Hata düzeltmeleri
- `docs/dokumantasyon` - Dokümantasyon güncellemeleri
- `refactor/kod-yeniden-duzenleme` - Kod yapılandırma

### 3. Geliştirme

#### Kod Standartları
- [STYLE_GUIDE.md](STYLE_GUIDE.md) dosyasını takip edin
- XML dokümantasyonu ekleyin
- Unit test yazın
- Kod tekrarından kaçının

#### Commit Mesajları
```
type(scope): subject

body (opsiyonel)

footer (opsiyonel)
```

**Tipler:**
- `feat`: Yeni özellik
- `fix`: Hata düzeltme
- `docs`: Dokümantasyon
- `style`: Kod stili (formatting, whitespace)
- `refactor`: Kod yeniden yapılandırma
- `test`: Test ekleme/güncelleme
- `chore`: Build, dependency güncellemeleri

**Örnekler:**
```
feat(prediction): Öğrenci başarı tahmini servisi ekle

- ML tabanlı tahmin algoritması implemente edildi
- Sigmoid regresyon modeli kullanıldı
- Risk seviyesi hesaplama eklendi

Closes #123
```

```
fix(database): SQLite locked hatası için retry mekanizması

- Exponential backoff ile 3 deneme
- Busy timeout 5000ms olarak ayarlandı

Fixes #456
```

### 4. Test

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli testleri çalıştır
dotnet test --filter "FullyQualifiedName~Database"

# Code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 5. Pull Request

#### PR Açmadan Önce Checklist

- [ ] Kod derleniyor (`dotnet build`)
- [ ] Testler geçiyor (`dotnet test`)
- [ ] Kod stili kontrolü (`dotnet format --verify`)
- [ ] XML dokümantasyonu tam
- [ ] CHANGELOG.md güncellendi (gerekirse)

#### PR Şablonu

```markdown
## Açıklama
Bu PR ne yapıyor? Neden gerekli?

## Değişiklikler
- Özellik/Hata düzeltme 1
- Özellik/Hata düzeltme 2

## Testler
- [ ] Unit testler eklendi
- [ ] Manuel test yapıldı
- [ ] E2E testler geçiyor

## Ekran Görüntüleri (UI değişiklikleri için)

## İlgili Issue'lar
Closes #123
Relates to #456
```

### 6. Code Review

Review süreci:
1. En az 1 reviewer onayı gerekli
2. CI/CD pipeline'ı geçmeli
3. Conflict'ler çözülmeli

---

## 🏗️ Proje Yapısı

```
optikdegerlendirme/
├── Models/           # Veri modelleri (POCO)
├── ViewModels/       # MVVM ViewModels
├── Views/            # WPF UserControls
├── Services/         # İş mantığı servisleri
├── Converters/       # XAML value converters
├── Helpers/          # Utility sınıfları
├── docs/             # Dokümantasyon
└── installer/        # Inno Setup kurulum scriptleri
```

### Katmanlı Mimari

```
┌─────────────────────────────────────┐
│         Presentation (Views)        │
├─────────────────────────────────────┤
│         Business Logic (VM)         │
├─────────────────────────────────────┤
│         Services Layer              │
├─────────────────────────────────────┤
│         Data Access (SQLite)        │
└─────────────────────────────────────┘
```

---

## 🧪 Test Yazımı

### Unit Test Örneği

```csharp
[TestFixture]
public class OpticalParserServiceTests
{
    private OpticalParserService _parser;
    
    [SetUp]
    public void Setup()
    {
        _parser = new OpticalParserService();
    }
    
    [Test]
    public async Task ParseFileAsync_ValidFormat_ReturnsStudents()
    {
        // Arrange
        var testFile = CreateTestFile("AHMET YILMAZ          1234567890AABC...");
        
        // Act
        var result = await _parser.ParseFileAsync(testFile);
        
        // Assert
        Assert.That(result.Students, Has.Count.EqualTo(1));
        Assert.That(result.Students[0].StudentId, Is.EqualTo("1234567890"));
        Assert.That(result.Errors, Is.Empty);
    }
    
    [Test]
    public void ParseLineAsync_InvalidStudentId_ThrowsFormatException()
    {
        // Arrange
        var invalidLine = "AHMET YILMAZ          INVALIDID AABC...";
        
        // Act & Assert
        Assert.ThrowsAsync<FormatException>(async () =>
            await _parser.ParseLineAsync(invalidLine, 1));
    }
}
```

### Test Kategorileri

| Kategori | Açıklama | Örnek |
|----------|----------|-------|
| **Unit** | Tek bir sınıf/metod testi | `DatabaseServiceTests` |
| **Integration** | Birden fazla katman testi | `ExamWorkflowTests` |
| **E2E** | UI otomasyon testleri | `MainWindowTests` |

---

## 📊 Performans Optimizasyonu

### Benchmarking

```csharp
[MemoryDiagnoser]
public class ParserBenchmarks
{
    private OpticalParserService _parser;
    private string _testData;
    
    [GlobalSetup]
    public void Setup()
    {
        _parser = new OpticalParserService();
        _testData = GenerateLargeTestData(10000);
    }
    
    [Benchmark]
    public async Task ParseLargeFile()
    {
        await _parser.ParseFileAsync(_testData);
    }
}
```

### Profiling

```bash
# Memory profiling
dotnet trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime:0x1:5

# Performance profiling
dotnet trace collect --process-id <PID> --profile gc-verbose
```

---

## 🐛 Hata Ayıklama

### Log Seviyeleri

```csharp
_logger.Verbose("Detaylı debug bilgisi");
_logger.Debug("Geliştirme bilgisi");
_logger.Information("Genel işlem bilgisi");
_logger.Warning("Uyarı");
_logger.Error("Hata");
_logger.Fatal("Kritik hata");
```

### Debug İpuçları

1. **Visual Studio**
   - Breakpoints kullanın
   - Watch window ile değişkenleri izleyin
   - Call stack'i inceleyin

2. **VS Code**
   - `launch.json` yapılandırması
   - Debug console kullanımı

3. **Log Dosyaları**
   - `%APPDATA%\OptikDegerlendirme\app_debug.log`

---

## 🔄 CI/CD Pipeline

### GitHub Actions İş Akışı

```yaml
# .github/workflows/pr-check.yml
name: PR Checks

on: [pull_request]

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Test
        run: dotnet test --no-build --verbosity normal
      
      - name: Format Check
        run: dotnet format --verify-no-changes
```

---

## 🌍 Yerelleştirme (Localization)

### Yeni Dil Ekleme

1. `Resources/` klasöründe yeni `.resx` dosyası oluştur
2. Çevirileri ekle
3. `LocalizationService`'i güncelle

```csharp
// LocalizationService.cs
public string GetString(string key, string language = "tr")
{
    return language switch
    {
        "tr" => Resources.Strings.ResourceManager.GetString(key),
        "en" => Resources.Strings_en.ResourceManager.GetString(key),
        _ => key
    };
}
```

---

## 📦 Release Süreci

### Versiyon Yönetimi (Semantic Versioning)

```
MAJOR.MINOR.PATCH

1.2.5
│ │ │
│ │ └─ Patch: Hata düzeltmeleri
│ └─── Minor: Yeni özellikler (geri uyumlu)
└───── Major: Kırıcı değişiklikler
```

### Release Adımları

1. **Versiyon Güncelleme**
   ```xml
   <!-- OptikFormApp.csproj -->
   <Version>1.2.5</Version>
   <AssemblyVersion>1.2.5.0</AssemblyVersion>
   ```

2. **CHANGELOG Güncelleme**
   ```markdown
   ## [1.2.5] - 2024-XX-XX
   ### Added
   - Yeni özellik
   
   ### Fixed
   - Hata düzeltme
   ```

3. **Tag Oluşturma**
   ```bash
   git tag -a v1.2.5 -m "Release version 1.2.5"
   git push origin v1.2.5
   ```

4. **GitHub Actions** otomatik olarak:
   - Build eder
   - Testleri çalıştırır
   - Installer oluşturur
   - Release yayımlar

---

## 💬 İletişim

### Soru Sorma

- **GitHub Discussions**: Genel sorular
- **GitHub Issues**: Bug report ve feature request
- **Email**: teknik destek için

### Topluluk Kuralları

- Saygılı ve profesyonel olun
- Kritik yapıcı olun
- Yardım isteyenlere yardım edin
- Dokümantasyonu okumadan soru sormayın 😊

---

## 🎉 Teşekkürler!

Katkınız için teşekkür ederiz. Her katkı, bu projeyi daha iyi yapıyor!

**İlk kez katkıda bulunacaklar için:**
- [First Contributions](https://firstcontributions.github.io/) rehberine bakabilirsiniz
- [Good First Issue](https://github.com/eekilinc/optikdegerlendirme/labels/good%20first%20issue) etiketli issue'larla başlayabilirsiniz

---

**Son Güncelleme:** 2024

**Bu belge sürekli güncellenmektedir.**
