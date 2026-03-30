using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using OptikFormApp.Models;
using OptikFormApp.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Data;

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
        private bool _isGeneralConfigOpen;
        private string _defaultExcelPath = "";
        private int _themeIndex;
        private int _layoutIndex;
        private bool _hasUnsavedData;
        private string _searchText = "";
        private string _schoolName = "Okul Adı";
        private double _netCoefficient = 1.0;
        private double _baseScore = 0.0;
        private double _wrongDeductionFactor = 0.25;
        private bool _isBusy;
        private string _busyMessage = "";
        private string _newOutcomeName = "";
        private string _newOutcomeRange = "";
        private string _newOutcomeBooklet = "A";

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

            // Kalıcı ayarları yükle
            var saved = _settingsService.Load();
            _schoolName = saved.SchoolName;
            _defaultExcelPath = saved.DefaultExcelPath;
            _netCoefficient = saved.NetCoefficient;
            _baseScore = saved.BaseScore;
            _wrongDeductionFactor = saved.WrongDeductionFactor;
            _themeIndex = saved.ThemeIndex;
            _layoutIndex = saved.LayoutIndex;
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
                    ShowAlert("Dışa Aktarma Hatası", "Önce veri yüklemeniz ve puanları hesaplamanız gerekmektedir.");
                    return;
                }
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Öğrenci Karnelerinin Kaydedileceği Klasörü Seçin" };
                if (dialog.ShowDialog() == true) {
                    IsBusy = true;
                    BusyMessage = "Toplu PDF karneleri oluşturuluyor...";
                    try {
                        AddToLog("Toplu PDF karneleri oluşturuluyor...");
                        var studentsCopy = new List<StudentResult>(Students);
                        var outcomesCopy = LearningOutcomes.ToList();
                        string folder = dialog.FolderName;
                        await Task.Run(() => _pdfService.GenerateStudentReports(studentsCopy, studentsCopy, outcomesCopy, folder, SchoolName));
                        ShowAlert("Başarılı", "Tüm öğrenci karneleri seçilen klasöre PDF olarak kaydedildi.");
                        AddToLog($"{Students.Count} adet öğrenci karnesi PDF olarak dışa aktarıldı.", LogLevel.Success);
                    } catch (Exception ex) {
                        AddToLog($"PDF oluşturma hatası: {ex.Message}", LogLevel.Error);
                        ShowAlert("Hata", $"PDF raporu oluşturulurken hata oluştu: {ex.Message}");
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
                    await _dbService.DeleteCourseAsync(c.Id);
                    await LoadCoursesAsync();
                    AddToLog($"'{c.Name}' dersi silindi.", LogLevel.Warning);
                }
            });
            DeleteExamCommand = new AsyncRelayCommand(async obj => {
                if (obj is ExamEntry e) {
                    await _dbService.DeleteExamAsync(e.Id);
                    if (SelectedCourse != null) await LoadExamsForCourseAsync(SelectedCourse.Id);
                    SelectedExam = null;
                    Students.Clear(); Statistics.Clear(); ValidationIssues.Clear(); AccuracyData.Clear(); ScoreDistData.Clear();
                    AddToLog($"'{e.Title}' sınavı silindi.", LogLevel.Warning);
                }
            });
            SaveExamCommand = new AsyncRelayCommand(async _ => {
                string title = string.IsNullOrWhiteSpace(NewExamName) ? $"{DateTime.Now:dd.MM.yyyy} Sınavı" : NewExamName;
                await SaveCurrentExamAsync(title, SelectedExam);
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

            ShowShortcutsCommand = new RelayCommand(_ => IsShortcutsOpen = true);
            CloseShortcutsCommand = new RelayCommand(_ => IsShortcutsOpen = false);

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
        }

        public string SearchText 
        { 
            get => _searchText; 
            set { _searchText = value; OnPropertyChanged(); StudentsView.Refresh(); } 
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
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is StudentResult student)
            {
                return student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       student.StudentId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }
            return true;
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
        private bool _isRenameModalOpen;
        public string RenameModalTitle { get => _renameModalTitle; set { _renameModalTitle = value; OnPropertyChanged(); } }
        private string _renameModalTitle = "";
        public string RenameInput1 { get => _renameInput1; set { _renameInput1 = value; OnPropertyChanged(); } }
        private string _renameInput1 = "";
        public string RenameInput2 { get => _renameInput2; set { _renameInput2 = value; OnPropertyChanged(); } }
        private string _renameInput2 = "";
        public bool ShowRenameInput2 { get => _showRenameInput2; set { _showRenameInput2 = value; OnPropertyChanged(); } }
        private bool _showRenameInput2;
        private string _renameContext = "";
        private int _renameId = 0;

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
        public bool IsUISettingsOpen { get => _isUISettingsOpen; set { _isUISettingsOpen = value; OnPropertyChanged(); } }
        public bool IsGeneralConfigOpen { get => _isGeneralConfigOpen; set { _isGeneralConfigOpen = value; OnPropertyChanged(); } }
        public string DefaultExcelPath { get => _defaultExcelPath; set { _defaultExcelPath = value; OnPropertyChanged(); SaveSettings(); } }
        
        public int ThemeIndex { get => _themeIndex; set { _themeIndex = value; OnPropertyChanged(); ApplyTheme(value == 1); SaveSettings(); } }
        public int LayoutIndex { get => _layoutIndex; set { _layoutIndex = value; OnPropertyChanged(); GridRowHeight = value == 0 ? 32 : 50; GridCellPadding = value == 0 ? new System.Windows.Thickness(10, 0, 10, 0) : new System.Windows.Thickness(15, 0, 15, 0); SaveSettings(); } }
        public int GridRowHeight { get => _gridRowHeight; set { _gridRowHeight = value; OnPropertyChanged(); } }
        private int _gridRowHeight = 32;
        public System.Windows.Thickness GridCellPadding { get => _gridCellPadding; set { _gridCellPadding = value; OnPropertyChanged(); } }
        private System.Windows.Thickness _gridCellPadding = new System.Windows.Thickness(10, 0, 10, 0);

        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string BusyMessage { get => _busyMessage; set { _busyMessage = value; OnPropertyChanged(); } }

        public string NewOutcomeName { get => _newOutcomeName; set { _newOutcomeName = value; OnPropertyChanged(); } }
        public string NewOutcomeRange { get => _newOutcomeRange; set { _newOutcomeRange = value; OnPropertyChanged(); } }
        public string NewOutcomeBooklet { get => _newOutcomeBooklet; set { _newOutcomeBooklet = value; OnPropertyChanged(); } }

        public bool HasUnsavedData { get => _hasUnsavedData; set { _hasUnsavedData = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaveStatusText)); } }
        public string SaveStatusText => _hasUnsavedData ? "⚠️ Kaydedilmedi" : (Students.Count > 0 ? "✅ Kaydedildi" : "");

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
        public ICommand CloseAboutCommand { get; set; }
        public ICommand ShowShortcutsCommand { get; set; }
        public ICommand CloseShortcutsCommand { get; set; }
        public ICommand ExportGradeListCommand { get; set; }
        public ICommand OpenUISettingsCommand { get; set; }
        public ICommand CloseUISettingsCommand { get; set; }
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

        public async Task SaveCurrentExamAsync(string title, ExamEntry? existingExam = null)
        {
            if (SelectedCourse == null)
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
                        CourseId = SelectedCourse.Id,
                        Title = title,
                        Date = DateTime.Now,
                        ConfigJson = JsonSerializer.Serialize(config)
                    };
                    await _dbService.SaveExamAsync(exam, new List<StudentResult>(Students));
                    AddToLog($"'{title}' sınavı veritabanına kaydedildi.", LogLevel.Success);
                }
                await LoadExamsForCourseAsync(SelectedCourse.Id);
                HasUnsavedData = false;
            }
            catch (Exception ex)
            {
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
                LayoutIndex = _layoutIndex
            });
        }

        private void ApplyTheme(bool dark) {
            if (System.Windows.Application.Current == null) return;
            var res = System.Windows.Application.Current.Resources;
            var cc = new System.Windows.Media.BrushConverter();
            res["AppBg"] = cc.ConvertFromString(dark ? "#0F172A" : "#F4F7FB");
            res["CardBg"] = cc.ConvertFromString(dark ? "#1E293B" : "White");
            res["TextMain"] = cc.ConvertFromString(dark ? "#F8FAFC" : "#1E293B");
            res["TextMuted"] = cc.ConvertFromString(dark ? "#94A3B8" : "#64748B");
            res["Border"] = cc.ConvertFromString(dark ? "#334155" : "#E2E8F0");
            res["HeaderBg"] = cc.ConvertFromString(dark ? "#0F172A" : "#F1F5F9");
            res["AltRowBg"] = cc.ConvertFromString(dark ? "#0F172A" : "#F8FAFC");
            res["HoverBg"] = cc.ConvertFromString(dark ? "#334155" : "#EFF6FF");
            res["ModalBackdrop"] = cc.ConvertFromString(dark ? "#B3000000" : "#800F172A");
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
                SelectedExam = null; // Unselect from sidebar
                Students.Clear(); AnswerKeys.Clear();
                Statistics.Clear(); ValidationIssues.Clear(); AccuracyData.Clear(); ScoreDistData.Clear();
                
                StatusMessage = "Dosya okunuyor, lütfen bekleyin...";
                try
                {
                    AddToLog($"{System.IO.Path.GetFileName(openFileDialog.FileName)} dosyası okunuyor...");
                    var (students, answerKeys, errors) = await _parserService.ParseFileAsync(openFileDialog.FileName);
                    bool isCritical = errors.Any(e => e.StartsWith("KRİTİK HATA"));
                    if (errors.Count > 0)
                    {
                        foreach (var err in errors) AddToLog(err, LogLevel.Error);
                        if (isCritical)
                        {
                            ShowAlert("Geçersiz Dosya Formatı", errors.First(e => e.StartsWith("KRİTİK HATA")) + "\n\nLütfen seçtiğiniz dosyanın doğru optik okuyucu çıktısı (.txt) olduğundan emin olun.");
                            StatusMessage = "Dosya formatı geçersiz.";
                            return;
                        }
                        string errorSummary = string.Join("\n", errors.Take(5));
                        if (errors.Count > 5) errorSummary += $"\n...ve {errors.Count - 5} hata daha.";
                        ShowAlert("Dosya Ayrıştırma Sorunları", $"Dosyada bazı hatalı satırlar tespit edildi:\n{errorSummary}\n\nGeçerli olan {students.Count} kayıt yüklendi.");
                    }
                    if (answerKeys.Count > 0)
                    {
                        AnswerKeys.Clear();
                        foreach(var ak in answerKeys) AnswerKeys.Add(ak);
                        AddToLog($"{answerKeys.Count} adet cevap anahtarı yüklendi.", LogLevel.Success);
                    }
                    Students.Clear();
                    int rowNum = 1;
                    foreach (var student in students) { student.RowNumber = rowNum++; Students.Add(student); }
                    if (students.Count > 0)
                    {
                        HasUnsavedData = true;
                        StatusMessage = $"{students.Count} öğrenci başarıyla yüklendi.";
                        AddToLog($"{students.Count} öğrenci kaydı başarıyla yüklendi.", LogLevel.Success);
                        bool hasValidKey = false;
                        foreach(var key in AnswerKeys) { if(!string.IsNullOrWhiteSpace(key.Answers)) hasValidKey = true; }
                        if (hasValidKey) await EvaluateAsync();
                    }
                    else
                    {
                        StatusMessage = "Dosyada yüklenebilir öğrenci kaydı bulunamadı.";
                        AddToLog("Yüklenebilir kayıt bulunamadı. Lütfen dosya formatını kontrol edin.", LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Hata: {ex.Message}";
                    AddToLog($"Yükleme hatası: {ex.Message}", LogLevel.Error);
                }
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
                    OnPropertyChanged(nameof(ValidationIssuesCount));
                });

                StatusMessage = "Değerlendirme başarıyla tamamlandı.";
                AddToLog("Değerlendirme başarıyla tamamlandı.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                StatusMessage = "Hesaplama sırasında teknik bir sorun oluştu.";
                AddToLog($"Hesaplama hatası (Teknik Detay): {ex.Message}", LogLevel.Error);
                ShowAlert("Hesaplama Hatası", "Puanlar hesaplanırken bir sorun oluştu. Lütfen verilerinizi ve cevap anahtarını kontrol edin.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateOutcomeStats()
        {
            if (Students.Count == 0) return;

            // 1. First, calculate booklet-specific stats for each outcome row
            foreach (var outcome in LearningOutcomes)
            {
                int localPossible = 0; int localCorrect = 0;
                foreach (var student in Students)
                {
                    if (string.Equals(student.BookletType, outcome.BookletName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var qNum in outcome.QuestionNumbers)
                        {
                            if (qNum > 0 && qNum <= student.QuestionResults.Count)
                            {
                                localPossible++;
                                if (student.QuestionResults[qNum - 1]) localCorrect++;
                            }
                        }
                    }
                }
                outcome.TotalQuestions = localPossible;
                outcome.CorrectCount = localCorrect;
                outcome.SuccessRate = localPossible > 0 ? Math.Round((double)localCorrect / localPossible * 100, 1) : 0;
            }

            // 2. Second, calculate global (merged) stats by topic name
            var groups = LearningOutcomes.GroupBy(o => o.Name.Trim().ToLowerInvariant());
            foreach (var group in groups)
            {
                int globalCorrect = 0; int globalTotal = 0;
                
                // Map booklets to outcomes in this group for fast lookup
                var bookletMap = group.ToDictionary(o => o.BookletName.ToUpperInvariant(), o => o);

                foreach (var student in Students)
                {
                    string stdBooklet = (student.BookletType ?? "A").ToUpperInvariant();
                    if (bookletMap.TryGetValue(stdBooklet, out var outcomeDef))
                    {
                        foreach (var qNum in outcomeDef.QuestionNumbers)
                        {
                            if (qNum > 0 && qNum <= student.QuestionResults.Count)
                            {
                                globalTotal++;
                                if (student.QuestionResults[qNum - 1]) globalCorrect++;
                            }
                        }
                    }
                }

                double globalRate = globalTotal > 0 ? Math.Round((double)globalCorrect / globalTotal * 100, 1) : 0;
                foreach (var outcome in group)
                {
                    outcome.GlobalCorrectCount = globalCorrect;
                    outcome.GlobalTotalQuestions = globalTotal;
                    outcome.GlobalSuccessRate = globalRate;
                }
            }
            OnPropertyChanged(nameof(LearningOutcomes));
            
            // Log the calculation completion
            AddToLog($"Kazanım odaklı başarı analizi tamamlandı: {LearningOutcomes.Count} kazanımdan {groups.Count()} benzersiz konu hesaplandı.", LogLevel.Success);
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

        private void ShowAlert(string title, string message)
        {
            AlertTitle = title;
            AlertMessage = message;
            IsAlertOpen = true;
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
