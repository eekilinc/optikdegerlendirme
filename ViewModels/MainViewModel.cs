using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using OptikFormApp.Models;
using OptikFormApp.Services;

namespace OptikFormApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly OpticalParserService _parserService;
        private readonly ExcelExportService _excelService;

        public MainViewModel()
        {
            _parserService = new OpticalParserService();
            _excelService = new ExcelExportService();
            
            Students = new ObservableCollection<StudentResult>();
            Statistics = new ObservableCollection<QuestionStatisticItem>();
            AnswerKeys = new ObservableCollection<AnswerKeyModel>
            {
                new AnswerKeyModel { BookletName = "A", Answers = "" }
            };

            LoadTxtCommand = new RelayCommand(async _ => await LoadTxtFileAsync());
            EvaluateCommand = new RelayCommand(_ => Evaluate(), _ => Students.Count > 0);
            
            AddAnswerKeyCommand = new RelayCommand(_ => AnswerKeys.Add(new AnswerKeyModel { BookletName = "B", Answers = "" }));
            RemoveAnswerKeyCommand = new RelayCommand(param => {
                if (param is AnswerKeyModel model) AnswerKeys.Remove(model);
            });

            OpenModalCommand = new RelayCommand(_ => IsModalOpen = true);
            CloseModalCommand = new RelayCommand(_ => IsModalOpen = false);
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
                    _excelService.ExportToExcel(new System.Collections.Generic.List<StudentResult>(Students), filePath);
                    ShowAlert("Başarılı", $"Excel raporu varsayılan dizinize kaydedildi:\n{filePath}");
                } else {
                    var sfd = new SaveFileDialog { Filter = "Excel Dosyası|*.xlsx", FileName = $"OptikSonuclar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
                    if (sfd.ShowDialog() == true) {
                        _excelService.ExportToExcel(new System.Collections.Generic.List<StudentResult>(Students), sfd.FileName);
                        StatusMessage = "Excel'e aktarım tamamlandı.";
                    }
                }
            });

            ApplyTheme(false);
        }

        public ObservableCollection<StudentResult> Students { get; set; }
        public ObservableCollection<AnswerKeyModel> AnswerKeys { get; set; }
        public ObservableCollection<QuestionStatisticItem> Statistics { get; set; }

        private bool _isModalOpen;
        public bool IsModalOpen
        {
            get => _isModalOpen;
            set { _isModalOpen = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "İşlem Bekleniyor. TXT dosyası yükleyin.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand LoadTxtCommand { get; }
        public RelayCommand EvaluateCommand { get; }
        public RelayCommand ExportExcelCommand { get; }
        
        public RelayCommand AddAnswerKeyCommand { get; }
        public RelayCommand RemoveAnswerKeyCommand { get; }
        public RelayCommand OpenModalCommand { get; }
        public RelayCommand CloseModalCommand { get; }
        public RelayCommand SelectFolderCommand { get; }

        private bool _isAlertOpen;
        public bool IsAlertOpen { get => _isAlertOpen; set { _isAlertOpen = value; OnPropertyChanged(); } }
        private string _alertTitle = string.Empty;
        public string AlertTitle { get => _alertTitle; set { _alertTitle = value; OnPropertyChanged(); } }
        private string _alertMessage = string.Empty;
        public string AlertMessage { get => _alertMessage; set { _alertMessage = value; OnPropertyChanged(); } }
        public RelayCommand CloseAlertCommand { get; }

        private void ShowAlert(string title, string message)
        {
            AlertTitle = title;
            AlertMessage = message;
            IsAlertOpen = true;
        }

        private bool _isAboutOpen;
        public bool IsAboutOpen { get => _isAboutOpen; set { _isAboutOpen = value; OnPropertyChanged(); } }

        private bool _isUISettingsOpen;
        public bool IsUISettingsOpen { get => _isUISettingsOpen; set { _isUISettingsOpen = value; OnPropertyChanged(); } }

        private bool _isGeneralConfigOpen;
        public bool IsGeneralConfigOpen { get => _isGeneralConfigOpen; set { _isGeneralConfigOpen = value; OnPropertyChanged(); } }
        
        public RelayCommand OpenAboutCommand { get; }
        public RelayCommand CloseAboutCommand { get; }

        public RelayCommand OpenUISettingsCommand { get; }
        public RelayCommand CloseUISettingsCommand { get; }

        public RelayCommand OpenGeneralConfigCommand { get; }
        public RelayCommand CloseGeneralConfigCommand { get; }
        
        public RelayCommand ExitCommand { get; }

        private string _defaultExcelPath = "";
        public string DefaultExcelPath { get => _defaultExcelPath; set { _defaultExcelPath = value; OnPropertyChanged(); } }

        private int _themeIndex;
        public int ThemeIndex {
            get => _themeIndex;
            set {
                _themeIndex = value;
                OnPropertyChanged();
                ApplyTheme(value == 1);
            }
        }
        
        private int _layoutIndex;
        public int LayoutIndex {
            get => _layoutIndex;
            set {
                _layoutIndex = value;
                OnPropertyChanged();
                GridRowHeight = value == 0 ? 32 : 50;
                GridCellPadding = value == 0 ? new System.Windows.Thickness(10, 0, 10, 0) : new System.Windows.Thickness(15, 0, 15, 0);
            }
        }

        private int _gridRowHeight = 32;
        public int GridRowHeight { get => _gridRowHeight; set { _gridRowHeight = value; OnPropertyChanged(); } }

        private System.Windows.Thickness _gridCellPadding = new System.Windows.Thickness(10, 0, 10, 0);
        public System.Windows.Thickness GridCellPadding { get => _gridCellPadding; set { _gridCellPadding = value; OnPropertyChanged(); } }

        private void ApplyTheme(bool dark) {
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

            // Override OS Native WPF Colors for Menus, ContextMenus, and Popups
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
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Optik Okuyucu Dosyasını Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                StatusMessage = "Dosya okunuyor, lütfen bekleyin...";
                try
                {
                    var parsedData = await _parserService.ParseFileAsync(openFileDialog.FileName);
                    
                    if (parsedData.answerKeys.Count > 0)
                    {
                        AnswerKeys.Clear();
                        foreach(var ak in parsedData.answerKeys)
                            AnswerKeys.Add(ak);
                    }

                    Students.Clear();
                    foreach (var student in parsedData.students)
                    {
                        Students.Add(student);
                    }
                    StatusMessage = $"{parsedData.students.Count} öğrenci başarıyla yüklendi. {(parsedData.answerKeys.Count > 0 ? $"({parsedData.answerKeys.Count} Cevap Anahtarı Okundu)" : "")}";
                    
                    // Otomatik Değerlendirme Tetikle
                    bool hasValidKey = false;
                    foreach(var key in AnswerKeys) {
                        if(!string.IsNullOrWhiteSpace(key.Answers)) hasValidKey = true;
                    }
                    if (hasValidKey && Students.Count > 0)
                    {
                        Evaluate();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Hata: {ex.Message}";
                }
            }
        }

        private void Evaluate()
        {
            bool hasValidKey = false;
            foreach(var key in AnswerKeys) 
            {
                if(!string.IsNullOrWhiteSpace(key.Answers)) hasValidKey = true;
            }
            
            if (!hasValidKey)
            {
                StatusMessage = "Cevap anahtarı girilmeden hesaplama yapılamaz!";
                ShowAlert("Eksik Cevap Anahtarı", "Lütfen puanları hesaplamadan veya dosyayı içe aktarmadan önce 'Cevap Anahtarlarını Yönet' bölümünden bir cevap şablonu (Örn: A) doldurunuz.");
                return;
            }

            try
            {
                var list = new System.Collections.Generic.List<StudentResult>(Students);
                var keys = new System.Collections.Generic.List<AnswerKeyModel>(AnswerKeys);
                
                _parserService.EvaluateStudents(list, keys);
                
                var temp = list;
                Students.Clear();
                foreach (var st in temp)
                {
                    Students.Add(st);
                }

                Statistics.Clear();
                var statsList = _parserService.CalculateStatistics(list, keys);
                foreach(var stat in statsList)
                {
                    Statistics.Add(stat);
                }

                StatusMessage = "Değerlendirme başarıyla tamamlandı.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Değerlendirme hatası: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
