using File_Search.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace File_Search
{
    class SearchProcessor
    {
        #region InstanceFields

        private string _searchString;
        private List<string> _exclusions;
        private List<string> _extensions;
        private Dictionary<string, int> _results;
        private Dictionary<string, Dictionary<int, string>> _resultsContent;

        private List<string> _skippedFiles;
        private bool _isRegex;
        private bool _searchFileNames;
        private Regex _regex;
        private List<string> _searchPaths;

        #endregion

        #region Properties

        public List<string> SkippedFiles
        {
            get { return _skippedFiles; }
        }

        public Dictionary<string, Dictionary<int, string>> ResultsContent
        {
            get { return _resultsContent; }
        }

        public Dictionary<string, int> Results
        {
            get { return _results; }
        }

        #endregion

        #region Constructor

        public SearchProcessor(string searchString, List<string> exclusions, List<string> extensions, BaseViewModel directoryTree, bool isRegex, bool searchFileNames)
        {
            _searchString = searchString;
            _exclusions = exclusions;
            _extensions = extensions;
            _skippedFiles = new List<string>();
            _searchFileNames = searchFileNames;
            _isRegex = isRegex;
            if (isRegex)
                _regex = new Regex(searchString);

            _searchPaths = new List<string>();
            GenerateSearchPaths(directoryTree);
        }

        #endregion

        #region Work Methods

        // This will be fired asynchronously (it can take forever to run)
        // As such, we need to pass it the control it will be binding
        // when it's done so that we can fire and forget
        public void Search(BackgroundWorker worker, DoWorkEventArgs e)
        {
            _results = new Dictionary<string, int>();
            _resultsContent = new Dictionary<string, Dictionary<int, string>>();

            var totalLength = _searchPaths.Count * _extensions.Count;
            var completedChunks = 0;

            foreach (var path in _searchPaths)
            {
                if (IsCancelled(worker, e))
                    break;

                foreach (var extension in _extensions)
                {
                    if (IsCancelled(worker, e))
                        break;

                    DirectorySearch(path, extension, worker, e);

                    worker.ReportProgress((int)(((double)++completedChunks / totalLength) * 100));
                }
            }
        }

        private void DirectorySearch(string path, string extension, BackgroundWorker worker, DoWorkEventArgs e)
        {
            // dig through all accessible files in the directory
            foreach (var file in Directory.EnumerateFiles(path, "*" + extension).Where(x => DirectoryViewModel.CanAccessFile(x) && (new FileInfo(x).Attributes & FileAttributes.Hidden) == 0).Select(x => x))
            {
                if (IsCancelled(worker, e))
                    break;

                if (extension == ".*" || file.EndsWith(extension) && !ShouldExclude(file))
                {
                    if (_searchFileNames && SearchFileName(file))
                    {
                        _results.Add(file, 0);
                        _resultsContent.Add(file, new Dictionary<int, string>());
                    }
                    else if (!_searchFileNames)
                    {
                        var occurences = SearchFile(file);

                        if (occurences.Count > 0)
                        {
                            var key = file;
                            if (!_results.ContainsKey(key))
                            {
                                _results.Add(key, occurences.Count);
                                _resultsContent.Add(key, occurences);
                            }
                        }
                    }
                }
            }

            // run this same method on all accessible sub-directories
            foreach (var directory in Directory.EnumerateDirectories(path).Where(x => DirectoryViewModel.CanAccessDirectory(x) && (new DirectoryInfo(x).Attributes & FileAttributes.Hidden) == 0).Select(x => x))
            {
                if (IsCancelled(worker, e))
                    break;

                DirectorySearch(directory, extension, worker, e);
            }
        }

        #endregion

        #region Utility Methods

        private Dictionary<int, string> SearchFile(string filePath)
        {
            try
            {
                var file = new StreamReader(filePath);
                var lines = new Dictionary<int, string>();

                var count = 1;
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    if ((_isRegex && _regex.IsMatch(line)) || line.ToLower().Contains(_searchString.ToLower()))
                        lines.Add(count, line);
                    count++;
                }

                file.Close();
                return lines;
            }
            catch (Exception e)
            {
                SkippedFiles.Add(filePath);
                return new Dictionary<int, string>();
            }
        }

        private bool SearchFileName(string filePath)
        {
            return _isRegex ? _regex.IsMatch(filePath) : filePath.ToLower().Contains(_searchString.ToLower());
        }

        private bool ShouldExclude(string filePath)
        {
            foreach (var path in _exclusions)
                if (filePath.ToLower().Contains(path.ToLower()))
                    return true;
            return false;
        }

        private void GenerateSearchPaths(BaseViewModel root)
        {
            foreach (var node in root.Children)
            {
                // if its checked, everything below it is checked and so we don't care about its children
                if (node.IsChecked == true)
                    _searchPaths.Add(node.Path);
                // null = indeterminate, so now we go and check all of its children
                else if (node.IsChecked == null)
                    GenerateSearchPaths(node);
            }
        }

        private bool IsCancelled(BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return true;
            }
            return false;
        }

        #endregion

        #region File Handling Methods

        public void OpenFile(string filePath, string editorPath)
        {
            ExecuteShellCommand(editorPath, filePath);
        }

        public void OpenDirectory(string filePath)
        {
            var directory = filePath.Substring(0, filePath.LastIndexOf(@"\"));
            ExecuteShellCommand("explorer", directory);
        }

        #endregion

        #region Execute Shell Command

        private void ExecuteShellCommand(string program, string arguments)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo("\"" + program + "\"", arguments)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
            }
            catch (Exception e)
            {
                return;
            }
        }

        #endregion
    }
}
