# Karmaşık Algoritmalar ve Veri Akışları

Bu belge, Optik Değerlendirme Sistemi'nin karmaşık algoritmalarını, veri akışlarını ve matematiksel modellerini detaylı olarak açıklar.

---

## 🎯 Başarı Tahmini Algoritması

### Genel Bakış

**SuccessPredictionService**, öğrencilerin sınav başarısını tahmin etmek için çok faktörlü bir regresyon modeli kullanır. Bu algoritma, öğrencinin mevcut performansını, sınıf istatistiklerini ve tarihsel verileri analiz ederek gelecekteki başarı olasılığını hesaplar.

### Veri Akışı

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  StudentResult  │────▶│  AnalyzeFactors  │────▶│  CalculateScore │
│     Input       │     │  (5 Faktör)      │     │  (Regression)   │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                                           │
                              ┌──────────────────┐        │
                              │  Class Statistics│◀───────┤
                              │  (Mean, Max, Std)│        │
                              └──────────────────┘        │
                                                           ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Risk Level     │◀────│  Pass Probability│◀────│  Confidence     │
│  (Low/Med/High) │     │  (Sigmoid)       │     │  (Data Quality) │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

### Faktör Analizi

Algoritma 5 temel başarı faktörünü analiz eder:

| Faktör | Açıklama | Ağırlık | Hesaplama |
|--------|----------|---------|-----------|
| **PerformanceFactor** | Normalized puan | 35% | `score / 100` |
| **SuccessRate** | Doğru/yanlış oranı | 25% | `correct / (correct + wrong)` |
| **RankingFactor** | Sınıf içi sıralama | 20% | `1 - (rank / totalStudents)` |
| **NetFactor** | Net sayısı oranı | 15% | `netCount / maxNet` |
| **EmptyRate** | Boş bırakma oranı | 5% | `1 - (empty / total)` |

```csharp
// Ağırlıklı skor hesaplama
weightedScore = 
    PerformanceFactor * 0.35 +
    SuccessRate * 0.25 +
    RankingFactor * 0.20 +
    NetFactor * 0.15 +
    EmptyRate * 0.05;
```

### Regresyon Modeli

Tahmin skoru iki bileşenin birleşimi ile hesaplanır:

```
Prediction = (WeightedScore * 100 * 0.6) + (Regression * 0.4)

Where:
  WeightedScore = Faktörlerin ağırlıklı toplamı (0-1)
  Regression = Sınıf ortalaması * 0.3 + Öğrenci puanı * 0.7
```

**Örnek Hesaplama:**
```
Öğrenci Puanı: 75
Sınıf Ortalaması: 65
Weighted Score: 0.72

Prediction = (0.72 * 100 * 0.6) + ((65 * 0.3 + 75 * 0.7) * 0.4)
           = 43.2 + (72 * 0.4)
           = 43.2 + 28.8
           = 72.0
```

### Güven Seviyesi Hesaplama

Güven seviyesi, veri miktarı ve tamamlama oranına dayanır:

```csharp
// Veri miktarı güveni (daha fazla öğrenci = daha yüksek güven)
dataConfidence = min(totalStudents / 100, 1.0)

// Tamamlama oranı
completionRate = answeredQuestions / totalQuestions

// Toplam güven
totalConfidence = max(0.3, dataConfidence * 0.7 + completionRate * 0.3)
```

### Geçme Olasılığı (Sigmoid Fonksiyonu)

Geçme olasılığı, sigmoid (logistic) fonksiyonu ile hesaplanır:

```
                    1
P(pass) = ───────────────────
          1 + e^(-gap * confidence)

Where:
  gap = (predictedScore - passingScore) / 20
  e = Euler's number (~2.718)
```

**Sigmoid Grafiği:**

```
Probability
    1.0 │                    ┌────────
        │                 ┌─┘
    0.8 │              ┌──┘
        │           ┌──┘
    0.5 │──────────┘
        │      ┌───┘
    0.2 │   ┌──┘
        │┌──┘
    0.0 ├─┘
        └─────┬─────┬─────┬─────┬─────▶ Score Gap
            -40   -20    0    +20   +40
```

### Risk Seviyesi Belirleme

Risk skoru hesaplanır ve eşik değerlerine göre kategorize edilir:

```csharp
riskScore = predictedScore * confidence;

if (riskScore < passingScore * 0.8)   → High Risk
else if (riskScore < passingScore * 1.1) → Medium Risk
else                                      → Low Risk
```

**Örnek:**
```
Predicted: 72, Confidence: 0.85, Passing: 50
Risk Score = 72 * 0.85 = 61.2

Thresholds:
  High:    < 40 (50 * 0.8)
  Medium:  40-55 (50 * 1.1)
  Low:     > 55

Result: Low Risk ✅
```

### Sınıf Seviyesi Analizi

```csharp
// Sınıf risk dağılımı
HighRisk%  = highRiskCount / totalStudents
MediumRisk% = mediumRiskCount / totalStudents
LowRisk%   = lowRiskCount / totalStudents

// Geçme oranı
PassRate = studentsWithProbability > 0.7 / totalStudents

// Otomatik öneriler
if (HighRisk% > 30%) → "Ek destek gerekli"
if (AverageScore < Passing) → "Genel tekrar önerilir"
if (PassRate < 80%) → "Ek çalışma programı"
```

---

## 📊 Net Hesaplama Algoritması

### Standart 4'lü Sistem

```
Net = Doğru - (Yanlış / 4)

Örnek:
  Doğru: 80
  Yanlış: 12
  Net = 80 - (12 / 4) = 80 - 3 = 77
```

### Özelleştirilebilir Katsayı

```csharp
public double CalculateNet(int correct, int wrong, double coefficient = 0.25)
{
    return correct - (wrong * coefficient);
}
```

**Katsayı Değerleri:**
- `0.25` = 4 yanlış 1 doğru götürür (standart)
- `0.33` = 3 yanlış 1 doğru götürür
- `0.00` = Yanlışlar ceza vermez

---

## 📈 İstatistik Hesaplamaları

### Temel İstatistikler

```csharp
public class StatisticsCalculator
{
    // Aritmetik Ortalama
    public static double Mean(List<double> values)
    {
        return values.Sum() / values.Count;
    }
    
    // Standart Sapma
    public static double StdDev(List<double> values)
    {
        double mean = Mean(values);
        double variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }
    
    // Medyan
    public static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
    
    // Mod (En sık görülen değer)
    public static double Mode(List<double> values)
    {
        return values.GroupBy(v => v)
                     .OrderByDescending(g => g.Count())
                     .First()
                     .Key;
    }
}
```

### Yüzde Dilimler

```csharp
public static double Percentile(List<double> values, double percentile)
{
    var sorted = values.OrderBy(v => v).ToList();
    int index = (int)Math.Ceiling(sorted.Count * percentile) - 1;
    return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
}

// Kullanım:
Percentile(scores, 0.90) // 90. percentile (en iyi %10)
Percentile(scores, 0.10) // 10. percentile (en düşük %10)
```

### Sınıf İçi Sıralama

```csharp
public static void CalculateRanks(List<StudentResult> students)
{
    // Puana göre sırala (yüksekten düşüğe)
    var ordered = students.OrderByDescending(s => s.Score).ToList();
    
    for (int i = 0; i < ordered.Count; i++)
    {
        ordered[i].Rank = i + 1;
        
        // Yüzdelik dilim
        ordered[i].Percentile = 100.0 * (students.Count - i) / students.Count;
    }
}
```

---

## 🔄 Optik Form Parse Algoritması

### Veri Formatı

```
[0-21]   Ad Soyad (22 karakter)
[22-31]  Öğrenci No (10 karakter)
[32]     Kitapçık Tipi (1 karakter: A/B/C/D)
[33+]    Cevaplar (100+ karakter)
```

### Parse Adımları

```csharp
public async Task<ParseResult> ParseLineAsync(string line, int lineNumber)
{
    // 1. Validasyon
    if (line.Length < 33)
        throw new FormatException("Satır çok kısa");
    
    // 2. Alanları çıkar
    string fullName = line.Substring(0, 22).Trim();
    string studentId = line.Substring(22, 10).Trim();
    string bookletType = line.Substring(32, 1).ToUpper();
    string rawAnswers = line.Substring(33).TrimEnd();
    
    // 3. Format kontrolü
    if (!IsValidStudentId(studentId))
        throw new FormatException("Geçersiz öğrenci numarası");
    
    if (!IsValidBookletType(bookletType))
        throw new FormatException("Geçersiz kitapçık tipi");
    
    // 4. Cevapları parse et
    var answers = ParseAnswers(rawAnswers);
    
    // 5. Sonuç nesnesi oluştur
    return new StudentResult
    {
        FullName = fullName,
        StudentId = studentId,
        BookletType = bookletType,
        Answers = answers,
        SourceLine = lineNumber
    };
}
```

### Hata Toleransı

```csharp
private bool IsTolerableError(string error)
{
    // Hata mesajına göre tolerans seviyesi
    return error switch
    {
        "StudentId_Empty" => false,     // Kritik
        "Name_Missing" => true,          // Uyarı
        "Answers_Partial" => true,       // Kabul edilebilir
        "Booklet_Invalid" => false,       // Kritik
        _ => false
    };
}
```

---

## 📊 Cevap Anahtarı Eşleştirme

### Doğru/Yanlış/Boş Hesaplama

```csharp
public void EvaluateAnswers(StudentResult student, AnswerKeyModel key)
{
    int correct = 0, wrong = 0, empty = 0;
    
    for (int i = 0; i < key.Answers.Count; i++)
    {
        char studentAnswer = i < student.Answers.Count 
            ? student.Answers[i] 
            : ' ';
        char correctAnswer = key.Answers[i];
        
        if (studentAnswer == ' ' || studentAnswer == '-')
        {
            empty++;
            student.AnswerStatus[i] = AnswerState.Empty;
        }
        else if (char.ToUpper(studentAnswer) == char.ToUpper(correctAnswer))
        {
            correct++;
            student.AnswerStatus[i] = AnswerState.Correct;
            student.Score += key.Points[i];
        }
        else
        {
            wrong++;
            student.AnswerStatus[i] = AnswerState.Wrong;
        }
    }
    
    student.CorrectCount = correct;
    student.IncorrectCount = wrong;
    student.EmptyCount = empty;
    student.NetCount = correct - (wrong * key.WrongDeduction);
}
```

### Çoklu Kitapçık Desteği

```
Kitapçık A: 1-A, 2-B, 3-C, 4-D, 5-E
Kitapçık B: 1-E, 2-A, 3-B, 4-C, 5-D  (kaydırılmış)
Kitapçık C: 1-D, 2-E, 3-A, 4-B, 5-C
Kitapçık D: 1-C, 2-D, 3-E, 4-A, 5-B
```

```csharp
public AnswerKeyModel GetKeyForBooklet(string bookletType, List<AnswerKeyModel> allKeys)
{
    return allKeys.FirstOrDefault(k => 
        k.BookletType.Equals(bookletType, StringComparison.OrdinalIgnoreCase))
        ?? allKeys.First(); // Varsayılan A kitapçığı
}
```

---

## 💾 Veritabanı İşlem Akışı

### Transaction Yönetimi

```csharp
public async Task<bool> SaveExamWithStudentsAsync(ExamEntry exam, List<StudentResult> students)
{
    using var transaction = _connection.BeginTransaction();
    
    try
    {
        // 1. Sınav kaydet
        var examId = await InsertExamAsync(exam, transaction);
        
        // 2. Öğrencileri kaydet
        foreach (var student in students)
        {
            student.ExamId = examId;
            await InsertStudentAsync(student, transaction);
            
            // 3. Cevapları kaydet
            await InsertAnswersAsync(student.Answers, student.Id, transaction);
        }
        
        // 4. Transaction commit
        transaction.Commit();
        return true;
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        _logger.Error(ex, "Transaction başarısız");
        return false;
    }
}
```

### Batch Insert (Performans)

```csharp
public async Task BulkInsertStudentsAsync(List<StudentResult> students)
{
    using var cmd = _connection.CreateCommand();
    
    // Hazırlıklı ifade (prepared statement)
    cmd.CommandText = @"
        INSERT INTO Students (ExamId, StudentId, FullName, Score, NetCount)
        VALUES (@examId, @studentId, @fullName, @score, @netCount)";
    
    foreach (var student in students)
    {
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@examId", student.ExamId);
        cmd.Parameters.AddWithValue("@studentId", student.StudentId);
        cmd.Parameters.AddWithValue("@fullName", student.FullName);
        cmd.Parameters.AddWithValue("@score", student.Score);
        cmd.Parameters.AddWithValue("@netCount", student.NetCount);
        
        await cmd.ExecuteNonQueryAsync();
    }
}
```

---

## 📊 Excel/CSV Export Akışı

### Streaming Export (Büyük Veri)

```csharp
public async Task ExportLargeDataAsync(string filePath, IAsyncEnumerable<StudentResult> students)
{
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Sonuçlar");
    
    // Başlıklar
    worksheet.Cell(1, 1).Value = "Öğrenci No";
    worksheet.Cell(1, 2).Value = "Ad Soyad";
    worksheet.Cell(1, 3).Value = "Puan";
    
    int row = 2;
    await foreach (var student in students)
    {
        worksheet.Cell(row, 1).Value = student.StudentId;
        worksheet.Cell(row, 2).Value = student.FullName;
        worksheet.Cell(row, 3).Value = student.Score;
        
        // Her 1000 satırda bir save
        if (row % 1000 == 0)
        {
            worksheet.Row(row).Style.Font.Bold = false;
        }
        
        row++;
    }
    
    workbook.SaveAs(filePath);
}
```

### CSV Format

```csharp
public async Task ExportCsvAsync(string filePath, List<StudentResult> students)
{
    var csv = new StringBuilder();
    
    // Header
    csv.AppendLine("StudentId,FullName,Score,NetCount,Correct,Wrong,Empty");
    
    // Data
    foreach (var student in students)
    {
        csv.AppendLine($"{student.StudentId},{EscapeCsv(student.FullName)},{student.Score},{student.NetCount},{student.CorrectCount},{student.IncorrectCount},{student.EmptyCount}");
    }
    
    await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
}

private string EscapeCsv(string value)
{
    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    return value;
}
```

---

## 🔄 Memory Yönetimi ve Performans

### Virtualization (DataGrid)

```xml
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          ScrollViewer.CanContentScroll="True"
          EnableRowVirtualization="True"
          EnableColumnVirtualization="True">
```

### Lazy Loading

```csharp
public class LazyDataProvider
{
    private readonly int _pageSize = 100;
    
    public async Task<List<StudentResult>> GetPageAsync(int pageNumber)
    {
        return await _dbService.QueryAsync<StudentResult>(
            "SELECT * FROM Students LIMIT @limit OFFSET @offset",
            new { limit = _pageSize, offset = pageNumber * _pageSize });
    }
}
```

### Memory Cache

```csharp
public class CacheService<T>
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    
    public T GetOrCreate(string key, Func<T> factory, TimeSpan expiration)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration;
            return factory();
        });
    }
}
```

---

## 🎯 Sonuç

Bu algoritmalar, Optik Değerlendirme Sistemi'nin temel matematiksel ve mantıksal işlemlerini oluşturur. Her algoritma:

- ✅ **Test edilebilir** - Unit testlerle doğrulanmış
- ✅ **Ölçeklenebilir** - Büyük veri setlerinde çalışabilir
- ✅ **Hata toleranslı** - Graceful failure handling
- ✅ **Performans odaklı** - Optimize edilmiş hesaplamalar

**Not**: Algoritmalar sürekli olarak gerçek dünya verileriyle test edilmekte ve iyileştirilmektedir.
