using System;
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
            _notificationService = new NotificationService();
            _undoRedoManager = new UndoRedoManager(50);
            _statsReportService = new StatisticsReportService();
            _templateService = new TemplateService();
            _jsonDataService = new JsonDataService();
            _shortcutService = new KeyboardShortcutService();

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

            // Toast Notification Commands
            DismissNotificationCommand = new RelayCommand(param => {
                if (param is string notificationId) {
                    _notificationService.Dismiss(notificationId);
                }
            });

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

            // Template Manager Commands
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
                // Tam eşleşme veya fuzzy search
                return student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       student.StudentId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       FuzzySearchService.ContainsFuzzy(student.FullName, SearchText, 0.6) ||
                       FuzzySearchService.ContainsFuzzy(student.StudentId, SearchText, 0.7);
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
        public ICommand UndoCommand { get; set; }
        public ICommand RedoCommand { get; set; }
        public ICommand OpenGitHubCommand { get; set; }

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
                    foreach (var err in errors) AddToLog(err, LogLevel.Error);
                    if (isCritical)
                    {
                        ShowToastError("Dosya formatı geçersiz. Lütfen optik okuyucu çıktısı (.txt) olduğundan emin olun.");
                        StatusMessage = "Dosya formatı geçersiz.";
                        return;
                    }
                    string errorSummary = string.Join("\n", errors.Take(5));
                    if (errors.Count > 5) errorSummary += $"\n...ve {errors.Count - 5} hata daha.";
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
            int questionCount = Math.Min(Students.FirstOrDefault()?.QuestionResults.Count ?? 0, 50);

            for (int i = 0; i < questionCount; i++)
            {
                var questionNum = i + 1;
                var correctCount = Students.Count(s => s.QuestionResults[i]);
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
