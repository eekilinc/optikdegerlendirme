using System;
using System.Windows;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using OptikFormApp.Models;
using OptikFormApp.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Input;

namespace OptikFormApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DatabaseService _dbService;
        private readonly OpticalParserService _parserService;
        private readonly ExcelExportService _excelService;
        private readonly ValidationService _validationService;
        private readonly PdfReportService _pdfService;
        private readonly AppSettingsService _settingsService;
        private readonly CsvExportService _csvService;
        private readonly NotificationService _notificationService;
        private readonly UndoRedoManager _undoRedoManager;
        private readonly StatisticsReportService _statsReportService;
        private readonly TemplateService _templateService;
        private readonly JsonDataService _jsonDataService;
        private readonly KeyboardShortcutService _shortcutService;
        private readonly ProgressService _progressService;
        private readonly VersionCheckService _versionCheckService;
        private readonly ItemAnalysisService _itemAnalysisService;
        private readonly SuccessPredictionService _successPredictionService;
        private readonly VersionService _versionService;
        private readonly BackupService _backupService;

        private readonly object _studentsLock = new();
        private readonly object _coursesLock = new();
        private readonly object _examsLock = new();
        private readonly object _statsLock = new();
        private readonly object _validationLock = new();
        private readonly object _logsLock = new();
        
        private Course? _selectedCourse;
        private ExamEntry? _selectedExam;
        private bool _isAddCourseOpen;
        private string _newCourseCode = "";
        private string _newCourseName = "";
        private bool _isModalOpen;
        private string _statusMessage = "İşlem Bekleniyor. TXT dosyası yükleyin.";
        private bool _isQuestionSettingsOpen;
        private bool _isLearningOutcomesOpen;
        private bool _isAlertOpen;
        private string _alertTitle = string.Empty;
        private string _alertMessage = string.Empty;
        private bool _isAboutOpen;
        private bool _isUISettingsOpen;
        private bool _isRenameModalOpen;
        private bool _isConfirmModalOpen;
        private string _confirmModalTitle = "";
        private string _confirmModalMessage = "";
        private string _confirmButtonText = "Sil";
        private System.Action? _onConfirmAction;
        private bool _isShortcutSettingsOpen;
        private string _renameModalTitle = "";
        private string _renameInput1 = "";
        private string _renameInput2 = "";
        private bool _showRenameInput2;
        private int _renameId;
        private string _renameContext = "";
        private string _schoolName = "Okul Adı";
        private string _defaultExcelPath = "";
        private int _themeIndex = 0;
        private int _layoutIndex = 0;
        private double _fontSize = 14;
        private double _netCoefficient = 1.0;
        private double _baseScore = 0.0;
        private double _wrongDeductionFactor = 0.25;
        private bool _isBusy;
        private string _busyMessage = "";
        private string _newOutcomeName = "";
        private string _newOutcomeRange = "";
        private string _newOutcomeBooklet = "A";
        private string _searchText = "";
        private double? _minScoreFilter;
        private double? _maxScoreFilter;
        private string? _selectedBookletFilter;
        private int? _minCorrectFilter;
        private int? _maxWrongFilter;
        private string _updateStatus = "Güncelleme kontrolü yapılmadı";
        private bool _isCheckingForUpdate;
        private bool _hasUpdateAvailable;
        private string _latestVersion = "";
        private string _selectedSortColumn = "Score";
        private bool _isSortDescending = true;
        private bool _isAdvancedFilterOpen;
        private bool _isItemAnalysisOpen;
        private bool _isSuccessPredictionOpen;
        private bool _isValidationDetailsOpen = false;
        private bool _isSaveExamModalOpen;
        private bool _isDetailedAnswerKeyEditorOpen;
        private bool _isBulkAnswerEntryOpen;
        private AnswerKeyModel? _selectedAnswerKeyForEdit;
        private string _bulkAnswerEntryText = "";
        private string _bulkAnswerPreview = "";
        
        // Inline course management fields
        private bool _isAddCourseInlineOpen = false;
        private string _newCourseCodeInline = "";
        private string _newCourseNameInline = "";
        
        // Separate field for SaveExamModal dropdown (to avoid affecting sidebar)
        private Course? _selectedCourseForSave;
        
        private bool _isGeneralConfigOpen;
        private bool _hasUnsavedData;
        
        private ObservableCollection<ItemAnalysisService.QuestionItemStats> _questionStats = new();
        private ObservableCollection<ItemAnalysisService.AnomalyResult> _anomalies = new();
        private ItemAnalysisService.ReliabilityStats _reliabilityStats = new(0,0,0,0,0,0,0,0);
        
        private ObservableCollection<SuccessPredictionService.PredictionResult> _studentPredictions = new();
        private SuccessPredictionService.ClassPredictionSummary _classPredictionSummary = new();
        private double _passingScore = 50;

        // Student Detail Modal fields
        private bool _isStudentDetailOpen;
        private StudentResult? _selectedStudentDetail;
        private StudentResult? _originalStudentBeforeEdit;

        public ICollectionView StudentsView { get; }

        public MainViewModel()
        {
            _parserService = new OpticalParserService();
            _excelService = new ExcelExportService();
            _validationService = new ValidationService();
            _pdfService = new PdfReportService();
            _dbService = new DatabaseService();
            _settingsService = new AppSettingsService();
            _csvService = new CsvExportService();
            _notificationService = new NotificationService();
            _undoRedoManager = new UndoRedoManager(50);
            _statsReportService = new StatisticsReportService();
            _templateService = new TemplateService();
            _jsonDataService = new JsonDataService();
            _shortcutService = new KeyboardShortcutService();
            _progressService = ProgressService.Instance;
            _itemAnalysisService = new ItemAnalysisService();
            _successPredictionService = new SuccessPredictionService();
            _versionService = new VersionService();
            _versionCheckService = new VersionCheckService();
            _backupService = new BackupService(_dbService, _settingsService, _notificationService, _jsonDataService);

            // Undo/Redo event handlers
            _undoRedoManager.CanUndoChanged += (s, e) => { OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(UndoDescription)); };
            _undoRedoManager.CanRedoChanged += (s, e) => { OnPropertyChanged(nameof(CanRedo)); OnPropertyChanged(nameof(RedoDescription)); };
            _undoRedoManager.CommandExecuted += (s, desc) => AddToLog($"İşlem: {desc}", LogLevel.Info);

            // Kalıcı ayarları yükle
            var saved = _settingsService.Load();
            _schoolName = saved.SchoolName;
            _defaultExcelPath = saved.DefaultExcelPath;
            _netCoefficient = saved.NetCoefficient;
            _baseScore = saved.BaseScore;
            _wrongDeductionFactor = saved.WrongDeductionFactor;
            _themeIndex = saved.ThemeIndex;
            _layoutIndex = saved.LayoutIndex;
            _fontSize = saved.FontSize > 0 ? saved.FontSize : 14;
            _gridRowHeight = _layoutIndex == 0 ? 32 : 50;
            _gridCellPadding = _layoutIndex == 0
                ? new System.Windows.Thickness(10, 0, 10, 0)
                : new System.Windows.Thickness(15, 0, 15, 0);
            
            Students = new ObservableCollection<StudentResult>();
            AnswerKeys = new ObservableCollection<AnswerKeyModel>();
            Statistics = new ObservableCollection<QuestionStatisticItem>();
            ValidationIssues = new ObservableCollection<ValidationIssue>();
            Courses = new ObservableCollection<Course>();
            CourseExams = new ObservableCollection<ExamEntry>();
            
            StudentsView = CollectionViewSource.GetDefaultView(Students);
            StudentsView.Filter = FilterStudents;
            
            // Temayı uygula (saved ayarları yüklendikten sonra)
            ApplyTheme(_themeIndex == 1);
            ApplyFontSize(_fontSize);
            
            _initTask = InitializeAsync();

            AnswerKeys.Add(new AnswerKeyModel { BookletName = "A", Answers = "" });

            LoadTxtCommand = new RelayCommand(async _ => await LoadTxtFileAsync());
            EvaluateCommand = new AsyncRelayCommand(async _ => await EvaluateAsync(), _ => Students.Count > 0);
            
            AddToLog("Uygulama hazır. Lütfen bir optik veri (.txt) dosyası yükleyin.", LogLevel.Info);
            
            AddAnswerKeyCommand = new RelayCommand(_ => AnswerKeys.Add(new AnswerKeyModel { BookletName = "B", Answers = "" }));
            RemoveAnswerKeyCommand = new RelayCommand(param => {
                if (param is AnswerKeyModel model) AnswerKeys.Remove(model);
            });

            OpenModalCommand = new RelayCommand(_ => IsModalOpen = true);
            CloseModalCommand = new AsyncRelayCommand(async _ => { IsModalOpen = false; if (Students.Count > 0) { await EvaluateAsync(); await AutoSaveSelectedExamAsync(); } });
            OpenQuestionSettingsCommand = new RelayCommand(_ => {
                // Preserve existing settings that still apply
                var existingSettings = new Dictionary<(string, int), QuestionSetting>();
                foreach (var s in QuestionSettings)
                    existingSettings[(s.BookletName, s.QuestionNumber)] = s;
                QuestionSettings.Clear();
                foreach (var key in AnswerKeys.Where(k => !string.IsNullOrEmpty(k.Answers)))
                {
                    int maxLen = key.Answers.Length;
                    for (int i = 1; i <= maxLen; i++)
                    {
                        if (existingSettings.TryGetValue((key.BookletName, i), out var existing))
                            QuestionSettings.Add(existing);
                        else
                            QuestionSettings.Add(new QuestionSetting { BookletName = key.BookletName, QuestionNumber = i });
                    }
                }
                IsQuestionSettingsOpen = true;
                AddToLog("Soru ayarları paneli açıldı (Kitapçık bazlı).");
            });
            CloseQuestionSettingsCommand = new AsyncRelayCommand(async _ => { 
                IsQuestionSettingsOpen = false; 
                await EvaluateAsync(); 
                await AutoSaveSelectedExamAsync(); 
            });

            OpenLearningOutcomesCommand = new RelayCommand(_ => IsLearningOutcomesOpen = true);
            CloseLearningOutcomesCommand = new AsyncRelayCommand(async _ => {
                IsLearningOutcomesOpen = false;
                await EvaluateAsync();
                await AutoSaveSelectedExamAsync();
            });
            
            AddLearningOutcomeCommand = new RelayCommand(_ => 
            {
                if (!string.IsNullOrWhiteSpace(NewOutcomeRange) && !string.IsNullOrWhiteSpace(NewOutcomeName))
                {
                    var outcome = new LearningOutcome 
                    { 
                        Name = NewOutcomeName,
                        BookletName = NewOutcomeBooklet,
                        QuestionNumbersRaw = NewOutcomeRange
                    };
                    LearningOutcomes.Add(outcome);
                    
                    // Clear input fields
                    NewOutcomeName = string.Empty;
                    NewOutcomeRange = string.Empty;
                    NewOutcomeBooklet = "A";
                    
                    AddToLog($"Yeni kazanım eklendi: {outcome.Name} ({outcome.BookletName} kitapçığı)", LogLevel.Success);
                    
                    // Recalculate if we have students
                    if (Students.Count > 0)
                    {
                        UpdateOutcomeStats();
                    }
                }
                else
                {
                    ShowAlert("Eksik Bilgi", "Lütfen kazanım adı ve soru numaralarını girin.");
                }
            });
            RemoveOutcomeCommand = new RelayCommand(p => { if (p is LearningOutcome lo) LearningOutcomes.Remove(lo); });

            CloseAlertCommand = new RelayCommand(_ => IsAlertOpen = false);
            OpenAboutCommand = new RelayCommand(_ => IsAboutOpen = true);
            CloseAboutCommand = new RelayCommand(_ => IsAboutOpen = false);
            OpenGitHubCommand = new RelayCommand(_ => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                    FileName = "https://github.com/eekilinc",
                    UseShellExecute = true
                });
            });

            // Version Check Command
            CheckForUpdatesCommand = new AsyncRelayCommand(async _ => {
                IsCheckingForUpdate = true;
                UpdateStatus = "Kontrol ediliyor...";
                try
                {
                    var result = await _versionCheckService.CheckForUpdateAsync(CurrentVersion);
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        UpdateStatus = $"Hata: {result.ErrorMessage}";
                        HasUpdateAvailable = false;
                    }
                    else
                    {
                        HasUpdateAvailable = result.HasUpdate;
                        LatestVersion = result.LatestVersion;
                        UpdateStatus = result.StatusText;
                        
                        if (result.HasUpdate)
                        {
                            AddToLog($"Yeni sürüm mevcut: {result.LatestVersion}", LogLevel.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus = "Kontrol hatası";
                    AddToLog($"Güncelleme kontrolü hatası: {ex.Message}", LogLevel.Error);
                }
                finally
                {
                    IsCheckingForUpdate = false;
                }
            });

            OpenReleasesPageCommand = new RelayCommand(_ => {
                _versionCheckService.OpenReleasesPage();
            });
            OpenUISettingsCommand = new RelayCommand(_ => IsUISettingsOpen = true);
            CloseUISettingsCommand = new RelayCommand(_ => IsUISettingsOpen = false);
            OpenGeneralConfigCommand = new RelayCommand(_ => IsGeneralConfigOpen = true);
            CloseGeneralConfigCommand = new AsyncRelayCommand(async _ => { 
                IsGeneralConfigOpen = false; 
                await EvaluateAsync();
                await AutoSaveSelectedExamAsync(); 
            });

            SelectFolderCommand = new RelayCommand(_ => {
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Excel Dosyaları İçin Varsayılan Klasörü Seçin" };
                if (dialog.ShowDialog() == true) {
                    DefaultExcelPath = dialog.FolderName;
                }
            });

            AddToLog("Gelişmiş puanlama ve arama modülü aktif.", LogLevel.Info);

            ExitCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

            ExportExcelCommand = new AsyncRelayCommand(async _ => {
                if (Students.Count == 0) {
                    ShowAlert("Dışa Aktarma Hatası", "Dışa aktarılacak öğrenci kaydı bulunamadı.");
                    return;
                }
                
                string? filePath = null;
                if (!string.IsNullOrWhiteSpace(DefaultExcelPath) && System.IO.Directory.Exists(DefaultExcelPath)) {
                    filePath = System.IO.Path.Combine(DefaultExcelPath, $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                } else {
                    var sfd = new SaveFileDialog { Filter = "Excel Dosyası|*.xlsx", FileName = $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
                    if (sfd.ShowDialog() == true) filePath = sfd.FileName;
                }

                if (filePath != null) {
                    IsBusy = true;
                    BusyMessage = "Excel raporu oluşturuluyor...";
                    try {
                        var studentsCopy = new List<StudentResult>(Students);
                        var statsCopy = new List<QuestionStatisticItem>(Statistics);
                        var outcomesCopy = LearningOutcomes.ToList();
                        await Task.Run(() => _excelService.ExportToExcel(studentsCopy, statsCopy, outcomesCopy, filePath));
                        ShowAlert("Başarılı", $"Excel raporu kaydedildi:\n{filePath}");
                        AddToLog($"Excel raporu oluşturuldu: {System.IO.Path.GetFileName(filePath)}", LogLevel.Success);
                    } catch (Exception ex) {
                        ShowAlert("Hata", $"Excel oluşturulurken hata: {ex.Message}");
                    } finally {
                        IsBusy = false;
                    }
                }
            });

            ExportCsvCommand = new AsyncRelayCommand(async _ => {
                if (Students.Count == 0) {
                    ShowAlert("Dışa Aktarma Hatası", "Dışa aktarılacak öğrenci kaydı bulunamadı.");
                    return;
                }
                var sfd = new SaveFileDialog { 
                    Filter = "CSV Dosyası|*.csv", 
                    FileName = $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "CSV Olarak Kaydet"
                };
                if (sfd.ShowDialog() == true) {
                    IsBusy = true;
                    BusyMessage = "CSV dosyası oluşturuluyor...";
                    try {
                        var studentsCopy = new List<StudentResult>(Students);
                        await Task.Run(() => _csvService.ExportToCsv(studentsCopy, sfd.FileName));
                        StatusMessage = "CSV'ye aktarım tamamlandı.";
                        AddToLog($"CSV raporu oluşturuldu: {System.IO.Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    } catch (Exception ex) {
                        AddToLog($"CSV hatası: {ex.Message}", LogLevel.Error);
                        ShowAlert("Hata", $"CSV oluşturulurken hata: {ex.Message}");
                    } finally {
                        IsBusy = false;
                    }
                }
            });

            ExportPdfCommand = new AsyncRelayCommand(async _ => {
                if (Students.Count == 0) {
                    ShowToastError("Önce veri yüklemeniz ve puanları hesaplamanız gerekmektedir.");
                    return;
                }
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Öğrenci Karnelerinin Kaydedileceği Klasörü Seçin" };
                if (dialog.ShowDialog() == true) {
                    IsBusy = true;
                    BusyMessage = "PDF karneleri hazırlanıyor... (0%)";
                    
                    var progress = new Progress<PdfReportService.PdfProgressReport>(report => {
                        BusyMessage = $"PDF karneleri hazırlanıyor... ({report.Percentage:F0}%) - {report.CurrentStudentName}";
                    });
                    
                    try {
                        var studentsCopy = new List<StudentResult>(Students);
                        var outcomesCopy = LearningOutcomes.ToList();
                        string folder = dialog.FolderName;
                        
                        await _pdfService.GenerateStudentReportsAsync(studentsCopy, studentsCopy, outcomesCopy, folder, SchoolName, progress);
                        
                        ShowToastSuccess($"{Students.Count} adet öğrenci karnesi PDF olarak kaydedildi.");
                        AddToLog($"{Students.Count} adet öğrenci karnesi PDF olarak dışa aktarıldı.", LogLevel.Success);
                    } catch (Exception ex) {
                        ShowToastError($"PDF oluşturma hatası: {ex.Message}");
                        AddToLog($"PDF hatası: {ex.Message}", LogLevel.Error);
                    } finally {
                        IsBusy = false;
                    }
                }
            });

            ExportSinglePdfCommand = new AsyncRelayCommand(async p => {
                if (p is StudentResult student) {
                    var sfd = new SaveFileDialog { 
                        Filter = "PDF Dosyası|*.pdf", 
                        FileName = $"Karne_{student.StudentId}_{student.FullName.Replace(" ", "_")}.pdf",
                        Title = "Öğrenci Karnesini Kaydet"
                    };
                    if (sfd.ShowDialog() == true) {
                        IsBusy = true;
                        BusyMessage = $"{student.FullName} için karne hazırlanıyor...";
                        try {
                            var allStudentsCopy = new List<StudentResult>(Students);
                            var outcomesCopy = LearningOutcomes.ToList();
                            string dir = System.IO.Path.GetDirectoryName(sfd.FileName) ?? "";
                            await Task.Run(() => _pdfService.GenerateStudentReports(new List<StudentResult> { student }, allStudentsCopy, outcomesCopy, dir, SchoolName));
                            StatusMessage = $"{student.FullName} için karne oluşturuldu.";
                            AddToLog($"{student.FullName} için PDF karne oluşturuldu.", LogLevel.Success);
                        } catch (Exception ex) {
                            AddToLog($"Karne hatası: {ex.Message}", LogLevel.Error);
                            ShowAlert("Hata", $"Karne oluşturulurken hata oluştu: {ex.Message}");
                        } finally {
                            IsBusy = false;
                        }
                    }
                }
            });

            ShowAddCourseCommand = new RelayCommand(_ => IsAddCourseOpen = true);
            CloseAddCourseCommand = new RelayCommand(_ => IsAddCourseOpen = false);
            AddCourseCommand = new AsyncRelayCommand(async _ => {
                if (string.IsNullOrWhiteSpace(NewCourseName)) return;
                var c = new Course { Code = NewCourseCode, Name = NewCourseName };
                await _dbService.SaveCourseAsync(c);
                await LoadCoursesAsync();
                IsAddCourseOpen = false;
                NewCourseCode = ""; NewCourseName = "";
                AddToLog($"'{c.Name}' dersi eklendi.", LogLevel.Success);
            });
            DeleteCourseCommand = new AsyncRelayCommand(async obj => {
                if (obj is Course c) {
                    // Sınav var mı kontrol et
                    var exams = await _dbService.GetExamsForCourseAsync(c.Id);
                    if (exams.Any()) {
                        // Modern onay modalını göster
                        ConfirmModalTitle = "Ders Silme Onayı";
                        ConfirmModalMessage = $"'{c.Name}' dersine ait {exams.Count()} sınav bulunuyor.\n\nDersi silerseniz tüm sınavlar da silinecek.\n\nSilmek istediğinize emin misiniz?";
                        ConfirmButtonText = "Sil";
                        _onConfirmAction = async () => {
                            await ExecuteDeleteCourseAsync(c, exams);
                        };
                        IsConfirmModalOpen = true;
                        return;
                    }
                    
                    // Sınav yoksa direkt sil
                    await ExecuteDeleteCourseAsync(c, exams);
                }
            });
            DeleteExamCommand = new AsyncRelayCommand(async obj => {
                if (obj is ExamEntry e) {
                    await _dbService.DeleteExamAsync(e.Id);
                    
                    // Silinen sınavı CourseExams koleksiyonundan da kaldır
                    var examToRemove = CourseExams.FirstOrDefault(ex => ex.Id == e.Id);
                    if (examToRemove != null)
                        CourseExams.Remove(examToRemove);
                    
                    // Eğer silinen sınav seçiliyse veya başka sınav seçili değilse, tüm verileri temizle
                    if (SelectedExam?.Id == e.Id || SelectedExam == null)
                    {
                        SelectedExam = null;
                        Students.Clear();
                        AnswerKeys.Clear();
                        QuestionSettings.Clear();
                        LearningOutcomes.Clear();
                        Statistics.Clear();
                        ValidationIssues.Clear();
                        AccuracyData.Clear();
                        ScoreDistData.Clear();
                        HasUnsavedData = false;
                        StatusMessage = "Sınav silindi. Yeni veri yükleyin.";
                        
                        // Varsayılan cevap anahtarını geri ekle
                        AnswerKeys.Add(new AnswerKeyModel { BookletName = "A", Answers = "" });
                    }
                    
                    OnPropertyChanged(nameof(CourseExams));
                    AddToLog($"'{e.Title}' sınavı silindi.", LogLevel.Warning);
                }
            });
            SaveExamCommand = new AsyncRelayCommand(async _ => {
                // Yeni modal'ı aç
                IsSaveExamModalOpen = true;
            });

            OpenSaveExamModalCommand = new RelayCommand(_ => {
                IsSaveExamModalOpen = true;
                // Initialize with current selected course to avoid null
                SelectedCourseForSave = SelectedCourse;
            });

            CloseSaveExamModalCommand = new RelayCommand(_ => {
                IsSaveExamModalOpen = false;
            });

            ConfirmSaveExamCommand = new AsyncRelayCommand(async _ => {
                string title = string.IsNullOrWhiteSpace(NewExamName) ? $"{DateTime.Now:dd.MM.yyyy} Sınavı" : NewExamName;
                
                // Use SelectedCourseForSave for saving, fallback to SelectedCourse if null
                var courseToUse = SelectedCourseForSave ?? SelectedCourse;
                if (courseToUse == null)
                {
                    ShowToastError("Lütfen bir ders seçin!");
                    return;
                }
                
                // Pass course directly - DON'T change SelectedCourse to avoid clearing Students
                await SaveCurrentExamAsync(title, courseToUse, SelectedExam);
                
                IsSaveExamModalOpen = false;
                NewExamName = ""; // Reset
                SelectedCourseForSave = null; // Reset
            });

            // Inline Course Management Commands
            ToggleAddCourseInlineCommand = new RelayCommand(_ => {
                IsAddCourseInlineOpen = !IsAddCourseInlineOpen;
                if (!IsAddCourseInlineOpen)
                {
                    NewCourseCodeInline = "";
                    NewCourseNameInline = "";
                }
            });

            AddCourseInlineCommand = new AsyncRelayCommand(async _ => {
                if (string.IsNullOrWhiteSpace(NewCourseNameInline)) {
                    ShowToastError("Ders adı girmelisiniz!");
                    return;
                }
                var c = new Course { Code = NewCourseCodeInline, Name = NewCourseNameInline };
                await _dbService.SaveCourseAsync(c);
                await LoadCoursesAsync();
                IsAddCourseInlineOpen = false;
                NewCourseCodeInline = "";
                NewCourseNameInline = "";
                ShowToastSuccess($"'{c.Name}' dersi eklendi!");
                AddToLog($"Yeni ders eklendi: {c.Name}", LogLevel.Success);
            });

            DeleteSelectedCourseInlineCommand = new AsyncRelayCommand(async _ => {
                var courseToDelete = SelectedCourseForSave ?? SelectedCourse;
                if (courseToDelete == null) {
                    ShowToastError("Silinecek ders seçilmedi!");
                    return;
                }
                
                // Sınav var mı kontrol et
                var exams = await _dbService.GetExamsForCourseAsync(courseToDelete.Id);
                if (exams.Any()) {
                    // Modern onay modalını göster
                    ConfirmModalTitle = "Ders Silme Onayı";
                    ConfirmModalMessage = $"'{courseToDelete.Name}' dersine ait {exams.Count()} sınav bulunuyor.\n\nDersi silerseniz tüm sınavlar da silinecek.\n\nSilmek istediğinize emin misiniz?";
                    ConfirmButtonText = "Sil";
                    _onConfirmAction = async () => {
                        await ExecuteDeleteCourseAsync(courseToDelete, exams);
                        // Only clear selections if deleted course matches current selections
                        if (SelectedCourseForSave?.Id == courseToDelete.Id) SelectedCourseForSave = null;
                    };
                    IsConfirmModalOpen = true;
                    return;
                }
                
                // Sınav yoksa direkt sil
                await ExecuteDeleteCourseAsync(courseToDelete, exams);
                if (SelectedCourseForSave?.Id == courseToDelete.Id) SelectedCourseForSave = null;
            });

            OpenRenameCourseCommand = new RelayCommand(obj => {
                if (obj is Course c) {
                    _renameContext = "Course";
                    _renameId = c.Id;
                    RenameModalTitle = "Dersi Düzenle";
                    RenameInput1 = c.Code;
                    RenameInput2 = c.Name;
                    ShowRenameInput2 = true;
                    IsRenameModalOpen = true;
                }
            });
            OpenRenameExamCommand = new RelayCommand(obj => {
                if (obj is ExamEntry e) {
                    _renameContext = "Exam";
                    _renameId = e.Id;
                    RenameModalTitle = "Sınavı Düzenle";
                    RenameInput1 = e.Title;
                    ShowRenameInput2 = false;
                    IsRenameModalOpen = true;
                }
            });
            CloseRenameModalCommand = new RelayCommand(_ => IsRenameModalOpen = false);
            SaveRenameCommand = new AsyncRelayCommand(async _ => {
                if (_renameContext == "Course") {
                    if (string.IsNullOrWhiteSpace(RenameInput1) || string.IsNullOrWhiteSpace(RenameInput2)) return;
                    await _dbService.RenameCourseAsync(_renameId, RenameInput1, RenameInput2);
                    await LoadCoursesAsync();
                    if (SelectedCourse != null && SelectedCourse.Id == _renameId) {
                        SelectedCourse.Code = RenameInput1;
                        SelectedCourse.Name = RenameInput2;
                        OnPropertyChanged(nameof(SelectedCourse));
                    }
                    AddToLog($"Ders düzenlendi: {RenameInput2}", LogLevel.Success);
                } else if (_renameContext == "Exam") {
                    if (string.IsNullOrWhiteSpace(RenameInput1)) return;
                    await _dbService.RenameExamAsync(_renameId, RenameInput1);
                    if (SelectedCourse != null) await LoadExamsForCourseAsync(SelectedCourse.Id);
                    if (SelectedExam != null && SelectedExam.Id == _renameId) {
                        SelectedExam.Title = RenameInput1;
                        OnPropertyChanged(nameof(SelectedExam));
                    }
                    AddToLog($"Sınav yeniden adlandırıldı: {RenameInput1}", LogLevel.Success);
                }
                IsRenameModalOpen = false;
            });

            // Confirmation Modal Commands
            CancelConfirmCommand = new RelayCommand(_ => {
                IsConfirmModalOpen = false;
                _onConfirmAction = null;
            });
            ConfirmActionCommand = new AsyncRelayCommand(async _ => {
                IsConfirmModalOpen = false;
                if (_onConfirmAction != null)
                {
                    var action = _onConfirmAction;
                    _onConfirmAction = null;
                    action();
                }
            });

            ShowShortcutsCommand = new RelayCommand(_ => IsShortcutsOpen = true);
            CloseShortcutsCommand = new RelayCommand(_ => IsShortcutsOpen = false);

            // Shortcut Settings Commands
            OpenShortcutSettingsCommand = new RelayCommand(_ => IsShortcutSettingsOpen = true);
            CloseShortcutSettingsCommand = new RelayCommand(_ => IsShortcutSettingsOpen = false);
            EditShortcutCommand = new RelayCommand(param => {
                // TODO: Implement shortcut editing dialog
                ShowToastInfo("Kısayol düzenleme yakında gelecek!");
            });
            ResetShortcutsCommand = new RelayCommand(_ => {
                _shortcutService.ResetToDefaults();
                ShowToastSuccess("Kısayollar varsayılana sıfırlandı!");
            });

            ExportGradeListCommand = new RelayCommand(_ =>
            {
                if (Students.Count == 0) {
                    ShowAlert("Dışa Aktarma Hatası", "Dışa aktarılacak öğrenci kaydı bulunamadı.");
                    return;
                }
                var sfd = new SaveFileDialog {
                    Filter = "CSV Dosyası|*.csv",
                    FileName = $"NotListesi_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    Title = "Not Listesini Kaydet"
                };
                if (sfd.ShowDialog() == true) {
                    try {
                        _csvService.ExportGradeList(new System.Collections.Generic.List<StudentResult>(Students), sfd.FileName);
                        StatusMessage = "Not listesi CSV olarak aktarıldı.";
                        AddToLog($"Not listesi oluşturuldu: {System.IO.Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    } catch (Exception ex) {
                        AddToLog($"Not listesi hatası: {ex.Message}", LogLevel.Error);
                        ShowAlert("Hata", $"Not listesi oluşturulurken hata: {ex.Message}");
                    }
                }
            });

            ApplyTheme(false);

            // Toast Notification Commands
            DismissNotificationCommand = new RelayCommand(param => {
                if (param is string notificationId) {
                    _notificationService.Dismiss(notificationId);
                }
            });

            // Progress Commands
            CancelProgressCommand = new RelayCommand(_ => _progressService.CancelOperation());

            // Undo/Redo Commands
            UndoCommand = new RelayCommand(_ => {
                if (_undoRedoManager.CanUndo)
                {
                    _undoRedoManager.Undo();
                    ShowToastInfo("İşlem geri alındı");
                    EvaluateAsync().ConfigureAwait(false);
                }
            }, _ => CanUndo);

            RedoCommand = new RelayCommand(_ => {
                if (_undoRedoManager.CanRedo)
                {
                    _undoRedoManager.Redo();
                    ShowToastInfo("İşlem yenilendi");
                    EvaluateAsync().ConfigureAwait(false);
                }
            }, _ => CanRedo);

            CopyReportCommand = new RelayCommand(_ => {
                if (!string.IsNullOrEmpty(ReportText))
                {
                    System.Windows.Clipboard.SetText(ReportText);
                    ShowToastSuccess("Rapor panoya kopyalandı!");
                }
            });

            OpenTemplateManagerCommand = new RelayCommand(_ => {
                RefreshTemplates();
                IsTemplateManagerOpen = true;
                OnPropertyChanged(nameof(IsTemplateManagerOpen));
            });

            CloseTemplateManagerCommand = new RelayCommand(_ => {
                IsTemplateManagerOpen = false;
                OnPropertyChanged(nameof(IsTemplateManagerOpen));
            });

            SaveAsTemplateCommand = new RelayCommand(_ => {
                if (string.IsNullOrWhiteSpace(NewTemplateName))
                {
                    ShowToastError("Şablon adı girmelisiniz!");
                    return;
                }

                var template = _templateService.CreateFromCurrent(
                    NewTemplateName,
                    NewTemplateDescription,
                    AnswerKeys,
                    QuestionSettings,
                    LearningOutcomes,
                    NetCoefficient,
                    BaseScore,
                    WrongDeductionFactor,
                    SchoolName
                );

                _templateService.SaveTemplate(template);
                RefreshTemplates();
                
                NewTemplateName = "";
                NewTemplateDescription = "";
                OnPropertyChanged(nameof(NewTemplateName));
                OnPropertyChanged(nameof(NewTemplateDescription));
                
                ShowToastSuccess("Şablon başarıyla kaydedildi!");
            });

            LoadTemplateCommand = new RelayCommand(param => {
                if (param is string templateId)
                {
                    var template = _templateService.LoadTemplate(templateId);
                    if (template != null)
                    {
                        AnswerKeys.Clear();
                        foreach (var key in template.AnswerKeys) AnswerKeys.Add(key);

                        QuestionSettings.Clear();
                        foreach (var setting in template.QuestionSettings) QuestionSettings.Add(setting);

                        LearningOutcomes.Clear();
                        foreach (var outcome in template.LearningOutcomes) LearningOutcomes.Add(outcome);

                        NetCoefficient = template.NetCoefficient;
                        BaseScore = template.BaseScore;
                        WrongDeductionFactor = template.WrongDeductionFactor;
                        SchoolName = template.SchoolName;

                        OnPropertyChanged(nameof(NetCoefficient));
                        OnPropertyChanged(nameof(BaseScore));
                        OnPropertyChanged(nameof(WrongDeductionFactor));
                        OnPropertyChanged(nameof(SchoolName));

                        IsTemplateManagerOpen = false;
                        OnPropertyChanged(nameof(IsTemplateManagerOpen));

                        ShowToastSuccess($"'{template.Name}' şablonu yüklendi!");
                        AddToLog($"Şablon yüklendi: {template.Name}", LogLevel.Success);
                    }
                }
            });

            DeleteTemplateCommand = new RelayCommand(param => {
                if (param is string templateId)
                {
                    _templateService.DeleteTemplate(templateId);
                    RefreshTemplates();
                    ShowToastInfo("Şablon silindi.");
                }
            });

            ExportTemplateCommand = new RelayCommand(_ => {
                var dialog = new SaveFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    FileName = $"SınavŞablonu_{DateTime.Now:yyyyMMdd}.json"
                };
                if (dialog.ShowDialog() == true)
                {
                    // Export current settings as template
                    var template = _templateService.CreateFromCurrent(
                        "Dışa Aktarılan Şablon",
                        "",
                        AnswerKeys,
                        QuestionSettings,
                        LearningOutcomes,
                        NetCoefficient,
                        BaseScore,
                        WrongDeductionFactor,
                        SchoolName
                    );
                    _templateService.SaveTemplate(template);
                    _templateService.ExportTemplate(template.Id, dialog.FileName);
                    ShowToastSuccess("Şablon dışa aktarıldı!");
                }
            });

            ImportTemplateCommand = new RelayCommand(_ => {
                var dialog = new OpenFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    Title = "Şablon Dosyası Seçin"
                };
                if (dialog.ShowDialog() == true)
                {
                    var template = _templateService.ImportTemplate(dialog.FileName);
                    if (template != null)
                    {
                        RefreshTemplates();
                        ShowToastSuccess($"'{template.Name}' şablonu içe aktarıldı!");
                    }
                    else
                    {
                        ShowToastError("Şablon içe aktarılamadı!");
                    }
                }
            });

            // JSON Export/Import Commands
            ExportJsonCommand = new AsyncRelayCommand(async _ => {
                if (Students.Count == 0)
                {
                    ShowToastError("Dışa aktarılacak veri yok!");
                    return;
                }

                var dialog = new SaveFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    FileName = $"Sınav_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    BusyMessage = "JSON dosyası oluşturuluyor...";
                    try
                    {
                        await _jsonDataService.ExportToJsonAsync(
                            dialog.FileName,
                            SelectedExam?.Title ?? "Dışa Aktarılan Sınav",
                            SelectedCourse?.Name ?? "",
                            SchoolName,
                            NetCoefficient,
                            BaseScore,
                            WrongDeductionFactor,
                            AnswerKeys,
                            QuestionSettings,
                            LearningOutcomes,
                            Students
                        );
                        ShowToastSuccess("Veriler JSON olarak dışa aktarıldı!");
                        AddToLog($"JSON dışa aktarma: {Path.GetFileName(dialog.FileName)}", LogLevel.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"Dışa aktarma hatası: {ex.Message}");
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            });

            ImportJsonCommand = new AsyncRelayCommand(async _ => {
                var dialog = new OpenFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    Title = "Sınav Verisi JSON Dosyası Seçin"
                };
                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    BusyMessage = "JSON dosyası okunuyor...";
                    try
                    {
                        var data = await _jsonDataService.ImportFromJsonAsync(dialog.FileName);
                        if (data == null)
                        {
                            ShowToastError("JSON dosyası okunamadı veya geçersiz format!");
                            return;
                        }

                        // Clear existing data
                        SelectedExam = null;
                        Students.Clear();
                        AnswerKeys.Clear();
                        QuestionSettings.Clear();
                        LearningOutcomes.Clear();

                        // Load new data
                        foreach (var key in data.AnswerKeys) AnswerKeys.Add(key);
                        foreach (var setting in data.QuestionSettings) QuestionSettings.Add(setting);
                        foreach (var outcome in data.LearningOutcomes) LearningOutcomes.Add(outcome);
                        foreach (var student in data.Students) Students.Add(student);

                        NetCoefficient = data.NetCoefficient;
                        BaseScore = data.BaseScore;
                        WrongDeductionFactor = data.WrongDeductionFactor;
                        SchoolName = data.SchoolName;

                        OnPropertyChanged(nameof(NetCoefficient));
                        OnPropertyChanged(nameof(BaseScore));
                        OnPropertyChanged(nameof(WrongDeductionFactor));
                        OnPropertyChanged(nameof(SchoolName));

                        HasUnsavedData = true;
                        ShowToastSuccess($"'{data.ExamTitle}' JSON'dan yüklendi! Öğrenci: {data.Students.Count}");
                        AddToLog($"JSON içe aktarma: {data.ExamTitle} ({data.Students.Count} öğrenci)", LogLevel.Success);

                        await EvaluateAsync();
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"İçe aktarma hatası: {ex.Message}");
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            });

            // Backup & Restore Commands
            CreateFullBackupCommand = new AsyncRelayCommand(async _ => {
                var sfd = new SaveFileDialog {
                    Filter = "ZIP Dosyası|*.zip",
                    FileName = $"OptikBackup_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
                    Title = "Yedek Dosyasını Kaydet"
                };
                if (sfd.ShowDialog() == true)
                {
                    IsBusy = true;
                    BusyMessage = "Tam yedek oluşturuluyor...";
                    try
                    {
                        await _backupService.CreateFullBackupAsync(sfd.FileName);
                        ShowToastSuccess("Yedekleme tamamlandı!");
                        AddToLog($"Tam yedek oluşturuldu: {Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"Yedekleme hatası: {ex.Message}");
                        AddToLog($"Yedekleme hatası: {ex.Message}", LogLevel.Error);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            });

            CreateDatabaseBackupCommand = new AsyncRelayCommand(async _ => {
                var sfd = new SaveFileDialog {
                    Filter = "SQLite Database|*.db",
                    FileName = $"OptikDB_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Veritabanı Yedeğini Kaydet"
                };
                if (sfd.ShowDialog() == true)
                {
                    IsBusy = true;
                    BusyMessage = "Veritabanı yedeği oluşturuluyor...";
                    try
                    {
                        await _backupService.CreateDatabaseBackupAsync(sfd.FileName);
                        ShowToastSuccess("Veritabanı yedeği oluşturuldu!");
                        AddToLog($"Veritabanı yedeği: {Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"Yedekleme hatası: {ex.Message}");
                        AddToLog($"Veritabanı yedekleme hatası: {ex.Message}", LogLevel.Error);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            });

            ExportSettingsCommand = new AsyncRelayCommand(async _ => {
                var sfd = new SaveFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    FileName = $"OptikSettings_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                    Title = "Ayarları Dışa Aktar"
                };
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        await _backupService.ExportSettingsAsync(sfd.FileName);
                        ShowToastSuccess("Ayarlar dışa aktarıldı!");
                        AddToLog($"Ayarlar dışa aktarıldı: {Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"Aktarma hatası: {ex.Message}");
                    }
                }
            });

            RestoreFullBackupCommand = new AsyncRelayCommand(async _ => {
                var ofd = new OpenFileDialog {
                    Filter = "ZIP Yedek Dosyası|*.zip",
                    Title = "Yedek Dosyası Seçin"
                };
                if (ofd.ShowDialog() == true)
                {
                    ConfirmModalTitle = "Yedek Geri Yükleme";
                    ConfirmModalMessage = "Mevcut verilerin üzerine yazılacak.\n\nDevam etmek istediğinize emin misiniz?";
                    ConfirmButtonText = "Geri Yükle";
                    _onConfirmAction = async () => {
                        IsBusy = true;
                        BusyMessage = "Yedek geri yükleniyor...";
                        try
                        {
                            await _backupService.RestoreFullBackupAsync(ofd.FileName, true);
                            await LoadCoursesAsync();
                            ShowToastSuccess("Yedek geri yüklendi! Lütfen uygulamayı yeniden başlatın.");
                            AddToLog($"Yedek geri yüklendi: {Path.GetFileName(ofd.FileName)}", LogLevel.Success);
                        }
                        catch (Exception ex)
                        {
                            ShowToastError($"Geri yükleme hatası: {ex.Message}");
                            AddToLog($"Geri yükleme hatası: {ex.Message}", LogLevel.Error);
                        }
                        finally
                        {
                            IsBusy = false;
                        }
                    };
                    IsConfirmModalOpen = true;
                }
            });

            RestoreDatabaseCommand = new AsyncRelayCommand(async _ => {
                var ofd = new OpenFileDialog {
                    Filter = "SQLite Database|*.db",
                    Title = "Veritabanı Yedeği Seçin"
                };
                if (ofd.ShowDialog() == true)
                {
                    ConfirmModalTitle = "Veritabanı Geri Yükleme";
                    ConfirmModalMessage = "Mevcut veritabanı değiştirilecek.\n\nDevam etmek istediğinize emin misiniz?";
                    ConfirmButtonText = "Geri Yükle";
                    _onConfirmAction = async () => {
                        IsBusy = true;
                        BusyMessage = "Veritabanı geri yükleniyor...";
                        try
                        {
                            await _backupService.RestoreDatabaseAsync(ofd.FileName);
                            await LoadCoursesAsync();
                            ShowToastSuccess("Veritabanı geri yüklendi!");
                            AddToLog($"Veritabanı geri yüklendi: {Path.GetFileName(ofd.FileName)}", LogLevel.Success);
                        }
                        catch (Exception ex)
                        {
                            ShowToastError($"Geri yükleme hatası: {ex.Message}");
                            AddToLog($"Veritabanı geri yükleme hatası: {ex.Message}", LogLevel.Error);
                        }
                        finally
                        {
                            IsBusy = false;
                        }
                    };
                    IsConfirmModalOpen = true;
                }
            });

            ImportSettingsCommand = new AsyncRelayCommand(async _ => {
                var ofd = new OpenFileDialog {
                    Filter = "JSON Dosyası|*.json",
                    Title = "Ayar Dosyası Seçin"
                };
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        await _backupService.ImportSettingsAsync(ofd.FileName);
                        // Ayarları yeniden yükle
                        var saved = _settingsService.Load();
                        SchoolName = saved.SchoolName;
                        NetCoefficient = saved.NetCoefficient;
                        BaseScore = saved.BaseScore;
                        WrongDeductionFactor = saved.WrongDeductionFactor;
                        ThemeIndex = saved.ThemeIndex;
                        FontSize = saved.FontSize > 0 ? saved.FontSize : 14;
                        ApplyTheme(ThemeIndex == 1);
                        ApplyFontSize(FontSize);
                        ShowToastSuccess("Ayarlar içe aktarıldı!");
                    }
                    catch (Exception ex)
                    {
                        ShowToastError($"Ayar içe aktarma hatası: {ex.Message}");
                    }
                }
            });

            // Item Analysis Commands
            OpenItemAnalysisCommand = new RelayCommand(_ => {
                IsItemAnalysisOpen = true;
                RunItemAnalysisAsync().ConfigureAwait(false);
            });
            CloseItemAnalysisCommand = new RelayCommand(_ => IsItemAnalysisOpen = false);
            RunItemAnalysisCommand = new AsyncRelayCommand(async _ => await RunItemAnalysisAsync());

            // Advanced Filter Commands
            OpenAdvancedFilterCommand = new RelayCommand(_ => IsAdvancedFilterOpen = true);
            CloseAdvancedFilterCommand = new RelayCommand(_ => IsAdvancedFilterOpen = false);
            ClearFiltersCommand = new RelayCommand(_ => ClearAdvancedFilters());
            ToggleSortDirectionCommand = new RelayCommand(_ => { IsSortDescending = !IsSortDescending; });

            // Success Prediction Commands
            OpenSuccessPredictionCommand = new RelayCommand(_ => {
                IsSuccessPredictionOpen = true;
                RunSuccessPredictionAsync().ConfigureAwait(false);
            });
            CloseSuccessPredictionCommand = new RelayCommand(_ => IsSuccessPredictionOpen = false);
            RunSuccessPredictionCommand = new AsyncRelayCommand(async _ => await RunSuccessPredictionAsync());

            // Student Detail Commands
            OpenStudentDetailCommand = new RelayCommand(obj => {
                if (obj is StudentResult student)
                {
                    // Store reference to original student
                    _originalStudentBeforeEdit = student;
                    
                    // Create editable COPY - changes here won't affect original until saved
                    SelectedStudentDetail = new StudentResult
                    {
                        StudentId = student.StudentId,
                        FullName = student.FullName,
                        BookletType = student.BookletType,
                        RawAnswers = student.RawAnswers,
                        Answers = new List<string>(student.Answers),
                        Score = student.Score,
                        CorrectCount = student.CorrectCount,
                        IncorrectCount = student.IncorrectCount,
                        EmptyCount = student.EmptyCount,
                        NetCount = student.NetCount,
                        Rank = student.Rank,
                        RowNumber = student.RowNumber,
                        QuestionResults = new List<bool>(student.QuestionResults),
                        ColoredAnswers = new ObservableCollection<AnswerItem>(student.ColoredAnswers.Select(a => new AnswerItem { Character = a.Character, State = a.State, QuestionNumber = a.QuestionNumber }))
                    };
                    
                    IsStudentDetailOpen = true;
                    AddToLog($"Öğrenci detayı açıldı: {student.FullName} ({student.StudentId})", LogLevel.Info);
                }
            });
            CloseStudentDetailCommand = new RelayCommand(_ => {
                // Discard changes - just close without applying to original
                IsStudentDetailOpen = false;
                _originalStudentBeforeEdit = null;
                SelectedStudentDetail = null;
            });
            SaveStudentDetailCommand = new AsyncRelayCommand(async _ => {
                if (SelectedStudentDetail != null && _originalStudentBeforeEdit != null)
                {
                    // Apply changes to the ORIGINAL student
                    _originalStudentBeforeEdit.StudentId = SelectedStudentDetail.StudentId;
                    _originalStudentBeforeEdit.FullName = SelectedStudentDetail.FullName;
                    
                    // Add to undo stack
                    var command = new StudentDataChangeCommand(
                        new List<StudentResult>(Students),
                        new List<StudentResult>(Students),
                        $"Öğrenci düzenlendi: {SelectedStudentDetail.FullName}"
                    );
                    _undoRedoManager.ExecuteCommand(command);
                    
                    AddToLog($"Öğrenci bilgileri güncellendi: {SelectedStudentDetail.FullName} ({SelectedStudentDetail.StudentId})", LogLevel.Success);
                    ShowToastSuccess("Öğrenci bilgileri güncellendi!");
                    
                    // Refresh the view
                    StudentsView.Refresh();
                    
                    // Auto-save if exam is selected
                    if (SelectedExam != null)
                    {
                        await AutoSaveSelectedExamAsync();
                    }
                }
                IsStudentDetailOpen = false;
                _originalStudentBeforeEdit = null;
                SelectedStudentDetail = null;
            });

            // Validation Details Commands
            ShowValidationDetailsCommand = new RelayCommand(_ => IsValidationDetailsOpen = true);
            CloseValidationDetailsCommand = new RelayCommand(_ => IsValidationDetailsOpen = false);
            CopyValidationErrorsCommand = new RelayCommand(_ => {
                var errorText = string.Join("\n", ValidationIssues.Select(i => $"{i.Title}: {i.Message} (Etkilenen: {i.AffectedItems})"));
                System.Windows.Clipboard.SetText(errorText);
                ShowToastSuccess("Hatalar panoya kopyalandı!");
            });

            // Detailed Answer Key Editor Commands
            OpenDetailedAnswerKeyEditorCommand = new RelayCommand(_ => {
                // Select first answer key if none selected
                if (SelectedAnswerKeyForEdit == null && AnswerKeys.Count > 0)
                    SelectedAnswerKeyForEdit = AnswerKeys[0];
                
                // Sync details from answers string
                SelectedAnswerKeyForEdit?.SyncDetailsFromAnswers();
                
                IsDetailedAnswerKeyEditorOpen = true;
                AddToLog("Detaylı cevap anahtarı düzenleyici açıldı.");
            });
            
            // Open detailed editor from BookletSettingsModal
            OpenDetailedAnswerKeyEditorFromBookletCommand = new RelayCommand(param => {
                if (param is AnswerKeyModel key)
                {
                    SelectedAnswerKeyForEdit = key;
                    SelectedAnswerKeyForEdit?.SyncDetailsFromAnswers();
                    IsDetailedAnswerKeyEditorOpen = true;
                    AddToLog($"Detaylı düzenleyici açıldı: Kitapçık {key.BookletName}");
                }
            });
            
            CloseDetailedAnswerKeyEditorCommand = new AsyncRelayCommand(async _ => { 
                // Sync answers from details back to string
                SelectedAnswerKeyForEdit?.SyncAnswersFromDetails();
                IsDetailedAnswerKeyEditorOpen = false; 
                if (Students.Count > 0) { 
                    await EvaluateAsync(); 
                    await AutoSaveSelectedExamAsync(); 
                }
            });
            
            // Add question to answer key
            AddQuestionToAnswerKeyCommand = new RelayCommand(_ => {
                if (SelectedAnswerKeyForEdit == null) return;
                
                int nextQuestionNumber = SelectedAnswerKeyForEdit.AnswerDetails.Count + 1;
                SelectedAnswerKeyForEdit.AnswerDetails.Add(new AnswerDetailItem
                {
                    QuestionNumber = nextQuestionNumber,
                    Answer = "A" // Default answer
                });
                
                // Sync to string
                SelectedAnswerKeyForEdit.SyncAnswersFromDetails();
                
                OnPropertyChanged(nameof(SelectedAnswerKeyForEdit));
                AddToLog($"Yeni soru eklendi: Soru {nextQuestionNumber}");
            });
            
            // Remove last question from answer key
            RemoveLastQuestionFromAnswerKeyCommand = new RelayCommand(_ => {
                if (SelectedAnswerKeyForEdit == null || SelectedAnswerKeyForEdit.AnswerDetails.Count == 0) return;
                
                var lastItem = SelectedAnswerKeyForEdit.AnswerDetails.Last();
                SelectedAnswerKeyForEdit.AnswerDetails.Remove(lastItem);
                
                // Re-number questions
                for (int i = 0; i < SelectedAnswerKeyForEdit.AnswerDetails.Count; i++)
                {
                    SelectedAnswerKeyForEdit.AnswerDetails[i].QuestionNumber = i + 1;
                }
                
                // Sync to string
                SelectedAnswerKeyForEdit.SyncAnswersFromDetails();
                
                OnPropertyChanged(nameof(SelectedAnswerKeyForEdit));
                AddToLog($"Son soru silindi: Kalan {SelectedAnswerKeyForEdit.AnswerDetails.Count} soru");
            });
            
            AnalyzeAnswerKeyCommand = new RelayCommand(_ => {
                if (SelectedAnswerKeyForEdit == null || Students.Count == 0) return;
                
                // Calculate statistics for each question
                foreach (var detail in SelectedAnswerKeyForEdit.AnswerDetails)
                {
                    int qIndex = detail.QuestionNumber - 1;
                    int correct = 0, wrong = 0, empty = 0;
                    
                    foreach (var student in Students.Where(s => s.BookletType == SelectedAnswerKeyForEdit.BookletName))
                    {
                        if (qIndex < student.QuestionResults.Count)
                        {
                            if (student.QuestionResults[qIndex]) correct++;
                            else if (qIndex < student.Answers.Count && 
                                     (string.IsNullOrEmpty(student.Answers[qIndex]) || student.Answers[qIndex] == " "))
                                empty++;
                            else
                                wrong++;
                        }
                    }
                    
                    detail.CorrectCount = correct;
                    detail.WrongCount = wrong;
                    detail.EmptyCount = empty;
                    detail.DifficultyIndex = Students.Count > 0 ? (double)correct / Students.Count : 0;
                }
                
                OnPropertyChanged(nameof(SelectedAnswerKeyForEdit));
                OnPropertyChanged(nameof(SelectedAnswerKeyStats));
                AddToLog($"Cevap anahtarı analizi tamamlandı: {SelectedAnswerKeyForEdit.BookletName} kitapçığı");
                ShowToastSuccess("Cevap anahtarı analizi tamamlandı!");
            });
            
            OpenBulkAnswerEntryCommand = new RelayCommand(_ => {
                IsBulkAnswerEntryOpen = true;
            });
            
            CloseBulkAnswerEntryCommand = new RelayCommand(_ => {
                IsBulkAnswerEntryOpen = false;
                BulkAnswerEntryText = "";
                BulkAnswerPreview = "";
            });
            
            ApplyBulkAnswerEntryCommand = new RelayCommand(_ => {
                if (SelectedAnswerKeyForEdit == null || string.IsNullOrWhiteSpace(BulkAnswerEntryText)) return;
                
                var lines = BulkAnswerEntryText.Split('\n', '\r')
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim())
                    .ToList();
                
                var parsedAnswers = new Dictionary<int, string>();
                
                foreach (var line in lines)
                {
                    // Parse "1 A" or "5-C" format
                    var parts = line.Split(' ', '-', '.', ')');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0], out int qNum))
                        {
                            var answer = parts[1].ToUpper().Trim();
                            if (answer.Length > 0 && char.IsLetter(answer[0]))
                            {
                                parsedAnswers[qNum] = answer[0].ToString();
                            }
                        }
                    }
                }
                
                // Apply to answer details
                foreach (var kvp in parsedAnswers)
                {
                    var detail = SelectedAnswerKeyForEdit.AnswerDetails.FirstOrDefault(d => d.QuestionNumber == kvp.Key);
                    if (detail != null)
                    {
                        detail.Answer = kvp.Value;
                    }
                }
                
                // Sync to main answers string
                SelectedAnswerKeyForEdit.SyncAnswersFromDetails();
                
                IsBulkAnswerEntryOpen = false;
                BulkAnswerEntryText = "";
                ShowToastSuccess($"{parsedAnswers.Count} cevap uygulandı!");
                AddToLog($"Toplu cevap girişi: {parsedAnswers.Count} cevap uygulandı.");
            });
        }

        private void UpdateBulkPreview()
        {
            var lines = BulkAnswerEntryText.Split('\n', '\r')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .ToList();
            
            int count = 0;
            foreach (var line in lines)
            {
                var parts = line.Split(' ', '-', '.', ')');
                if (parts.Length >= 2 && int.TryParse(parts[0], out _))
                    count++;
            }
            
            BulkAnswerPreview = count > 0 ? $"{count} geçerli giriş algılandı" : "Henüz geçerli giriş yok";
        }

        public string SearchText 
        { 
            get => _searchText; 
            set { _searchText = value; OnPropertyChanged(); StudentsView.Refresh(); } 
        }

        // Advanced Filtering Properties
        public bool IsAdvancedFilterOpen { get => _isAdvancedFilterOpen; set { _isAdvancedFilterOpen = value; OnPropertyChanged(); } }
        public double? MinScoreFilter { get => _minScoreFilter; set { _minScoreFilter = value; OnPropertyChanged(); StudentsView.Refresh(); } }
        public double? MaxScoreFilter { get => _maxScoreFilter; set { _maxScoreFilter = value; OnPropertyChanged(); StudentsView.Refresh(); } }
        public string? SelectedBookletFilter { get => _selectedBookletFilter; set { _selectedBookletFilter = value; OnPropertyChanged(); StudentsView.Refresh(); } }
        public int? MinCorrectFilter { get => _minCorrectFilter; set { _minCorrectFilter = value; OnPropertyChanged(); StudentsView.Refresh(); } }
        public int? MaxWrongFilter { get => _maxWrongFilter; set { _maxWrongFilter = value; OnPropertyChanged(); StudentsView.Refresh(); } }
        public string SelectedSortColumn { get => _selectedSortColumn; set { _selectedSortColumn = value; OnPropertyChanged(); ApplySorting(); } }
        public bool IsSortDescending { get => _isSortDescending; set { _isSortDescending = value; OnPropertyChanged(); ApplySorting(); } }
        
        // Available Booklet Types for Filter
        public ObservableCollection<string> AvailableBookletTypes => new ObservableCollection<string>(AnswerKeys.Select(k => k.BookletName).Distinct());
        
        // Active Filter Status
        public bool HasActiveFilters => 
            !string.IsNullOrWhiteSpace(SearchText) ||
            MinScoreFilter.HasValue ||
            MaxScoreFilter.HasValue ||
            !string.IsNullOrEmpty(SelectedBookletFilter) ||
            MinCorrectFilter.HasValue ||
            MaxWrongFilter.HasValue;
        
        public string UpdateStatus { get => _updateStatus; set { _updateStatus = value; OnPropertyChanged(); } }
        public bool IsCheckingForUpdate { get => _isCheckingForUpdate; set { _isCheckingForUpdate = value; OnPropertyChanged(); } }
        public bool HasUpdateAvailable { get => _hasUpdateAvailable; set { _hasUpdateAvailable = value; OnPropertyChanged(); } }
        public string LatestVersion { get => _latestVersion; set { _latestVersion = value; OnPropertyChanged(); } }

        public string ActiveFiltersSummary
        {
            get
            {
                var filters = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(SearchText))
                    filters.Add($"Arama: '{SearchText}'");
                if (MinScoreFilter.HasValue || MaxScoreFilter.HasValue)
                    filters.Add($"Puan: {MinScoreFilter?.ToString() ?? "-∞"} - {MaxScoreFilter?.ToString() ?? "∞"}");
                if (!string.IsNullOrEmpty(SelectedBookletFilter))
                    filters.Add($"Kitapçık: {SelectedBookletFilter}");
                if (MinCorrectFilter.HasValue)
                    filters.Add($"Min Doğru: {MinCorrectFilter}");
                if (MaxWrongFilter.HasValue)
                    filters.Add($"Max Yanlış: {MaxWrongFilter}");
                
                return filters.Count > 0 ? "Aktif Filtreler: " + string.Join(", ", filters) : "Filtre yok";
            }
        }

        public string SchoolName { get => _schoolName; set { _schoolName = value; OnPropertyChanged(); SaveSettings(); } }
        public double NetCoefficient { get => _netCoefficient; set { _netCoefficient = value; OnPropertyChanged(); SaveSettings(); } }
        public double BaseScore { get => _baseScore; set { _baseScore = value; OnPropertyChanged(); SaveSettings(); } }
        public double WrongDeductionFactor { get => _wrongDeductionFactor; set { _wrongDeductionFactor = value; OnPropertyChanged(); OnPropertyChanged(nameof(DeductionRuleIndex)); SaveSettings(); } }
        
        public int DeductionRuleIndex 
        { 
            get => _wrongDeductionFactor switch { 0 => 0, 0.33 => 1, 0.25 => 2, _ => 2 };
            set 
            { 
                WrongDeductionFactor = value switch { 0 => 0, 1 => 0.33, 2 => 0.25, _ => 0.25 };
            } 
        }

        public int ValidationIssuesCount => ValidationIssues.Count;

        private bool FilterStudents(object obj)
        {
            if (obj is not StudentResult student) return false;
            
            // Text search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                bool matchesSearch = student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                     student.StudentId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                     FuzzySearchService.ContainsFuzzy(student.FullName, SearchText, 0.6) ||
                                     FuzzySearchService.ContainsFuzzy(student.StudentId, SearchText, 0.7);
                if (!matchesSearch) return false;
            }
            
            // Score range filter
            if (MinScoreFilter.HasValue && student.Score < MinScoreFilter.Value) return false;
            if (MaxScoreFilter.HasValue && student.Score > MaxScoreFilter.Value) return false;
            
            // Booklet type filter
            if (!string.IsNullOrEmpty(SelectedBookletFilter) && student.BookletType != SelectedBookletFilter) return false;
            
            // Correct count filter
            if (MinCorrectFilter.HasValue && student.CorrectCount < MinCorrectFilter.Value) return false;
            
            // Wrong count filter
            if (MaxWrongFilter.HasValue && student.IncorrectCount > MaxWrongFilter.Value) return false;
            
            return true;
        }

        private void ApplySorting()
        {
            StudentsView.SortDescriptions.Clear();
            
            var sortDirection = IsSortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            
            StudentsView.SortDescriptions.Add(new SortDescription(SelectedSortColumn, sortDirection));
            StudentsView.Refresh();
        }

        private void ClearAdvancedFilters()
        {
            MinScoreFilter = null;
            MaxScoreFilter = null;
            SelectedBookletFilter = null;
            MinCorrectFilter = null;
            MaxWrongFilter = null;
            SearchText = "";
            StudentsView.Refresh();
        }



        // Collections and Properties
        public ObservableCollection<StudentResult> Students { get; set; }
        public ObservableCollection<AnswerKeyModel> AnswerKeys { get; set; }
        public ObservableCollection<QuestionStatisticItem> Statistics { get; set; }
        public ObservableCollection<ValidationIssue> ValidationIssues { get; set; }
        public ObservableCollection<QuestionSetting> QuestionSettings { get; set; } = new();
        public ObservableCollection<LearningOutcome> LearningOutcomes { get; set; } = new();
        public ObservableCollection<Course> Courses { get; set; }
        public ObservableCollection<ExamEntry> CourseExams { get; set; }
        public ObservableCollection<LogEntry> Logs { get; set; } = new();
        public ObservableCollection<ChartItem> AccuracyData { get; set; } = new();
        public ObservableCollection<ChartItem> ScoreDistData { get; set; } = new();

        public Course? SelectedCourse
        {
            get => _selectedCourse;
            set { _selectedCourse = value; OnPropertyChanged(); if (value != null) _ = LoadExamsForCourseAsync(value.Id); else CourseExams.Clear(); }
        }

        public ExamEntry? SelectedExam
        {
            get => _selectedExam;
            set { _selectedExam = value; OnPropertyChanged(); if (value != null) _ = LoadExamDataAsync(value); }
        }

        public bool IsAddCourseOpen { get => _isAddCourseOpen; set { _isAddCourseOpen = value; OnPropertyChanged(); } }
        public string NewCourseCode { get => _newCourseCode; set { _newCourseCode = value; OnPropertyChanged(); } }
        public string NewCourseName { get => _newCourseName; set { _newCourseName = value; OnPropertyChanged(); } }
        
        public string NewExamName { get => _newExamName; set { _newExamName = value; OnPropertyChanged(); } }
        private string _newExamName = "";

        public bool IsRenameModalOpen { get => _isRenameModalOpen; set { _isRenameModalOpen = value; OnPropertyChanged(); } }
        public bool IsConfirmModalOpen { get => _isConfirmModalOpen; set { _isConfirmModalOpen = value; OnPropertyChanged(); } }
        public string ConfirmModalTitle { get => _confirmModalTitle; set { _confirmModalTitle = value; OnPropertyChanged(); } }
        public string ConfirmModalMessage { get => _confirmModalMessage; set { _confirmModalMessage = value; OnPropertyChanged(); } }
        public string ConfirmButtonText { get => _confirmButtonText; set { _confirmButtonText = value; OnPropertyChanged(); } }
        public string RenameModalTitle { get => _renameModalTitle; set { _renameModalTitle = value; OnPropertyChanged(); } }
        public string RenameInput1 { get => _renameInput1; set { _renameInput1 = value; OnPropertyChanged(); } }
        public string RenameInput2 { get => _renameInput2; set { _renameInput2 = value; OnPropertyChanged(); } }
        public bool ShowRenameInput2 { get => _showRenameInput2; set { _showRenameInput2 = value; OnPropertyChanged(); } }

        public bool IsModalOpen { get => _isModalOpen; set { _isModalOpen = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsQuestionSettingsOpen { get => _isQuestionSettingsOpen; set { _isQuestionSettingsOpen = value; OnPropertyChanged(); } }
        public bool IsLearningOutcomesOpen { get => _isLearningOutcomesOpen; set { _isLearningOutcomesOpen = value; OnPropertyChanged(); } }
        public bool IsAlertOpen { get => _isAlertOpen; set { _isAlertOpen = value; OnPropertyChanged(); } }
        public string AlertTitle { get => _alertTitle; set { _alertTitle = value; OnPropertyChanged(); } }
        public string AlertMessage { get => _alertMessage; set { _alertMessage = value; OnPropertyChanged(); } }
        public bool IsAboutOpen { get => _isAboutOpen; set { _isAboutOpen = value; OnPropertyChanged(); } }
        public bool IsShortcutsOpen { get => _isShortcutsOpen; set { _isShortcutsOpen = value; OnPropertyChanged(); } }
        private bool _isShortcutsOpen;
        public bool IsShortcutSettingsOpen { get => _isShortcutSettingsOpen; set { _isShortcutSettingsOpen = value; OnPropertyChanged(); } }
        public ICommand OpenShortcutSettingsCommand { get; set; }
        public ICommand CloseShortcutSettingsCommand { get; set; }
        public ICommand EditShortcutCommand { get; set; }
        public ICommand ResetShortcutsCommand { get; set; }
        public bool IsUISettingsOpen { get => _isUISettingsOpen; set { _isUISettingsOpen = value; OnPropertyChanged(); } }
        public bool IsGeneralConfigOpen { get => _isGeneralConfigOpen; set { _isGeneralConfigOpen = value; OnPropertyChanged(); } }
        public string DefaultExcelPath { get => _defaultExcelPath; set { _defaultExcelPath = value; OnPropertyChanged(); SaveSettings(); } }
        
        public int ThemeIndex { get => _themeIndex; set { _themeIndex = value; OnPropertyChanged(); ApplyTheme(value == 1); OnPropertyChanged(nameof(IsDarkTheme)); SaveSettings(); } }
        public bool IsDarkTheme => _themeIndex == 1;
        public string CurrentVersion => _versionService.CurrentVersion;
        public int LayoutIndex { get => _layoutIndex; set { _layoutIndex = value; OnPropertyChanged(); GridRowHeight = value == 0 ? 32 : 50; GridCellPadding = value == 0 ? new System.Windows.Thickness(10, 0, 10, 0) : new System.Windows.Thickness(15, 0, 15, 0); SaveSettings(); } }
        public double FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChanged(); ApplyFontSize(value); SaveSettings(); } }

        private void ApplyFontSize(double size)
        {
            if (System.Windows.Application.Current == null) return;
            var res = System.Windows.Application.Current.Resources;
            res["GlobalFontSize"] = size;
            res["GlobalHeaderFontSize"] = size + 4;
            res["GlobalSmallFontSize"] = size - 2;
        }
        public int GridRowHeight { get => _gridRowHeight; set { _gridRowHeight = value; OnPropertyChanged(); } }
        private int _gridRowHeight = 32;
        public System.Windows.Thickness GridCellPadding { get => _gridCellPadding; set { _gridCellPadding = value; OnPropertyChanged(); } }
        private System.Windows.Thickness _gridCellPadding = new System.Windows.Thickness(10, 0, 10, 0);

        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string BusyMessage { get => _busyMessage; set { _busyMessage = value; OnPropertyChanged(); } }

        public string NewOutcomeName { get => _newOutcomeName; set { _newOutcomeName = value; OnPropertyChanged(); } }
        public string NewOutcomeRange { get => _newOutcomeRange; set { _newOutcomeRange = value; OnPropertyChanged(); } }
        public string NewOutcomeBooklet { get => _newOutcomeBooklet; set { _newOutcomeBooklet = value; OnPropertyChanged(); } }

        // Zorluk Seviyesi Analizi için yeni property'ler
        public ObservableCollection<QuestionDifficulty> QuestionDifficulties { get; } = new ObservableCollection<QuestionDifficulty>();
        public double AverageDifficulty { get; set; } = 0;
        public string DifficultyDistribution { get; set; } = "";

        // İstatistiksel Raporlar için yeni property'ler
        public StatisticsReportService.ClassStatistics ClassStats { get; private set; } = new();
        public ObservableCollection<StatisticsReportService.ScoreDistribution> ScoreDistribution { get; } = new();
        public ObservableCollection<StatisticsReportService.QuestionCorrelation> QuestionCorrelations { get; } = new();
        public Dictionary<string, double> Percentiles { get; private set; } = new();
        public string ReportText { get; private set; } = "";
        public ICommand CopyReportCommand { get; set; }

        // Toast Notifications
        public System.Collections.ObjectModel.ObservableCollection<ToastNotificationModel> Notifications => _notificationService.ActiveNotifications;
        public ICommand DismissNotificationCommand { get; set; }

        public bool HasUnsavedData { get => _hasUnsavedData; set { _hasUnsavedData = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaveStatusText)); } }
        public string SaveStatusText => _hasUnsavedData ? "⚠️ Kaydedilmedi" : (Students.Count > 0 ? "✅ Kaydedildi" : "");

        // Undo/Redo Properties
        public bool CanUndo => _undoRedoManager.CanUndo;
        public bool CanRedo => _undoRedoManager.CanRedo;
        public string UndoDescription => _undoRedoManager.CanUndo ? $"Geri Al: {_undoRedoManager.GetUndoDescription()}" : "Geri alınacak işlem yok";
        public string RedoDescription => _undoRedoManager.CanRedo ? $"Yenile: {_undoRedoManager.GetRedoDescription()}" : "Yenilenecek işlem yok";

        // Commands
        public ICommand LoadTxtCommand { get; set; }
        public ICommand EvaluateCommand { get; set; }
        public ICommand ExportExcelCommand { get; set; }
        public ICommand ExportCsvCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }
        public ICommand ExportSinglePdfCommand { get; set; }
        public ICommand AddAnswerKeyCommand { get; set; }
        public ICommand RemoveAnswerKeyCommand { get; set; }
        public ICommand OpenModalCommand { get; set; }
        public ICommand CloseModalCommand { get; set; }
        public ICommand OpenQuestionSettingsCommand { get; set; }
        public ICommand CloseQuestionSettingsCommand { get; set; }
        public ICommand OpenLearningOutcomesCommand { get; set; }
        public ICommand CloseLearningOutcomesCommand { get; set; }
        public ICommand AddLearningOutcomeCommand { get; set; }
        public ICommand AddOutcomeCommand { get; set; } = new RelayCommand(_ => { });
        public ICommand RemoveOutcomeCommand { get; set; }
        public ICommand SelectFolderCommand { get; set; }
        public ICommand CloseAlertCommand { get; set; }
        public ICommand OpenAboutCommand { get; set; }
        public ICommand OpenGitHubCommand { get; set; }
        public ICommand CheckForUpdatesCommand { get; set; }
        public ICommand OpenReleasesPageCommand { get; set; }
        public ICommand CloseAboutCommand { get; set; }
        public ICommand ShowShortcutsCommand { get; set; }
        public ICommand CloseShortcutsCommand { get; set; }
        public ICommand ExportGradeListCommand { get; set; }
        public ICommand OpenUISettingsCommand { get; set; }
        public ICommand CloseUISettingsCommand { get; set; }
        public bool IsSaveExamModalOpen
        {
            get => _isSaveExamModalOpen;
            set
            {
                _isSaveExamModalOpen = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenSaveExamModalCommand { get; set; }
        public ICommand CloseSaveExamModalCommand { get; set; }
        public ICommand ConfirmSaveExamCommand { get; set; }
        public ICommand OpenGeneralConfigCommand { get; set; }
        public ICommand CloseGeneralConfigCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand ShowAddCourseCommand { get; set; }
        public ICommand CloseAddCourseCommand { get; set; }
        public ICommand AddCourseCommand { get; set; }
        public ICommand DeleteCourseCommand { get; set; }
        public ICommand DeleteExamCommand { get; set; }
        public ICommand SaveExamCommand { get; set; }
        public ICommand OpenRenameCourseCommand { get; set; }
        public ICommand OpenRenameExamCommand { get; set; }
        public ICommand CloseRenameModalCommand { get; set; }
        public ICommand SaveRenameCommand { get; set; }
        public ICommand CancelConfirmCommand { get; set; }
        public ICommand ConfirmActionCommand { get; set; }
        public ICommand UndoCommand { get; set; }
        public ICommand RedoCommand { get; set; }

        // Template Manager Properties and Commands
        public bool IsTemplateManagerOpen { get; set; }
        public string NewTemplateName { get; set; } = "";
        public string NewTemplateDescription { get; set; } = "";
        public ObservableCollection<TemplateService.ExamTemplate> Templates { get; } = new();
        public ICommand OpenTemplateManagerCommand { get; set; }
        public ICommand CloseTemplateManagerCommand { get; set; }
        public ICommand SaveAsTemplateCommand { get; set; }
        public ICommand LoadTemplateCommand { get; set; }
        public ICommand DeleteTemplateCommand { get; set; }
        public ICommand ExportTemplateCommand { get; set; }
        public ICommand ImportTemplateCommand { get; set; }

        // JSON Data Import/Export Commands
        public ICommand ExportJsonCommand { get; set; }
        public ICommand ImportJsonCommand { get; set; }

        // Backup & Restore Commands
        public ICommand CreateFullBackupCommand { get; set; }
        public ICommand CreateDatabaseBackupCommand { get; set; }
        public ICommand ExportSettingsCommand { get; set; }
        public ICommand RestoreFullBackupCommand { get; set; }
        public ICommand RestoreDatabaseCommand { get; set; }
        public ICommand ImportSettingsCommand { get; set; }
        
        public bool IsItemAnalysisOpen { get => _isItemAnalysisOpen; set { _isItemAnalysisOpen = value; OnPropertyChanged(); } }
        public ObservableCollection<ItemAnalysisService.QuestionItemStats> QuestionStats { get => _questionStats; set { _questionStats = value; OnPropertyChanged(); } }
        public ObservableCollection<ItemAnalysisService.AnomalyResult> Anomalies { get => _anomalies; set { _anomalies = value; OnPropertyChanged(); } }
        public ItemAnalysisService.ReliabilityStats ReliabilityStats { get => _reliabilityStats; set { _reliabilityStats = value; OnPropertyChanged(); } }
        
        // Item Analysis Commands
        public ICommand OpenItemAnalysisCommand { get; set; }
        public ICommand CloseItemAnalysisCommand { get; set; }
        public ICommand RunItemAnalysisCommand { get; set; }

        // Advanced Filter Commands
        public ICommand OpenAdvancedFilterCommand { get; set; }
        public ICommand CloseAdvancedFilterCommand { get; set; }
        public ICommand ClearFiltersCommand { get; set; }
        public ICommand ToggleSortDirectionCommand { get; set; }

        // Success Prediction Properties
        public bool IsSuccessPredictionOpen { get => _isSuccessPredictionOpen; set { _isSuccessPredictionOpen = value; OnPropertyChanged(); } }
        public ObservableCollection<SuccessPredictionService.PredictionResult> StudentPredictions { get => _studentPredictions; set { _studentPredictions = value; OnPropertyChanged(); } }
        public SuccessPredictionService.ClassPredictionSummary ClassPredictionSummary { get => _classPredictionSummary; set { _classPredictionSummary = value; OnPropertyChanged(); } }
        public double PassingScore { get => _passingScore; set { _passingScore = value; OnPropertyChanged(); } }
        
        // Success Prediction Commands
        public ICommand OpenSuccessPredictionCommand { get; set; }
        public ICommand CloseSuccessPredictionCommand { get; set; }
        public ICommand RunSuccessPredictionCommand { get; set; }

        // Student Detail Properties
        public bool IsStudentDetailOpen { get => _isStudentDetailOpen; set { _isStudentDetailOpen = value; OnPropertyChanged(); } }
        public StudentResult? SelectedStudentDetail { get => _selectedStudentDetail; set { _selectedStudentDetail = value; OnPropertyChanged(); } }
        
        // Student Detail Commands
        public ICommand OpenStudentDetailCommand { get; set; }
        public ICommand CloseStudentDetailCommand { get; set; }
        public ICommand SaveStudentDetailCommand { get; set; }

        // Validation Details Properties
        public bool IsValidationDetailsOpen { get => _isValidationDetailsOpen; set { _isValidationDetailsOpen = value; OnPropertyChanged(); } }
        
        // Validation Details Commands
        public ICommand ShowValidationDetailsCommand { get; set; }
        public ICommand CloseValidationDetailsCommand { get; set; }
        public ICommand CopyValidationErrorsCommand { get; set; }

        // Detailed Answer Key Editor Properties
        public bool IsDetailedAnswerKeyEditorOpen { get => _isDetailedAnswerKeyEditorOpen; set { _isDetailedAnswerKeyEditorOpen = value; OnPropertyChanged(); } }
        public bool IsBulkAnswerEntryOpen { get => _isBulkAnswerEntryOpen; set { _isBulkAnswerEntryOpen = value; OnPropertyChanged(); } }
        public AnswerKeyModel? SelectedAnswerKeyForEdit { get => _selectedAnswerKeyForEdit; set { _selectedAnswerKeyForEdit = value; OnPropertyChanged(); } }
        public string BulkAnswerEntryText { get => _bulkAnswerEntryText; set { _bulkAnswerEntryText = value; OnPropertyChanged(); UpdateBulkPreview(); } }
        public string BulkAnswerPreview { get => _bulkAnswerPreview; set { _bulkAnswerPreview = value; OnPropertyChanged(); } }
        
        // Inline Course Management Properties
        public bool IsAddCourseInlineOpen { get => _isAddCourseInlineOpen; set { _isAddCourseInlineOpen = value; OnPropertyChanged(); } }
        public string NewCourseCodeInline { get => _newCourseCodeInline; set { _newCourseCodeInline = value; OnPropertyChanged(); } }
        public string NewCourseNameInline { get => _newCourseNameInline; set { _newCourseNameInline = value; OnPropertyChanged(); } }
        
        // Separate property for SaveExamModal dropdown (isolated from sidebar)
        public Course? SelectedCourseForSave { get => _selectedCourseForSave; set { _selectedCourseForSave = value; OnPropertyChanged(); } }
        
        // Selected answer key stats for display
        public string SelectedAnswerKeyStats => SelectedAnswerKeyForEdit?.AnswerDetails?.Count > 0 
            ? $"{SelectedAnswerKeyForEdit.AnswerDetails.Count} Soru"
            : "Henüz cevap yok";

        // Detailed Answer Key Editor Commands
        public ICommand OpenDetailedAnswerKeyEditorCommand { get; set; }
        public ICommand OpenDetailedAnswerKeyEditorFromBookletCommand { get; set; }
        public ICommand CloseDetailedAnswerKeyEditorCommand { get; set; }
        public ICommand AnalyzeAnswerKeyCommand { get; set; }
        public ICommand AddQuestionToAnswerKeyCommand { get; set; }
        public ICommand RemoveLastQuestionFromAnswerKeyCommand { get; set; }
        public ICommand OpenBulkAnswerEntryCommand { get; set; }
        public ICommand CloseBulkAnswerEntryCommand { get; set; }
        public ICommand ApplyBulkAnswerEntryCommand { get; set; }

        // Inline Course Management Commands
        public ICommand ToggleAddCourseInlineCommand { get; set; }
        public ICommand AddCourseInlineCommand { get; set; }
        public ICommand DeleteSelectedCourseInlineCommand { get; set; }

        // Progress Service
        public ProgressService Progress => _progressService;
        public ICommand CancelProgressCommand { get; set; }

        public void AddToLog(string message, LogLevel level = LogLevel.Info)
        {
            if (System.Windows.Application.Current == null)
            {
                Logs.Insert(0, new LogEntry { Message = message, Level = level });
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                Logs.Insert(0, new LogEntry { Message = message, Level = level });
                if (Logs.Count > 100) Logs.RemoveAt(100);
            });
        }

        private readonly Task _initTask;
        private async Task InitializeAsync()
        {
            await _dbService.InitializeDatabaseAsync();
            await LoadCoursesAsync();
        }

        private async Task LoadCoursesAsync()
        {
            var list = await _dbService.GetCoursesAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                Courses.Clear();
                foreach (var c in list) Courses.Add(c);
            });
        }

        private async Task LoadExamsForCourseAsync(int courseId)
        {
            var list = await _dbService.GetExamsForCourseAsync(courseId);
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                CourseExams.Clear();
                foreach (var e in list) CourseExams.Add(e);
                if (SelectedExam == null) Students.Clear();
            });
        }

        private async Task LoadExamDataAsync(ExamEntry exam)
        {
            try
            {
                IsBusy = true;
                BusyMessage = $"{exam.Title} yükleniyor...";
                AddToLog($"{exam.Title} sınav verileri yükleniyor...");
                
                var results = await _dbService.GetResultsForExamAsync(exam.Id);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    Students.Clear();
                    foreach (var r in results) Students.Add(r);

                    if (!string.IsNullOrEmpty(exam.ConfigJson))
                    {
                        var config = JsonSerializer.Deserialize<ExamConfigData>(exam.ConfigJson);
                        if (config != null)
                        {
                            AnswerKeys.Clear();
                            foreach (var k in config.AnswerKeys) AnswerKeys.Add(k);
                            QuestionSettings.Clear();
                            foreach (var s in config.QuestionSettings) QuestionSettings.Add(s);
                            LearningOutcomes.Clear();
                            foreach (var o in config.LearningOutcomes) LearningOutcomes.Add(o);
                            
                            SchoolName = config.SchoolName ?? "Okul Adı";
                            NetCoefficient = config.NetCoefficient != 0 ? config.NetCoefficient : 1.0;
                            BaseScore = config.BaseScore;
                            WrongDeductionFactor = (config.WrongDeductionFactor != 0 || exam.ConfigJson.Contains("WrongDeductionFactor")) ? config.WrongDeductionFactor : 0.25;
                        }
                    }
                });

                await EvaluateAsync();
                HasUnsavedData = false;
                AddToLog($"{exam.Title} başarıyla yüklendi.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddToLog($"Sınav yükleme hatası: {ex.Message}", LogLevel.Error);
                ShowAlert("Hata", "Sınav verileri yüklenirken bir sorun oluştu.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveCurrentExamAsync(string title, Course courseToSave, ExamEntry? existingExam = null)
        {
            AddToLog($"DEBUG: SaveCurrentExamAsync started - Students.Count: {Students.Count}, CourseToSave: {courseToSave?.Name}", LogLevel.Info);
            
            if (courseToSave == null)
            {
                ShowAlert("Hata", "Lütfen önce bir ders seçin.");
                return;
            }
            try
            {
                var config = new ExamConfigData
                {
                    AnswerKeys = new List<AnswerKeyModel>(AnswerKeys),
                    QuestionSettings = new List<QuestionSetting>(QuestionSettings),
                    LearningOutcomes = new List<LearningOutcome>(LearningOutcomes),
                    SchoolName = SchoolName,
                    NetCoefficient = NetCoefficient,
                    BaseScore = BaseScore,
                    WrongDeductionFactor = WrongDeductionFactor
                };

                if (existingExam != null && existingExam.Id > 0)
                {
                    // Update existing exam
                    existingExam.Title = title;
                    existingExam.ConfigJson = JsonSerializer.Serialize(config);
                    await _dbService.UpdateExamAsync(existingExam, new List<StudentResult>(Students));
                    AddToLog($"'{title}' sınavı güncellendi.", LogLevel.Success);
                }
                else
                {
                    // Insert new exam
                    var exam = new ExamEntry
                    {
                        CourseId = courseToSave.Id,
                        Title = title,
                        Date = DateTime.Now,
                        ConfigJson = JsonSerializer.Serialize(config)
                    };
                    await _dbService.SaveExamAsync(exam, new List<StudentResult>(Students));
                    AddToLog($"'{title}' sınavı veritabanına kaydedildi.", LogLevel.Success);
                }
                await LoadExamsForCourseAsync(courseToSave.Id);
                HasUnsavedData = false;
            }
            catch (Exception ex)
            {
                AddToLog($"DEBUG: Kaydetme hatası: {ex.Message}\nStack: {ex.StackTrace}", LogLevel.Error);
                AddToLog($"Kaydetme hatası: {ex.Message}", LogLevel.Error);
                ShowAlert("Hata", $"Kayıt sırasında hata oluştu: {ex.Message}");
            }
        }

        private async Task AutoSaveSelectedExamAsync()
        {
            if (SelectedExam == null || SelectedExam.Id <= 0 || SelectedCourse == null) return;
            try
            {
                var config = new ExamConfigData
                {
                    AnswerKeys = new List<AnswerKeyModel>(AnswerKeys),
                    QuestionSettings = new List<QuestionSetting>(QuestionSettings),
                    LearningOutcomes = new List<LearningOutcome>(LearningOutcomes),
                    SchoolName = SchoolName,
                    NetCoefficient = NetCoefficient,
                    BaseScore = BaseScore,
                    WrongDeductionFactor = WrongDeductionFactor
                };
                SelectedExam.ConfigJson = JsonSerializer.Serialize(config);
                await _dbService.UpdateExamAsync(SelectedExam, new List<StudentResult>(Students));
                AddToLog($"'{SelectedExam.Title}' değişiklikler otomatik kaydedildi.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddToLog($"Otomatik kaydetme hatası: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task ExecuteDeleteCourseAsync(Course c, System.Collections.Generic.List<ExamEntry> exams)
        {
            // Eğer silinen derse ait seçili sınav varsa, tüm verileri temizle
            if (SelectedExam != null && exams.Any(e => e.Id == SelectedExam.Id))
            {
                SelectedExam = null;
                Students.Clear();
                AnswerKeys.Clear();
                QuestionSettings.Clear();
                LearningOutcomes.Clear();
                Statistics.Clear();
                ValidationIssues.Clear();
                AccuracyData.Clear();
                ScoreDistData.Clear();
                HasUnsavedData = false;
                StatusMessage = "Ders ve sınavlar silindi. Yeni veri yükleyin.";
                AnswerKeys.Add(new AnswerKeyModel { BookletName = "A", Answers = "" });
            }
            
            await _dbService.DeleteCourseAsync(c.Id);
            await LoadCoursesAsync();
            
            // Seçili ders silindiyse temizle
            if (SelectedCourse?.Id == c.Id) {
                SelectedCourse = null;
                CourseExams.Clear();
            }
            
            AddToLog($"'{c.Name}' dersi ve {exams.Count()} sınavı silindi.", LogLevel.Warning);
        }

        private void SaveSettings()
        {
            _settingsService.Save(new OptikFormApp.Models.AppSettings
            {
                SchoolName = _schoolName,
                DefaultExcelPath = _defaultExcelPath,
                NetCoefficient = _netCoefficient,
                BaseScore = _baseScore,
                WrongDeductionFactor = _wrongDeductionFactor,
                ThemeIndex = _themeIndex,
                LayoutIndex = _layoutIndex,
                FontSize = (int)_fontSize
            });
        }

        private void ApplyTheme(bool dark) {
            if (System.Windows.Application.Current == null) return;
            var res = System.Windows.Application.Current.Resources;
            var cc = new System.Windows.Media.BrushConverter();
            
            // Modern renk paleti
            if (dark) {
                res["AppBg"] = cc.ConvertFromString("#0F172A");      // Slate-900
                res["CardBg"] = cc.ConvertFromString("#1E293B");     // Slate-800  
                res["TextMain"] = cc.ConvertFromString("#F8FAFC");    // Slate-50
                res["TextMuted"] = cc.ConvertFromString("#94A3B8");  // Slate-400
                res["Border"] = cc.ConvertFromString("#334155");     // Slate-700
                res["HeaderBg"] = cc.ConvertFromString("#0F172A");    // Slate-900
                res["AltRowBg"] = cc.ConvertFromString("#1E293B");   // Slate-800
                res["HoverBg"] = cc.ConvertFromString("#475569");    // Slate-600
                res["ModalBackdrop"] = cc.ConvertFromString("#B3000000"); // Black with opacity
                res["Accent"] = cc.ConvertFromString("#3B82F6");     // Blue-600
                res["AccentHover"] = cc.ConvertFromString("#2563EB");  // Blue-700
                res["Success"] = cc.ConvertFromString("#10B981");     // Green-600
                res["Warning"] = cc.ConvertFromString("#F59E0B");     // Amber-600
                res["Error"] = cc.ConvertFromString("#EF4444");       // Red-600
                
                // Glassmorphism renkleri
                res["GlassCardBgColor"] = cc.ConvertFromString("#FFFFFF"); // White for glass effect
                res["GlassCardBorderColor"] = cc.ConvertFromString("#FFFFFF"); // White for glass effect
                res["ShadowColor"] = cc.ConvertFromString("#000000");   // Black for shadows
            } else {
                res["AppBg"] = cc.ConvertFromString("#F8FAFC");      // Slate-50
                res["CardBg"] = cc.ConvertFromString("#FFFFFF");       // White
                res["TextMain"] = cc.ConvertFromString("#1E293B");    // Slate-800
                res["TextMuted"] = cc.ConvertFromString("#64748B");  // Slate-500
                res["Border"] = cc.ConvertFromString("#E2E8F0");     // Slate-200
                res["HeaderBg"] = cc.ConvertFromString("#F1F5F9");    // Slate-100
                res["AltRowBg"] = cc.ConvertFromString("#F8FAFC");   // Slate-50
                res["HoverBg"] = cc.ConvertFromString("#EFF6FF");    // Blue-50
                res["ModalBackdrop"] = cc.ConvertFromString("#800F172A"); // Black with opacity
                res["Accent"] = cc.ConvertFromString("#3B82F6");     // Blue-600
                res["AccentHover"] = cc.ConvertFromString("#2563EB");  // Blue-700
                res["Success"] = cc.ConvertFromString("#10B981");     // Green-600
                res["Warning"] = cc.ConvertFromString("#F59E0B");     // Amber-600
                res["Error"] = cc.ConvertFromString("#EF4444");       // Red-600
                
                // Glassmorphism renkleri
                res["GlassCardBgColor"] = cc.ConvertFromString("#FFFFFF"); // White for glass effect
                res["GlassCardBorderColor"] = cc.ConvertFromString("#FFFFFF"); // White for glass effect
                res["ShadowColor"] = cc.ConvertFromString("#000000");   // Black for shadows
            }
            
            // System colors
            res[System.Windows.SystemColors.MenuBrushKey] = res["CardBg"];
            res[System.Windows.SystemColors.MenuTextBrushKey] = res["TextMain"];
            res[System.Windows.SystemColors.WindowBrushKey] = res["AppBg"];
            res[System.Windows.SystemColors.WindowTextBrushKey] = res["TextMain"];
            res[System.Windows.SystemColors.ControlBrushKey] = res["CardBg"];
            res[System.Windows.SystemColors.ControlTextBrushKey] = res["TextMain"];
            res[System.Windows.SystemColors.HighlightBrushKey] = res["HoverBg"];
            res[System.Windows.SystemColors.HighlightTextBrushKey] = res["TextMain"];
        }

        private async Task LoadTxtFileAsync()
        {
            var openFileDialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt", Title = "Optik Okuyucu Dosyasını Seçin" };
            if (openFileDialog.ShowDialog() == true)
            {
                await LoadDroppedFileAsync(openFileDialog.FileName);
            }
        }

        public async Task LoadDroppedFileAsync(string filePath)
        {
            SelectedExam = null;
            Students.Clear(); AnswerKeys.Clear();
            Statistics.Clear(); ValidationIssues.Clear(); AccuracyData.Clear(); ScoreDistData.Clear();
            
            StatusMessage = "Dosya okunuyor, lütfen bekleyin...";
            ShowToastInfo($"{System.IO.Path.GetFileName(filePath)} dosyası yükleniyor...");
            
            try
            {
                var (students, answerKeys, errors) = await _parserService.ParseFileAsync(filePath);
                bool isCritical = errors.Any(e => e.StartsWith("KRİTİK HATA"));
                
                if (errors.Count > 0)
                {
                    // Hataları log'a ekle
                    foreach (var err in errors) AddToLog(err, LogLevel.Error);
                    
                    // Parse hatalarını ValidationIssues'e de ekle (üst panelde görünsün)
                    foreach (var err in errors.Where(e => !e.StartsWith("KRİTİK HATA")))
                    {
                        // Hata mesajını parçala: [ÖğrenciNo] İsim: Mesaj
                        var parts = err.Split(':');
                        var title = parts.Length > 1 ? parts[0].Trim() : "Parse Hatası";
                        var message = parts.Length > 1 ? string.Join(":", parts.Skip(1)).Trim() : err;
                        
                        ValidationIssues.Add(new ValidationIssue
                        {
                            Title = title.Length > 50 ? title.Substring(0, 50) + "..." : title,
                            Message = message,
                            Severity = ValidationSeverity.Warning,
                            AffectedItems = err.Contains('[') && err.Contains(']') 
                                ? err.Substring(err.IndexOf('['), err.IndexOf(']') - err.IndexOf('[') + 1)
                                : $"Satır {errors.IndexOf(err) + 1}"
                        });
                    }
                    OnPropertyChanged(nameof(ValidationIssuesCount));
                    
                    if (isCritical)
                    {
                        ShowToastError("Dosya formatı geçersiz. Lütfen optik okuyucu çıktısı (.txt) olduğundan emin olun.");
                        StatusMessage = "Dosya formatı geçersiz.";
                        return;
                    }
                    ShowToastWarning($"{errors.Count} satırda hata var, {students.Count} kayıt yüklendi.");
                }
                
                if (answerKeys.Count > 0)
                {
                    AnswerKeys.Clear();
                    foreach(var ak in answerKeys) AnswerKeys.Add(ak);
                    ShowToastSuccess($"{answerKeys.Count} adet cevap anahtarı yüklendi.");
                }
                
                Students.Clear();
                int rowNum = 1;
                foreach (var student in students) { student.RowNumber = rowNum++; Students.Add(student); }
                
                if (students.Count > 0)
                {
                    HasUnsavedData = true;
                    StatusMessage = $"{students.Count} öğrenci başarıyla yüklendi.";
                    ShowToastSuccess($"{students.Count} öğrenci kaydı başarıyla yüklendi.");
                    bool hasValidKey = false;
                    foreach(var key in AnswerKeys) { if(!string.IsNullOrWhiteSpace(key.Answers)) hasValidKey = true; }
                    if (hasValidKey) await EvaluateAsync();
                }
                else
                {
                    StatusMessage = "Dosyada yüklenebilir öğrenci kaydı bulunamadı.";
                    ShowToastWarning("Yüklenebilir kayıt bulunamadı. Lütfen dosya formatını kontrol edin.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
                ShowToastError($"Yükleme hatası: {ex.Message}");
            }
        }

        private async Task EvaluateAsync()
        {
            bool hasValidKey = false;
            foreach(var key in AnswerKeys) { if(!string.IsNullOrWhiteSpace(key.Answers)) hasValidKey = true; }
            if (!hasValidKey)
            {
                StatusMessage = "Cevap anahtarı girilmeden hesaplama yapılamaz!";
                AddToLog("Değerlendirme iptal: Cevap anahtarı eksik.", LogLevel.Warning);
                ShowAlert("Eksik Cevap Anahtarı", "Lütfen puanları hesaplamadan veya dosyayı içe aktarmadan önce 'Cevap Anahtarlarını Yönet' bölümünden bir cevap şablonu (Örn: A) doldurunuz.");
                return;
            }

            IsBusy = true;
            BusyMessage = "Puanlar hesaplanıyor...";
            AddToLog("Puanlar hesaplanıyor ve madde analizi yapılıyor...");
            
            try
            {
                // Uİ dışı verileri kopyala (Dispatcher hatasını kökten bitirmek için DERİN KOPYALAMA)
                var list = Students.Select(s => new StudentResult 
                { 
                    StudentId = s.StudentId, 
                    FullName = s.FullName, 
                    BookletType = s.BookletType, 
                    RawAnswers = s.RawAnswers,
                    Answers = new List<string>(s.Answers)
                }).ToList();

                var keys = new List<AnswerKeyModel>(AnswerKeys);
                var settings = new List<QuestionSetting>(QuestionSettings);
                var factor = WrongDeductionFactor;
                var coeff = NetCoefficient;
                var bScore = BaseScore;

                await Task.Run(() => 
                {
                    // Arka planda kopyalar üzerinde hesapla
                    _parserService.EvaluateStudents(list, keys, settings, factor);
                });

                // Sonuçları topla (Bu servisler read-only çalışır)
                var statsList = _parserService.CalculateStatistics(list, keys);
                var issues = _validationService.Validate(list, keys);

                // Ana iş parçacığında koleksiyonları ve ÖZELLİKLERİ güncelle
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    // 1. Önce özellik değerlerini ve RENK kodlarını İNDEKS bazlı eşitle (En güvenli yöntem)
                    for (int i = 0; i < Students.Count && i < list.Count; i++)
                    {
                        var original = Students[i];
                        var evaluated = list[i]; // Sıralama aynı olduğu için doğrudan indeks kullanıyoruz
                        
                        original.CorrectCount = evaluated.CorrectCount;
                        original.IncorrectCount = evaluated.IncorrectCount;
                        original.EmptyCount = evaluated.EmptyCount;
                        original.NetCount = evaluated.NetCount;
                        original.Rank = evaluated.Rank;
                        original.Score = Math.Round((evaluated.NetCount * coeff) + bScore, 2);
                        
                        // Detaylı sonuçları ve renkli cevapları kopyala
                        original.QuestionResults = new List<bool>(evaluated.QuestionResults);
                        original.ColoredAnswers.Clear();
                        foreach (var answer in evaluated.ColoredAnswers)
                        {
                            original.ColoredAnswers.Add(answer);
                        }
                    }

                    // 2. Diğer listeleri güncelle
                    Statistics.Clear();
                    foreach (var stat in statsList) Statistics.Add(stat);
                    
                    ValidationIssues.Clear();
                    foreach (var issue in issues) ValidationIssues.Add(issue);
                    
                    StudentsView.Refresh();
                    UpdateChartData(statsList, list);
                    UpdateOutcomeStats();
                    UpdateStatisticsReport();
                    OnPropertyChanged(nameof(ValidationIssuesCount));
                });

                StatusMessage = "Değerlendirme başarıyla tamamlandı.";
                AddToLog("Değerlendirme başarıyla tamamlandı.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                StatusMessage = "Hesaplama sırasında teknik bir sorun oluştu.";
                var detailedError = $"Hata: {ex.Message}\nStackTrace: {ex.StackTrace}";
                if (ex.InnerException != null)
                    detailedError += $"\nInner: {ex.InnerException.Message}";
                AddToLog($"Hesaplama hatası (Teknik Detay): {detailedError}", LogLevel.Error);
                ShowAlert("Hesaplama Hatası", "Puanlar hesaplanırken bir sorun oluştu. Lütfen verilerinizi ve cevap anahtarını kontrol edin.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateOutcomeStats()
        {
            if (Students.Count == 0 || LearningOutcomes.Count == 0) return;

            var groups = LearningOutcomes.GroupBy(o => o.Name).ToList();
            var updatedOutcomes = new List<LearningOutcome>();

            foreach (var group in groups)
            {
                var globalCorrect = 0;
                var globalTotal = 0;

                foreach (var outcome in group)
                {
                    var bookletStudents = Students.Where(s => s.BookletType == outcome.BookletName).ToList();
                    
                    foreach (var student in bookletStudents)
                    {
                        foreach (var qNum in outcome.QuestionNumbers)
                        {
                            if (qNum > 0 && qNum <= student.QuestionResults.Count)
                            {
                                globalTotal++;
                                if (student.QuestionResults[qNum - 1]) globalCorrect++;
                            }
                        }
                    }

                    outcome.TotalQuestions = globalTotal;
                    outcome.SuccessRate = globalTotal > 0 ? (double)globalCorrect / globalTotal * 100 : 0;
                }

                var firstOutcome = group.First();
                firstOutcome.GlobalCorrectCount = globalCorrect;
                firstOutcome.GlobalTotalCount = globalTotal;
                firstOutcome.GlobalSuccessRate = globalTotal > 0 ? (double)globalCorrect / globalTotal * 100 : 0;
                updatedOutcomes.Add(firstOutcome);
            }

            LearningOutcomes.Clear();
            foreach (var outcome in updatedOutcomes)
            {
                LearningOutcomes.Add(outcome);
            }
            
            OnPropertyChanged(nameof(LearningOutcomes));
            
            // Zorluk Seviyesi Analizini Güncelle
            UpdateQuestionDifficultyAnalysis();
            
            // Log the calculation completion
            AddToLog($"Kazanım odaklı başarı analizi tamamlandı: {LearningOutcomes.Count} kazanımdan {groups.Count()} benzersiz konu hesaplandı.", LogLevel.Success);
        }

        private void UpdateQuestionDifficultyAnalysis()
        {
            if (Students.Count == 0) return;

            QuestionDifficulties.Clear();
            double totalDifficulty = 0;
            // Tüm öğrencilerin minimum soru sayısını al (farklı kitapçıklar için)
            int minQuestionCount = Students.Min(s => s.QuestionResults?.Count ?? 0);
            int questionCount = Math.Min(minQuestionCount, 50);

            for (int i = 0; i < questionCount; i++)
            {
                var questionNum = i + 1;
                // Her öğrenci için bounds kontrolü
                var correctCount = Students.Count(s => s.QuestionResults != null && i < s.QuestionResults.Count && s.QuestionResults[i]);
                var successRate = Students.Count > 0 ? (double)correctCount / Students.Count * 100 : 0;

                var difficulty = new QuestionDifficulty
                {
                    QuestionNumber = questionNum,
                    SuccessRate = successRate,
                    CorrectAnswers = correctCount,
                    TotalAnswers = Students.Count,
                    DifficultyLevel = GetDifficultyLevel(successRate)
                };

                QuestionDifficulties.Add(difficulty);
                totalDifficulty += successRate;
            }

            AverageDifficulty = questionCount > 0 ? totalDifficulty / questionCount : 0;
            DifficultyDistribution = GetDifficultyDistribution();

            OnPropertyChanged(nameof(QuestionDifficulties));
            OnPropertyChanged(nameof(AverageDifficulty));
            OnPropertyChanged(nameof(DifficultyDistribution));
        }

        private string GetDifficultyLevel(double successRate)
        {
            return successRate switch
            {
                >= 80 => "Kolay",
                >= 60 => "Orta",
                >= 40 => "Zor",
                _ => "Çok Zor"
            };
        }

        private string GetDifficultyDistribution()
        {
            if (QuestionDifficulties.Count == 0) return "Veri yok";

            var easy = QuestionDifficulties.Count(q => q.DifficultyLevel == "Kolay");
            var medium = QuestionDifficulties.Count(q => q.DifficultyLevel == "Orta");
            var hard = QuestionDifficulties.Count(q => q.DifficultyLevel == "Zor");
            var veryHard = QuestionDifficulties.Count(q => q.DifficultyLevel == "Çok Zor");

            return $"Kolay: {easy}, Orta: {medium}, Zor: {hard}, Çok Zor: {veryHard}";
        }

        private void UpdateChartData(System.Collections.Generic.List<QuestionStatisticItem> stats, System.Collections.Generic.List<StudentResult> students)
        {
            AccuracyData.Clear();
            foreach(var s in stats) {
                AccuracyData.Add(new ChartItem { Label = s.QuestionNumber.ToString(), Value = s.CorrectPercent, Tooltip = $"Soru {s.QuestionNumber}: %{s.CorrectPercent} Doğru" });
            }
            ScoreDistData.Clear();
            var bins = new int[11];
            foreach(var s in students) {
                int binIdx = (int)Math.Min(s.Score / 10, 10);
                bins[binIdx]++;
            }
            for(int i = 0; i < bins.Length; i++) {
                ScoreDistData.Add(new ChartItem { Label = (i*10).ToString(), Value = bins[i], Tooltip = $"{i*10}-{(i*10)+10} Puan: {bins[i]} Öğrenci" });
            }
        }

        private void UpdateStatisticsReport()
        {
            if (Students.Count == 0) return;

            var studentsList = Students.ToList();

            // Class Statistics
            ClassStats = _statsReportService.CalculateClassStatistics(studentsList);
            OnPropertyChanged(nameof(ClassStats));

            // Score Distribution
            ScoreDistribution.Clear();
            foreach (var dist in _statsReportService.GetScoreDistribution(studentsList, 10))
                ScoreDistribution.Add(dist);

            // Percentiles
            Percentiles = _statsReportService.CalculatePercentiles(studentsList);
            OnPropertyChanged(nameof(Percentiles));

            // Question Correlations
            QuestionCorrelations.Clear();
            foreach (var corr in _statsReportService.AnalyzeQuestionCorrelations(studentsList, 50))
                QuestionCorrelations.Add(corr);

            // Report Text
            ReportText = _statsReportService.GenerateReportSummary(studentsList, SelectedExam?.Title ?? "Mevcut Sınav");
            OnPropertyChanged(nameof(ReportText));
        }

        private async Task RunItemAnalysisAsync()
        {
            if (Students.Count == 0 || AnswerKeys.Count == 0)
            {
                ShowToastWarning("Analiz için öğrenci ve cevap anahtarı gerekli!");
                return;
            }

            IsBusy = true;
            BusyMessage = "Gelişmiş soru analizi yapılıyor...";
            AddToLog("Madde analizi başlatıldı...", LogLevel.Info);

            try
            {
                var studentsCopy = Students.ToList();
                var answerKey = AnswerKeys.FirstOrDefault()?.Answers?.ToCharArray()?.Select(c => c.ToString()).ToArray() ?? Array.Empty<string>();
                int questionCount = AnswerKeys.FirstOrDefault()?.Answers?.Length ?? 0;

                await Task.Run(() =>
                {
                    // Soru istatistikleri
                    var questionStats = _itemAnalysisService.AnalyzeQuestions(studentsCopy, answerKey, questionCount);
                    
                    // Güvenilirlik analizi
                    var reliability = _itemAnalysisService.CalculateReliability(studentsCopy, answerKey, questionCount);
                    
                    // Anomali tespiti
                    var anomalies = _itemAnalysisService.DetectAnomalies(studentsCopy, answerKey, questionCount);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        QuestionStats.Clear();
                        foreach (var stat in questionStats) QuestionStats.Add(stat);

                        ReliabilityStats = reliability;
                        OnPropertyChanged(nameof(ReliabilityStats));

                        Anomalies.Clear();
                        foreach (var anomaly in anomalies) Anomalies.Add(anomaly);
                    });
                });

                AddToLog($"Madde analizi tamamlandı. KR-20: {ReliabilityStats.KR20:F3}, Cronbach's α: {ReliabilityStats.CronbachAlpha:F3}", LogLevel.Success);
                
                if (Anomalies.Count > 0)
                {
                    ShowToastWarning($"{Anomalies.Count} anomali tespit edildi!");
                }
            }
            catch (Exception ex)
            {
                AddToLog($"Madde analizi hatası: {ex.Message}", LogLevel.Error);
                ShowToastError("Analiz sırasında hata oluştu!");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunSuccessPredictionAsync()
        {
            if (Students.Count == 0)
            {
                ShowToastWarning("Tahmin için öğrenci verisi gerekli!");
                return;
            }

            IsBusy = true;
            BusyMessage = "Başarı tahmini yapılıyor...";
            AddToLog("Başarı tahmini analizi başlatıldı...", LogLevel.Info);

            try
            {
                var studentsCopy = Students.ToList();

                await Task.Run(() =>
                {
                    // Sınıf bazlı tahmin özeti
                    var classSummary = _successPredictionService.PredictClassSuccess(studentsCopy, PassingScore);
                    
                    // Bireysel öğrenci tahminleri
                    var predictions = studentsCopy
                        .Select(s => _successPredictionService.PredictStudentSuccess(s, studentsCopy, PassingScore))
                        .OrderByDescending(p => p.PredictedScore)
                        .ToList();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StudentPredictions.Clear();
                        foreach (var pred in predictions) StudentPredictions.Add(pred);

                        ClassPredictionSummary = classSummary;
                        OnPropertyChanged(nameof(ClassPredictionSummary));
                    });
                });

                AddToLog($"Başarı tahmini tamamlandı. Geçme oranı: %{ClassPredictionSummary.PassRate * 100:F1}, Yüksek riskli: {ClassPredictionSummary.HighRiskCount}", LogLevel.Success);
                
                if (ClassPredictionSummary.HighRiskCount > 0)
                {
                    ShowToastWarning($"{ClassPredictionSummary.HighRiskCount} öğrenci yüksek riskli!");
                }
            }
            catch (Exception ex)
            {
                AddToLog($"Başarı tahmini hatası: {ex.Message}", LogLevel.Error);
                ShowToastError("Tahmin sırasında hata oluştu!");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RefreshTemplates()
        {
            Templates.Clear();
            foreach (var template in _templateService.GetAllTemplates())
                Templates.Add(template);
        }

        private void ShowAlert(string title, string message)
        {
            AlertTitle = title;
            AlertMessage = message;
            IsAlertOpen = true;
        }

        // Toast Notification Helper Methods
        public void ShowToast(string message, string title = "", ToastType type = ToastType.Info)
        {
            _notificationService.Show(message, title, type);
        }

        public void ShowToastSuccess(string message, string title = "Başarılı")
        {
            _notificationService.ShowSuccess(message, title);
        }

        public void ShowToastError(string message, string title = "Hata")
        {
            _notificationService.ShowError(message, title);
        }

        public void ShowToastWarning(string message, string title = "Uyarı")
        {
            _notificationService.ShowWarning(message, title);
        }

        public void ShowToastInfo(string message, string title = "Bilgi")
        {
            _notificationService.ShowInfo(message, title);
        }

        public void Dispose()
        {
            _dbService?.Dispose();
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
