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
            AnswerKeys = new ObservableCollection<AnswerKeyModel>
            {
                new AnswerKeyModel { BookletName = "A", Answers = "" },
                new AnswerKeyModel { BookletName = "B", Answers = "" }
            };

            LoadTxtCommand = new RelayCommand(async _ => await LoadTxtFileAsync());
            EvaluateCommand = new RelayCommand(_ => Evaluate(), _ => Students.Count > 0);
            ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => Students.Count > 0);
            AddAnswerKeyCommand = new RelayCommand(_ => AnswerKeys.Add(new AnswerKeyModel { BookletName = "C", Answers = "" }));
            RemoveAnswerKeyCommand = new RelayCommand(param => {
                if (param is AnswerKeyModel model) AnswerKeys.Remove(model);
            });
        }

        public ObservableCollection<StudentResult> Students { get; set; }
        public ObservableCollection<AnswerKeyModel> AnswerKeys { get; set; }

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

        private async Task LoadTxtFileAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Optik Okuyucu Dosyasını Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                StatusMessage = "Dosya okunuyor, lütfen bekleyin...";
                try
                {
                    var results = await _parserService.ParseFileAsync(openFileDialog.FileName);
                    Students.Clear();
                    foreach (var student in results)
                    {
                        Students.Add(student);
                    }
                    StatusMessage = $"{results.Count} öğrenci başarıyla yüklendi.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Hata: {ex.Message}";
                }
            }
        }

        private void Evaluate()
        {
            if (AnswerKeys.Count == 0)
            {
                StatusMessage = "Lütfen en az 1 cevap anahtarı girin!";
                return;
            }

            try
            {
                var list = new System.Collections.Generic.List<StudentResult>(Students);
                _parserService.EvaluateStudents(list, new System.Collections.Generic.List<AnswerKeyModel>(AnswerKeys));
                
                var temp = list;
                Students.Clear();
                foreach (var st in temp)
                {
                    Students.Add(st);
                }

                StatusMessage = "Değerlendirme başarıyla tamamlandı.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Değerlendirme hatası: {ex.Message}";
            }
        }

        private void ExportToExcel()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Sonuçları Göster",
                FileName = "OptikSonuclar.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _excelService.ExportToExcel(new System.Collections.Generic.List<StudentResult>(Students), saveFileDialog.FileName);
                    StatusMessage = "Excel dosyası başarıyla dışa aktarıldı.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Dışa aktarma hatası: {ex.Message}";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
