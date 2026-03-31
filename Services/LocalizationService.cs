using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace OptikFormApp.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public enum Language
        {
            Turkish,
            English
        }

        private Language _currentLanguage = Language.Turkish;
        private Dictionary<string, string> _currentTranslations = new();

        public Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                LoadTranslations(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private LocalizationService()
        {
            LoadTranslations(_currentLanguage);
        }

        private void LoadTranslations(Language language)
        {
            _currentTranslations = language switch
            {
                Language.Turkish => GetTurkishTranslations(),
                Language.English => GetEnglishTranslations(),
                _ => GetTurkishTranslations()
            };
        }

        public string this[string key]
        {
            get => _currentTranslations.TryGetValue(key, out var value) ? value : key;
        }

        public string Translate(string key, params object[] args)
        {
            var translation = this[key];
            return args.Length > 0 ? string.Format(translation, args) : translation;
        }

        private Dictionary<string, string> GetTurkishTranslations()
        {
            return new Dictionary<string, string>
            {
                // General
                ["AppTitle"] = "AĞLASUN MYO Optik Değerlendirme",
                ["Welcome"] = "Hoş Geldiniz",
                ["Loading"] = "Yükleniyor...",
                ["Save"] = "Kaydet",
                ["Cancel"] = "İptal",
                ["Close"] = "Kapat",
                ["Delete"] = "Sil",
                ["Edit"] = "Düzenle",
                ["Add"] = "Ekle",
                ["Remove"] = "Kaldır",
                ["Refresh"] = "Yenile",
                ["Search"] = "Ara",
                ["Filter"] = "Filtrele",
                ["Export"] = "Dışa Aktar",
                ["Import"] = "İçe Aktar",
                ["Print"] = "Yazdır",
                ["Settings"] = "Ayarlar",
                ["Help"] = "Yardım",
                ["About"] = "Hakkında",
                ["Exit"] = "Çıkış",

                // Menu
                ["Menu_File"] = "Dosya",
                ["Menu_Course"] = "Ders Yönetimi",
                ["Menu_Operations"] = "İşlemler",
                ["Menu_Options"] = "Seçenekler",
                ["Menu_Help"] = "Yardım",

                // File Operations
                ["File_LoadTxt"] = "Optik Verisi Yükle (.txt)",
                ["File_SaveExam"] = "Sınavı Veritabanına Kaydet",
                ["File_ExportExcel"] = "Excel Raporu Al (.xlsx)",
                ["File_ExportCsv"] = "CSV Olarak Dışa Aktar (.csv)",
                ["File_ExportPdf"] = "Toplu PDF Karne Al (.pdf)",
                ["File_ExportJson"] = "JSON'a Aktar...",
                ["File_ImportJson"] = "JSON'dan Yükle...",

                // Tabs
                ["Tab_Students"] = "Öğrenci İşlemleri",
                ["Tab_Statistics"] = "Madde Analizi",
                ["Tab_Charts"] = "Grafiksel Analiz",
                ["Tab_Outcomes"] = "Kazanım Değerlendirmesi",
                ["Tab_Difficulty"] = "Zorluk Seviyesi Analizi",
                ["Tab_StatisticalReports"] = "İstatistiksel Raporlar",

                // Student Operations
                ["Student_ID"] = "Öğrenci No",
                ["Student_Name"] = "Ad Soyad",
                ["Student_Booklet"] = "Kitapçık",
                ["Student_Score"] = "Puan",
                ["Student_Net"] = "Net",
                ["Student_Rank"] = "Sıra",
                ["Student_Correct"] = "Doğru",
                ["Student_Incorrect"] = "Yanlış",
                ["Student_Empty"] = "Boş",

                // Statistics
                ["Stats_Average"] = "Ortalama",
                ["Stats_Median"] = "Medyan",
                ["Stats_StdDev"] = "Standart Sapma",
                ["Stats_SuccessRate"] = "Başarı Oranı",
                ["Stats_TotalStudents"] = "Toplam Öğrenci",
                ["Stats_Passed"] = "Geçen",
                ["Stats_Failed"] = "Kalan",

                // Templates
                ["Template_Manager"] = "Sınav Şablonlarını Yönet",
                ["Template_SaveCurrent"] = "Mevcut Ayarları Şablon Olarak Kaydet",
                ["Template_Name"] = "Şablon Adı",
                ["Template_Description"] = "Açıklama",
                ["Template_Load"] = "Şablon Yükle",
                ["Template_Export"] = "Şablon Dışa Aktar",
                ["Template_Import"] = "Şablon İçe Aktar",

                // Notifications
                ["Notify_Success"] = "Başarılı",
                ["Notify_Error"] = "Hata",
                ["Notify_Warning"] = "Uyarı",
                ["Notify_Info"] = "Bilgi",

                // Messages
                ["Msg_NoData"] = "Veri yok",
                ["Msg_NoStudents"] = "Öğrenci kaydı bulunamadı",
                ["Msg_NoAnswerKey"] = "Cevap anahtarı girilmemiş",
                ["Msg_Loading"] = "Yükleniyor, lütfen bekleyin...",
                ["Msg_Calculating"] = "Puanlar hesaplanıyor...",
                ["Msg_Exporting"] = "Dışa aktarılıyor...",
                ["Msg_Importing"] = "İçe aktarılıyor...",

                // Language
                ["Lang_Turkish"] = "Türkçe",
                ["Lang_English"] = "English",
                ["Lang_Change"] = "Dil Değiştir",
            };
        }

        private Dictionary<string, string> GetEnglishTranslations()
        {
            return new Dictionary<string, string>
            {
                // General
                ["AppTitle"] = "AĞLASUN MYO Optical Evaluation",
                ["Welcome"] = "Welcome",
                ["Loading"] = "Loading...",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["Close"] = "Close",
                ["Delete"] = "Delete",
                ["Edit"] = "Edit",
                ["Add"] = "Add",
                ["Remove"] = "Remove",
                ["Refresh"] = "Refresh",
                ["Search"] = "Search",
                ["Filter"] = "Filter",
                ["Export"] = "Export",
                ["Import"] = "Import",
                ["Print"] = "Print",
                ["Settings"] = "Settings",
                ["Help"] = "Help",
                ["About"] = "About",
                ["Exit"] = "Exit",

                // Menu
                ["Menu_File"] = "File",
                ["Menu_Course"] = "Course Management",
                ["Menu_Operations"] = "Operations",
                ["Menu_Options"] = "Options",
                ["Menu_Help"] = "Help",

                // File Operations
                ["File_LoadTxt"] = "Load Optical Data (.txt)",
                ["File_SaveExam"] = "Save Exam to Database",
                ["File_ExportExcel"] = "Export Excel Report (.xlsx)",
                ["File_ExportCsv"] = "Export as CSV (.csv)",
                ["File_ExportPdf"] = "Export PDF Reports (.pdf)",
                ["File_ExportJson"] = "Export to JSON...",
                ["File_ImportJson"] = "Import from JSON...",

                // Tabs
                ["Tab_Students"] = "Student Operations",
                ["Tab_Statistics"] = "Item Analysis",
                ["Tab_Charts"] = "Graphical Analysis",
                ["Tab_Outcomes"] = "Outcome Assessment",
                ["Tab_Difficulty"] = "Difficulty Level Analysis",
                ["Tab_StatisticalReports"] = "Statistical Reports",

                // Student Operations
                ["Student_ID"] = "Student ID",
                ["Student_Name"] = "Full Name",
                ["Student_Booklet"] = "Booklet",
                ["Student_Score"] = "Score",
                ["Student_Net"] = "Net",
                ["Student_Rank"] = "Rank",
                ["Student_Correct"] = "Correct",
                ["Student_Incorrect"] = "Incorrect",
                ["Student_Empty"] = "Empty",

                // Statistics
                ["Stats_Average"] = "Average",
                ["Stats_Median"] = "Median",
                ["Stats_StdDev"] = "Standard Deviation",
                ["Stats_SuccessRate"] = "Success Rate",
                ["Stats_TotalStudents"] = "Total Students",
                ["Stats_Passed"] = "Passed",
                ["Stats_Failed"] = "Failed",

                // Templates
                ["Template_Manager"] = "Manage Exam Templates",
                ["Template_SaveCurrent"] = "Save Current Settings as Template",
                ["Template_Name"] = "Template Name",
                ["Template_Description"] = "Description",
                ["Template_Load"] = "Load Template",
                ["Template_Export"] = "Export Template",
                ["Template_Import"] = "Import Template",

                // Notifications
                ["Notify_Success"] = "Success",
                ["Notify_Error"] = "Error",
                ["Notify_Warning"] = "Warning",
                ["Notify_Info"] = "Info",

                // Messages
                ["Msg_NoData"] = "No data",
                ["Msg_NoStudents"] = "No student records found",
                ["Msg_NoAnswerKey"] = "Answer key not entered",
                ["Msg_Loading"] = "Loading, please wait...",
                ["Msg_Calculating"] = "Calculating scores...",
                ["Msg_Exporting"] = "Exporting...",
                ["Msg_Importing"] = "Importing...",

                // Language
                ["Lang_Turkish"] = "Turkish",
                ["Lang_English"] = "English",
                ["Lang_Change"] = "Change Language",
            };
        }

        public void SaveLanguagePreference()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.config");
            File.WriteAllText(configPath, CurrentLanguage.ToString());
        }

        public void LoadLanguagePreference()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.config");
            if (File.Exists(configPath))
            {
                var savedLang = File.ReadAllText(configPath);
                if (Enum.TryParse<Language>(savedLang, out var lang))
                {
                    CurrentLanguage = lang;
                }
            }
        }
    }
}
