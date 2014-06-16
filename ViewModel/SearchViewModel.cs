using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace File_Search.ViewModel
{
    class SearchViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private bool _useRegex;
        private bool _searchFileNames;
        private string _exclusionList;
        private string _extensionList;

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value != _searchText)
                {
                    _searchText = value;
                    this.OnPropertyChanged("SearchText");
                }
            }
        }

        public bool UseRegex
        {
            get { return _useRegex; }
            set
            {
                if (value != _useRegex)
                {
                    _useRegex = value;
                    this.OnPropertyChanged("UseRegex");
                }
            }
        }

        public bool SearchFileNames
        {
            get { return _searchFileNames; }
            set
            {
                if (value != _searchFileNames)
                {
                    _searchFileNames = value;
                    this.OnPropertyChanged("SearchFileNames");
                }
            }
        }

        public string ExclusionString
        {
            get { return _exclusionList; }
            set
            {
                if (value != _exclusionList)
                {
                    _exclusionList = value;
                    MainWindow.Config.ExcludedFolders = this.ExclusionList;
                    this.OnPropertyChanged("ExclusionString");
                }
            }
        }

        public List<string> ExclusionList
        {
            get { return _exclusionList.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToList(); }
        }

        public string ExtensionString
        {
            get { return _extensionList; }
            set
            {
                if (value != _extensionList)
                {
                    _extensionList = value;
                    MainWindow.Config.Extensions = this.ExtensionList;
                    this.OnPropertyChanged("ExtensionString");
                }
            }
        }

        public List<string> ExtensionList
        {
            get { return _extensionList.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()).ToList(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
