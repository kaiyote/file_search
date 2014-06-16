using System.Reflection;
using System.Windows;
using File_Search.ViewModel;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;

namespace File_Search
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SearchProcessor _searcher;
        private BackgroundWorker _worker;

        public MainWindow()
        {
            InitializeComponent();

            Container.DataContext = new SearchViewModel
            {
                ExclusionString = Config.ExcludedFolders.Count > 0 ? string.Join(", ", Config.ExcludedFolders) : "bin, obj, Properties",
                ExtensionString = Config.Extensions.Count > 0 ? string.Join(", ", Config.Extensions) : ".cs, .sql, .config, .js, .xml"
            };

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            this.Closing += MainWindow_Closing;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            String resourceName = "File_Search.lib." +

                   new AssemblyName(args.Name).Name + ".dll";

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {

                var assemblyData = new Byte[stream.Length];

                stream.Read(assemblyData, 0, assemblyData.Length);

                return Assembly.Load(assemblyData);

            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Config.Save();
        }

        #region Config Property

        private static Configuration _config;
        private static bool _loaded;

        public static Configuration Config
        {
            get
            {
                if (_loaded)
                    return _config;
                else
                {
                    _config = Configuration.Load();
                    _loaded = true;
                    return _config;
                }
            }
        }

        #endregion

        #region Command Bindings and Events

        private void RootBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Select Root Directory",
                IsFolderPicker = true,
                InitialDirectory = @"C:\",
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = @"C:\",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.RootDirectory = dialog.FileName;
                Config.Save();
                RefreshBinding_Executed(sender, e);
            }
        }

        private void NotepadBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Select Text Editor",
                IsFolderPicker = false,
                InitialDirectory = @"C:\",
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = @"C:\",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.NotepadPath = dialog.FileName;
                Config.Save();
            }
        }

        private void RefreshBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var viewModel = new BaseViewModel(null, false);
            viewModel.Children.Add(new DirectoryViewModel(null, MainWindow.Config.RootDirectory ?? @"C:\"));
            FileTree.DataContext = viewModel;
        }

        private void SaveBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var saveDialog = new CommonSaveFileDialog()
            {
                DefaultExtension = "txt",
                DefaultFileName = "results",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            saveDialog.Filters.Add(new CommonFileDialogFilter("Text files", "*.txt"));
            saveDialog.Filters.Add(new CommonFileDialogFilter("All files", "*.*"));

            if (saveDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var file = new StreamWriter(saveDialog.FileName);
                foreach (var item in _searcher.Results.Keys)
                {
                    file.WriteLine(item);
                    foreach (var occurrence in _searcher.ResultsContent[item])
                        file.WriteLine("\tLine " + occurrence.Key + ": " + occurrence.Value);
                    file.WriteLine();
                }
                file.Flush();
                file.Close();
            }
        }

        private void QuitBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentList.Items.Clear();
            if (FileList.Items.Count > 0)
            {
                var key = ((KeyValuePair<string, int>)FileList.Items[FileList.SelectedIndex]).Key;
                foreach (var occurrence in _searcher.ResultsContent[key])
                {
                    ContentList.Items.Add(occurrence);
                }
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenFileFromFileList("explorer");
        }

        private void FileList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFileFromFileList(Config.NotepadPath);
        }

        private void OpenFileFromFileList(string openWith)
        {
            var file = ((KeyValuePair<string, int>)FileList.Items[FileList.SelectedIndex]).Key;
            _searcher.OpenFile(file, openWith);
        }

        #endregion

        #region Search and BackgroundWorker

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            FileList.Items.Clear();
            ContentList.Items.Clear();
            var parms = (SearchViewModel)Container.DataContext;
            _searcher = new SearchProcessor(parms.SearchText, parms.ExclusionList, parms.ExtensionList, (BaseViewModel)FileTree.DataContext, parms.UseRegex, parms.SearchFileNames);

            _worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };

            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            _worker.ProgressChanged += worker_ProgressChanged;

            _worker.RunWorkerAsync();
            SearchButton.IsEnabled = false;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var entry in _searcher.Results)
            {
                FileList.Items.Add(entry);
            }
            // we don't want the progress bar to sit at some random percent when we cancel
            ProgressBar.Value = 100;
            SearchButton.IsEnabled = true;
            FileList.SelectedIndex = 0;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _searcher.Search((BackgroundWorker)sender, e);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FileList.Items.Clear();
            ProgressBar.Value = 0;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_worker != null)
                _worker.CancelAsync();
        }

        #endregion
    }
}
