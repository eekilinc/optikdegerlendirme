# Optik Değerlendirme - Kod Stil Rehberi

Bu belge, projede tutarlı kod kalitesi sağlamak için kullanılan stil kurallarını ve best practice'leri içerir.

---

## 📋 Genel Prensipler

### 1. KISS (Keep It Simple, Stupid)
- Karmaşık çözümler yerine basit çözümler tercih edin
- Her metod tek bir iş yapmalı
- Aşırı mühendislikten kaçının

### 2. DRY (Don't Repeat Yourself)
- Kod tekrarından kaçının
- Tekrar eden kodları metodlara çıkarın
- Ortak işlevler için servisler kullanın

### 3. SOLID Prensipleri
- **Single Responsibility**: Her sınıf tek bir iş yapmalı
- **Open/Closed**: Yeni özellikler için genişlemeye açık, değişime kapalı
- **Liskov Substitution**: Türetilmiş sınıflar temel sınıf yerine kullanılabilmeli
- **Interface Segregation**: Küçük, odaklı interface'ler
- **Dependency Inversion**: Yüksek seviyeli modüller düşük seviyelere bağlı olmamalı

---

## 📝 İsimlendirme Kuralları

### Sınıflar
```csharp
// ✅ Doğru
public class StudentResult
public class OpticalParserService
public class ExamConfiguration

// ❌ Yanlış
public class studentresult
public class optical_parser_service
public class Exam_Configuration
```

**Kural**: PascalCase, isimler açıklayıcı olmalı

### Metodlar
```csharp
// ✅ Doğru
public async Task<List<StudentResult>> ParseFileAsync(string filePath)
public void SaveExamConfiguration(ExamConfigData config)
public bool ValidateStudentId(string studentId)

// ❌ Yanlış
public async Task<List<StudentResult>> parseFile(string filePath)
public void saveexamconfig(ExamConfigData config)
public bool check_id(string studentId)
```

**Kural**: PascalCase, fiil başlayan açıklayıcı isimler

### Değişkenler ve Alanlar
```csharp
// ✅ Doğru
private readonly string _databasePath;
private int _studentCount;
public string StatusMessage { get; set; }

// ❌ Yanlış
private readonly string DatabasePath;
private int student_count;
public string status_message { get; set; }
```

**Kural**: 
- Private alanlar: `_camelCase`
- Public/Protected: `PascalCase`
- Local değişkenler: `camelCase`

### Sabitler
```csharp
// ✅ Doğru
public const int MaxQuestionCount = 100;
public const string DefaultDatabaseName = "optik.db";

// ❌ Yanlış
public const int maxQuestions = 100;
public const string DEFAULT_DB = "optik.db";
```

**Kural**: PascalCase

### Interface'ler
```csharp
// ✅ Doğru
public interface IExamService
public interface IRepository<T>

// ❌ Yanlış
public interface ExamServiceInterface
public interface Repository
```

**Kural**: `I` prefix + PascalCase

---

## 🏗️ Kod Organizasyonu

### Namespace Yapısı
```
OptikFormApp
├── Models          # Veri modelleri
├── ViewModels      # MVVM ViewModels
├── Views           # WPF kontrolleri
├── Services        # İş mantığı
├── Converters      # XAML converters
└── Helpers         # Yardımcı sınıflar
```

### Dosya Düzeni
Her dosya tek bir sınıf içermeli:
```csharp
// StudentResult.cs
namespace OptikFormApp.Models;

public class StudentResult
{
    // ...
}
```

### Using İfadeleri
```csharp
// ✅ Doğru - gruplanmış ve sıralı
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

using OptikFormApp.Models;
using OptikFormApp.Services;

// ❌ Yanlış - karışık
using OptikFormApp.Models;
using System;
using Microsoft.Data.Sqlite;
using System.Linq;
```

**Sıralama**: System → Microsoft → 3rd party → Proje

---

## 💬 Yorum ve Dokümantasyon

### XML Dokümantasyonu
```csharp
/// <summary>
/// Optik form dosyasını parse eder.
/// </summary>
/// <param name="filePath">TXT dosyasının yolu.</param>
/// <returns>Parse sonuçları.</returns>
/// <exception cref="IOException">Dosya okuma hatası.</exception>
public async Task<ParseResult> ParseFileAsync(string filePath)
```

### Satır İçi Yorumlar
```csharp
// ✅ Doğru - neden sorusunu yanıtlar
// WAL mode daha iyi concurrency sağlar
PRAGMA journal_mode = WAL;

// ❌ Yanlış - ne yaptığını açıklar (kod zaten açık)
// Değişkeni 1 artır
_count++;
```

### Bölge Yorumları
```csharp
// ── Public API ─────────────────────────────────────────────────────────

// ── Private Methods ────────────────────────────────────────────────────

// ── Event Handlers ───────────────────────────────────────────────────────
```

---

## 🔒 Güvenlik ve Hata Yönetimi

### Exception Handling
```csharp
// ✅ Doğru - özel exception tipleri
try
{
    await _dbService.SaveAsync(data);
}
catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // BUSY
{
    // Database locked - retry logic
    await RetryAsync(() => _dbService.SaveAsync(data));
}
catch (IOException ex)
{
    _logger.Error(ex, "Dosya yazma hatası");
    throw new ApplicationException("Dosya kaydedilemedi", ex);
}

// ❌ Yanlış - genel yakalama
try
{
    await _dbService.SaveAsync(data);
}
catch (Exception ex)
{
    // Hata yutuluyor!
}
```

### Null Kontrolü
```csharp
// ✅ Doğru - modern C# özellikleri
public void ProcessStudent(StudentResult? student)
{
    ArgumentNullException.ThrowIfNull(student);
    
    var name = student.Name ?? throw new ArgumentException("İsim boş olamaz");
}

// ❌ Yanlış - eski stil
public void ProcessStudent(StudentResult student)
{
    if (student == null)
        throw new ArgumentNullException(nameof(student));
}
```

### Async/Await
```csharp
// ✅ Doğru
public async Task<List<Exam>> GetAllExamsAsync()
{
    var exams = await _dbService.GetExamsAsync().ConfigureAwait(false);
    return exams;
}

// ❌ Yanlış
public async Task<List<Exam>> GetAllExamsAsync()
{
    // ConfigureAwait(false) eksiz - potansiyel deadlock
    var exams = await _dbService.GetExamsAsync();
    return exams;
}
```

---

## 🎨 WPF ve XAML

### XAML Stili
```xml
<!-- ✅ Doğru - formatlanmış ve açıklayıcı -->
<Button Content="Kaydet"
        Command="{Binding SaveCommand}"
        IsEnabled="{Binding CanSave}"
        Style="{StaticResource PrimaryButton}"
        ToolTip="Sınavı kaydet (Ctrl+S)" />

<!-- ❌ Yanlış - tek satır ve formatlanmamış -->
<Button Content="Kaydet" Command="{Binding SaveCommand}" IsEnabled="{Binding CanSave}" Style="{StaticResource PrimaryButton}" />
```

### Binding'ler
```xml
<!-- ✅ Doğru - açıklayıcı binding isimleri -->
<TextBlock Text="{Binding StudentCount, StringFormat='{}{0} öğrenci'}" />
<ProgressBar Value="{Binding ParseProgress, Mode=OneWay}" />

<!-- ❌ Yanlış - karmaşık binding'ler inline -->
<TextBlock Text="{Binding Students.Count}" />
```

### Resource Keys
```xml
<!-- ✅ Doğru -->
<Style x:Key="PrimaryButton" TargetType="Button">
<Style x:Key="DataGridHeaderStyle" TargetType="DataGridColumnHeader">
<Color x:Key="PrimaryBlue">#3B82F6</Color>

<!-- ❌ Yanlış -->
<Style x:Key="btnPrimary" TargetType="Button">
<Style x:Key="style1" TargetType="DataGridColumnHeader">
```

---

## 🧪 Test Yazımı

### Test İsimlendirmesi
```csharp
// ✅ Doğru - MethodName_StateUnderTest_ExpectedBehavior
[Test]
public void ParseFileAsync_EmptyFile_ReturnsEmptyList()
[Test]
public void SaveExam_ValidData_SavesToDatabase()
[Test]
public void CalculateNet_ScoreWithWrongAnswers_DeductsPoints()

// ❌ Yanlış
[Test]
public void Test1()
[Test]
public void ParseTest()
```

### Test Yapısı (AAA)
```csharp
[Test]
public async Task ParseFileAsync_ValidFormat_ReturnsStudents()
{
    // Arrange
    var parser = new OpticalParserService();
    var testFile = "test_data.txt";
    
    // Act
    var result = await parser.ParseFileAsync(testFile);
    
    // Assert
    Assert.That(result.Students, Is.Not.Empty);
    Assert.That(result.Errors, Is.Empty);
}
```

---

## 📊 Performans

### String İşlemleri
```csharp
// ✅ Doğru - StringBuilder uzun string'ler için
var sb = new StringBuilder();
foreach (var student in students)
{
    sb.AppendLine(student.ToString());
}
return sb.ToString();

// ❌ Yanlış - string concatenation
var result = "";
foreach (var student in students)
{
    result += student.ToString() + Environment.NewLine;
}
```

### LINQ Kullanımı
```csharp
// ✅ Doğru - deferred execution kullan
var query = students
    .Where(s => s.Score > 50)
    .OrderByDescending(s => s.Score)
    .Take(10);

// ❌ Yanlış - gereksiz ToList()
var list = students.Where(s => s.Score > 50).ToList();
var sorted = list.OrderByDescending(s => s.Score).ToList();
var top10 = sorted.Take(10).ToList();
```

### Async Enumarable
```csharp
// ✅ Doğru - IAsyncEnumerable büyük veri setleri için
public async IAsyncEnumerable<StudentResult> StreamStudentsAsync()
{
    await foreach (var student in _dbService.GetStudentsAsync())
    {
        yield return student;
    }
}
```

---

## 🔍 Kod Review Checklist

### Her PR'de Kontrol Edilmeli

- [ ] İsimlendirme kurallarına uygunluk
- [ ] XML dokümantasyonu (public API'ler için)
- [ ] Null kontrolü ve hata yönetimi
- [ ] Async/await kullanımı (UI thread bloklanmamalı)
- [ ] Magic number ve string yok
- [ ] Test coverage (yeni kod için)
- [ ] Kod tekrarı yok

### Anti-Patterns

```csharp
// ❌ God Object
public class Manager // 2000+ satır, her şeyi yapıyor

// ❌ Spaghetti Code
if (a) { if (b) { if (c) { ... } } }

// ❌ Magic Numbers
if (score > 50) // 50 nedir?

// ❌ Copy-Paste
// Aynı kod 5 farklı yerde
```

---

## 🛠️ Araçlar ve Konfigürasyon

### EditorConfig
```ini
[*]
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
csharp_space_after_cast = false
```

### Gerekli VS Code Eklentileri
- C# Dev Kit
- EditorConfig for VS Code
- .NET Extension Pack

### Gerekli Visual Studio Eklentileri
- ReSharper veya Rider (isteğe bağlı)
- EditorConfig
- Spell Checker

---

## 🔄 Versiyon Kontrolü

### Commit Mesajları
```
feat: Yeni özellik ekle
fix: Hata düzelt
refactor: Kod yapılandırma (davranış değişmez)
docs: Dokümantasyon güncelle
test: Test ekle/güncelle
chore: Build/dependency güncelle
```

### Örnek Commit Mesajları
```
feat: Öğrenci başarı tahmini servisi ekle
fix: SQLite busy hatası için retry mekanizması
refactor: DatabaseService async yapıya geçir
docs: API dokümantasyonu güncelle
test: OpticalParserService unit testleri ekle
chore: .NET 9.0'a yükselt
```

---

**Bu stil rehberi proje ekibi tarafından ortak kararla belirlenmiş ve sürekli güncellenmektedir.**
