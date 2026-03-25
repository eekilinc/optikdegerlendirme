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

namespace OptikFormApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly OpticalParserService _parserService;
        private readonly ExcelExportService _excelService;
        private readonly ValidationService _validationService;
        private readonly PdfReportService _pdfService;
        
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
        private string _saveStatusText = "";

        public MainViewModel()
        {
            _parserService = new OpticalParserService();
            _excelService = new ExcelExportService();
            _validationService = new ValidationService();
            _pdfService = new PdfReportService();
            _dbService = new DatabaseService();
            
            Students = new ObservableCollection<StudentResult>();
            AnswerKeys = new ObservableCollection<AnswerKeyModel>();
            Statistics = new ObservableCollection<QuestionStatisticItem>();
            ValidationIssues = new ObservableCollection<ValidationIssue>();
            Courses = new ObservableCollection<Course>();
            CourseExams = new ObservableCollection<ExamEntry>();
            
            LoadCourses();

            AnswerKeys.Add(new AnswerKeyModel { BookletName = "A", Answers = "" });

            LoadTxtCommand = new RelayCommand(async _ => await LoadTxtFileAsync());
            EvaluateCommand = new RelayCommand(_ => Evaluate(), _ => Students.Count > 0);
            
            AddToLog("Uygulama hazır. Lütfen bir optik veri (.txt) dosyası yükleyin.", LogLevel.Info);
            
            AddAnswerKeyCommand = new RelayCommand(_ => AnswerKeys.Add(new AnswerKeyModel { BookletName = "B", Answers = "" }));
            RemoveAnswerKeyCommand = new RelayCommand(param => {
                if (param is AnswerKeyModel model) AnswerKeys.Remove(model);
            });

            OpenModalCommand = new RelayCommand(_ => IsModalOpen = true);
            CloseModalCommand = new RelayCommand(_ => { IsModalOpen = false; if (Students.Count > 0) { Evaluate(); AutoSaveSelectedExam(); } });
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
            CloseQuestionSettingsCommand = new RelayCommand(_ => { IsQuestionSettingsOpen = false; if (Students.Count > 0) { Evaluate(); AutoSaveSelectedExam(); } });

            OpenLearningOutcomesCommand = new RelayCommand(_ => IsLearningOutcomesOpen = true);
            CloseLearningOutcomesCommand = new RelayCommand(_ => {
                IsLearningOutcomesOpen = false;
                Evaluate();
                AutoSaveSelectedExam();
            });
            
            AddOutcomeCommand = new RelayCommand(_ => LearningOutcomes.Add(new LearningOutcome { Name = "Yeni Konu" }));
            RemoveOutcomeCommand = new RelayCommand(p => { if (p is LearningOutcome lo) LearningOutcomes.Remove(lo); });

            CloseAlertCommand = new RelayCommand(_ => IsAlertOpen = false);
            OpenAboutCommand = new RelayCommand(_ => IsAboutOpen = true);
            CloseAboutCommand = new RelayCommand(_ => IsAboutOpen = false);
            OpenUISettingsCommand = new RelayCommand(_ => IsUISettingsOpen = true);
            CloseUISettingsCommand = new RelayCommand(_ => IsUISettingsOpen = false);
            OpenGeneralConfigCommand = new RelayCommand(_ => IsGeneralConfigOpen = true);
            CloseGeneralConfigCommand = new RelayCommand(_ => IsGeneralConfigOpen = false);

            SelectFolderCommand = new RelayCommand(_ => {
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Excel Dosyaları İçin Varsayılan Klasörü Seçin" };
                if (dialog.ShowDialog() == true) {
                    DefaultExcelPath = dialog.FolderName;
                }
            });

            ExitCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

            ExportExcelCommand = new RelayCommand(_ => {
                if (Students.Count == 0) {
                    ShowAlert("Dışa Aktarma Hatası", "Dışa aktarılacak öğrenci kaydı bulunamadı.");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(DefaultExcelPath) && System.IO.Directory.Exists(DefaultExcelPath)) {
                    string filePath = System.IO.Path.Combine(DefaultExcelPath, $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    _excelService.ExportToExcel(new System.Collections.Generic.List<StudentResult>(Students), new System.Collections.Generic.List<QuestionStatisticItem>(Statistics), LearningOutcomes, filePath);
                    ShowAlert("Başarılı", $"Excel raporu varsayılan dizinize kaydedildi:\n{filePath}");
                } else {
                    var sfd = new SaveFileDialog { Filter = "Excel Dosyası|*.xlsx", FileName = $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
                    if (sfd.ShowDialog() == true) {
                        _excelService.ExportToExcel(new System.Collections.Generic.List<StudentResult>(Students), new System.Collections.Generic.List<QuestionStatisticItem>(Statistics), LearningOutcomes, sfd.FileName);
                        StatusMessage = "Excel'e aktarım tamamlandı.";
                        AddToLog($"Excel raporu oluşturuldu: {System.IO.Path.GetFileName(sfd.FileName)}", LogLevel.Success);
                    }
                }
            });

            ExportPdfCommand = new RelayCommand(async _ => {
                if (Students.Count == 0) {
                    ShowAlert("Dışa Aktarma Hatası", "Önce veri yüklemeniz ve puanları hesaplamanız gerekmektedir.");
                    return;
                }
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Öğrenci Karnelerinin Kaydedileceği Klasörü Seçin" };
                if (dialog.ShowDialog() == true) {
                    try {
                        AddToLog("Toplu PDF karneleri oluşturuluyor...");
                        var studentsCopy = new System.Collections.Generic.List<StudentResult>(Students);
                        var outcomesCopy = LearningOutcomes.ToList();
                        string folder = dialog.FolderName;
                        await System.Threading.Tasks.Task.Run(() => _pdfService.GenerateStudentReports(studentsCopy, studentsCopy, outcomesCopy, folder));
                        ShowAlert("Başarılı", "Tüm öğrenci karneleri seçilen klasöre PDF olarak kaydedildi.");
                        AddToLog($"{Students.Count} adet öğrenci karnesi PDF olarak dışa aktarıldı.", LogLevel.Success);
                    } catch (Exception ex) {
                        AddToLog($"PDF oluşturma hatası: {ex.Message}", LogLevel.Error);
                        ShowAlert("Hata", $"PDF raporu oluşturulurken hata oluştu: {ex.Message}");
                    }
                }
            });

            ExportSinglePdfCommand = new RelayCommand(async p => {
                if (p is StudentResult student) {
                    var sfd = new SaveFileDialog { 
                        Filter = "PDF Dosyası|*.pdf", 
                        FileName = $"Karne_{student.StudentId}_{student.FullName.Replace(" ", "_")}.pdf",
                        Title = "Öğrenci Karnesini Kaydet"
                    };
                    if (sfd.ShowDialog() == true) {
                        try {
                            var allStudentsCopy = new System.Collections.Generic.List<StudentResult>(Students);
                            var outcomesCopy = LearningOutcomes.ToList();
                            string dir = System.IO.Path.GetDirectoryName(sfd.FileName) ?? "";
                            await System.Threading.Tasks.Task.Run(() => _pdfService.GenerateStudentReports(new System.Collections.Generic.List<StudentResult> { student }, allStudentsCopy, outcomesCopy, dir));
                            StatusMessage = $"{student.FullName} için karne oluşturuldu.";
                            AddToLog($"{student.FullName} için PDF karne oluşturuldu.", LogLevel.Success);
                        } catch (Exception ex) {
                            AddToLog($"Karne hatası: {ex.Message}", LogLevel.Error);
                            ShowAlert("Hata", $"Karne oluşturulurken hata oluştu: {ex.Message}");
                        }
                    }
                }
            });

            ShowAddCourseCommand = new RelayCommand(_ => IsAddCourseOpen = true);
            CloseAddCourseCommand = new RelayCommand(_ => IsAddCourseOpen = false);
            AddCourseCommand = new RelayCommand(_ => {
                if (string.IsNullOrWhiteSpace(NewCourseName)) return;
                var c = new Course { Code = NewCourseCode, Name = NewCourseName };
                _dbService.SaveCourse(c);
                LoadCourses();
                IsAddCourseOpen = false;
                NewCourseCode = ""; NewCourseName = "";
                AddToLog($"'{c.Name}' dersi eklendi.", LogLevel.Success);
            });
            DeleteCourseCommand = new RelayCommand(obj => {
                if (obj is Course c) {
                    _dbService.DeleteCourse(c.Id);
                    LoadCourses();
                    AddToLog($"'{c.Name}' dersi silindi.", LogLevel.Warning);
                }
            });
            DeleteExamCommand = new RelayCommand(obj => {
                if (obj is ExamEntry e) {
                    _dbService.DeleteExam(e.Id);
                    if (SelectedCourse != null) LoadExamsForCourse(SelectedCourse.Id);
                    SelectedExam = null;
                    Students.Clear(); Statistics.Clear(); ValidationIssues.Clear(); AccuracyData.Clear(); ScoreDistData.Clear();
                    AddToLog($"'{e.Title}' sınavı silindi.", LogLevel.Warning);
                }
            });
            SaveExamCommand = new RelayCommand(_ => {
                string title = string.IsNullOrWhiteSpace(NewExamName) ? $"{DateTime.Now:dd.MM.yyyy} Sınavı" : NewExamName;
                SaveCurrentExam(title, SelectedExam);
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
            SaveRenameCommand = new RelayCommand(_ => {
                if (_renameContext == "Course") {
                    if (string.IsNullOrWhiteSpace(RenameInput1) || string.IsNullOrWhiteSpace(RenameInput2)) return;
                    _dbService.RenameCourse(_renameId, RenameInput1, RenameInput2);
                    LoadCourses();
                    if (SelectedCourse != null && SelectedCourse.Id == _renameId) {
                        SelectedCourse.Code = RenameInput1;
                        SelectedCourse.Name = RenameInput2;
                        OnPropertyChanged(nameof(SelectedCourse));
                    }
                    AddToLog($"Ders düzenlendi: {RenameInput2}", LogLevel.Success);
                } else if (_renameContext == "Exam") {
                    if (string.IsNullOrWhiteSpace(RenameInput1)) return;
                    _dbService.RenameExam(_renameId, RenameInput1);
                    if (SelectedCourse != null) LoadExamsForCourse(SelectedCourse.Id);
                    if (SelectedExam != null && SelectedExam.Id == _renameId) {
                        SelectedExam.Title = RenameInput1;
                        OnPropertyChanged(nameof(SelectedExam));
                    }
                    AddToLog($"Sınav yeniden adlandırıldı: {RenameInput1}", LogLevel.Success);
                }
                IsRenameModalOpen = false;
            });

            ApplyTheme(false);
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
            set { _selectedCourse = value; OnPropertyChanged(); if (value != null) LoadExamsForCourse(value.Id); else CourseExams.Clear(); }
        }

        public ExamEntry? SelectedExam
        {
            get => _selectedExam;
            set { _selectedExam = value; OnPropertyChanged(); if (value != null) LoadExamData(value); }
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
        public bool IsUISettingsOpen { get => _isUISettingsOpen; set { _isUISettingsOpen = value; OnPropertyChanged(); } }
        public bool IsGeneralConfigOpen { get => _isGeneralConfigOpen; set { _isGeneralConfigOpen = value; OnPropertyChanged(); } }
        public string DefaultExcelPath { get => _defaultExcelPath; set { _defaultExcelPath = value; OnPropertyChanged(); } }
        
        public int ThemeIndex { get => _themeIndex; set { _themeIndex = value; OnPropertyChanged(); ApplyTheme(value == 1); } }
        public int LayoutIndex { get => _layoutIndex; set { _layoutIndex = value; OnPropertyChanged(); GridRowHeight = value == 0 ? 32 : 50; GridCellPadding = value == 0 ? new System.Windows.Thickness(10, 0, 10, 0) : new System.Windows.Thickness(15, 0, 15, 0); } }
        public int GridRowHeight { get => _gridRowHeight; set { _gridRowHeight = value; OnPropertyChanged(); } }
        private int _gridRowHeight = 32;
        public System.Windows.Thickness GridCellPadding { get => _gridCellPadding; set { _gridCellPadding = value; OnPropertyChanged(); } }
        private System.Windows.Thickness _gridCellPadding = new System.Windows.Thickness(10, 0, 10, 0);

        public bool HasUnsavedData { get => _hasUnsavedData; set { _hasUnsavedData = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaveStatusText)); } }
        public string SaveStatusText => _hasUnsavedData ? "⚠️ Kaydedilmedi" : (Students.Count > 0 ? "✅ Kaydedildi" : "");

        // Commands
        public ICommand LoadTxtCommand { get; set; }
        public ICommand EvaluateCommand { get; set; }
        public ICommand ExportExcelCommand { get; set; }
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
        public ICommand AddOutcomeCommand { get; set; }
        public ICommand RemoveOutcomeCommand { get; set; }
        public ICommand SelectFolderCommand { get; set; }
        public ICommand CloseAlertCommand { get; set; }
        public ICommand OpenAboutCommand { get; set; }
        public ICommand CloseAboutCommand { get; set; }
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

        private void LoadCourses()
        {
            var list = _dbService.GetCourses();
            Courses.Clear();
            foreach (var c in list) Courses.Add(c);
        }

        private void LoadExamsForCourse(int courseId)
        {
            var list = _dbService.GetExamsForCourse(courseId);
            CourseExams.Clear();
            foreach (var e in list) CourseExams.Add(e);
            if (SelectedExam == null) Students.Clear();
        }

        private void LoadExamData(ExamEntry exam)
        {
            try
            {
                AddToLog($"{exam.Title} sınav verileri yükleniyor...");
                var results = _dbService.GetResultsForExam(exam.Id);
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
                    }
                }
                Evaluate();
                HasUnsavedData = false;
                AddToLog($"{exam.Title} başarıyla yüklendi.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddToLog($"Sınav yükleme hatası: {ex.Message}", LogLevel.Error);
            }
        }

        public void SaveCurrentExam(string title, ExamEntry? existingExam = null)
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
                    LearningOutcomes = new List<LearningOutcome>(LearningOutcomes)
                };

                if (existingExam != null && existingExam.Id > 0)
                {
                    // Update existing exam
                    existingExam.Title = title;
                    existingExam.ConfigJson = JsonSerializer.Serialize(config);
                    _dbService.UpdateExam(existingExam, new List<StudentResult>(Students));
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
                    _dbService.SaveExam(exam, new List<StudentResult>(Students));
                    AddToLog($"'{title}' sınavı veritabanına kaydedildi.", LogLevel.Success);
                }
                LoadExamsForCourse(SelectedCourse.Id);
                HasUnsavedData = false;
            }
            catch (Exception ex)
            {
                AddToLog($"Kaydetme hatası: {ex.Message}", LogLevel.Error);
                ShowAlert("Hata", $"Kayıt sırasında hata oluştu: {ex.Message}");
            }
        }

        private void AutoSaveSelectedExam()
        {
            if (SelectedExam == null || SelectedExam.Id <= 0 || SelectedCourse == null) return;
            try
            {
                var config = new ExamConfigData
                {
                    AnswerKeys = new List<AnswerKeyModel>(AnswerKeys),
                    QuestionSettings = new List<QuestionSetting>(QuestionSettings),
                    LearningOutcomes = new List<LearningOutcome>(LearningOutcomes)
                };
                SelectedExam.ConfigJson = JsonSerializer.Serialize(config);
                _dbService.UpdateExam(SelectedExam, new List<StudentResult>(Students));
                AddToLog($"'{SelectedExam.Title}' değişiklikler otomatik kaydedildi.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                AddToLog($"Otomatik kaydetme hatası: {ex.Message}", LogLevel.Error);
            }
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
                        if (hasValidKey) Evaluate();
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

        private void Evaluate()
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
            AddToLog("Puanlar hesaplanıyor ve madde analizi yapılıyor...");
            try
            {
                var list = new System.Collections.Generic.List<StudentResult>(Students);
                var keys = new System.Collections.Generic.List<AnswerKeyModel>(AnswerKeys);
                var settings = new System.Collections.Generic.List<QuestionSetting>(QuestionSettings);
                _parserService.EvaluateStudents(list, keys, settings);
                Students.Clear(); foreach (var st in list) Students.Add(st);
                Statistics.Clear();
                var statsList = _parserService.CalculateStatistics(list, keys);
                foreach(var stat in statsList) Statistics.Add(stat);
                ValidationIssues.Clear();
                var issues = _validationService.Validate(list, keys);
                foreach (var issue in issues) ValidationIssues.Add(issue);
                UpdateChartData(statsList, list);
                UpdateOutcomeStats();
                StatusMessage = "Değerlendirme başarıyla tamamlandı.";
                AddToLog("Değerlendirme başarıyla tamamlandı.", LogLevel.Success);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Değerlendirme hatası: {ex.Message}";
                AddToLog($"Değerlendirme hatası: {ex.Message}", LogLevel.Error);
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
