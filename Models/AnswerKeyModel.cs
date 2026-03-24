using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OptikFormApp.Models
{
    public class AnswerKeyModel : INotifyPropertyChanged
    {
        private string _bookletName = string.Empty;
        public string BookletName
        {
            get => _bookletName;
            set { _bookletName = value; OnPropertyChanged(); }
        }

        private string _answers = string.Empty;
        public string Answers
        {
            get => _answers;
            set { _answers = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
