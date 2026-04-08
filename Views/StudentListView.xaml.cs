using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OptikFormApp.Models;
using OptikFormApp.ViewModels;

namespace OptikFormApp.Views
{
    public partial class StudentListView : UserControl
    {
        public StudentListView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is StudentResult student)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.OpenStudentDetailCommand.Execute(student);
                }
            }
        }
    }
}
