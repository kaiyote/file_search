using File_Search.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace File_Search.Controls
{
    /// <summary>
    /// Interaction logic for FileTreeControl.xaml
    /// </summary>
    public partial class FileTreeControl : UserControl
    {
        public FileTreeControl()
        {
            InitializeComponent();

            var viewModel = new BaseViewModel(null, false);

            viewModel.Children.Add(new DirectoryViewModel(null, MainWindow.Config.RootDirectory ?? @"C:\"));
            base.DataContext = viewModel;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            ((CheckBox)sender).IsChecked = ((CheckBox)sender).IsChecked ?? false;
        }
    }
}
