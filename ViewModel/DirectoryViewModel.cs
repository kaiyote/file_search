using System;
using System.IO;
using System.Linq;

namespace File_Search.ViewModel
{
    class DirectoryViewModel : BaseViewModel
    {
        public DirectoryViewModel(DirectoryViewModel parent, string path)
            : base(parent, true)
        {
            _path = path;
        }

        public override void LoadChildren()
        {
            // Load the sub-directories of this directory
            foreach (string directory in Directory.EnumerateDirectories(_path).Where(x => CanAccessDirectory(x) && (new DirectoryInfo(x).Attributes & FileAttributes.Hidden) == 0).Select(x => x))
                this.Children.Add(new DirectoryViewModel(this, directory));

            // Load the files in this directory
            foreach (string file in Directory.EnumerateFiles(_path).Where(x => CanAccessFile(x) && (new FileInfo(x).Attributes & FileAttributes.Hidden) == 0).Select(x => x))
                this.Children.Add(new FileViewModel(this, file));
        }

        public static bool CanAccessDirectory(string folderPath)
        {
            try
            {
                // I've found GetAccessControl to not be entirely sufficient to throw an exception for protected directories
                Directory.GetAccessControl(folderPath);
                // if we make it past GetAccessControl, we should also check if we can get at the files in the directory
                // C:\Program Files (x86)\Google\Crash Reports is a directory I've found that requires this check
                Directory.EnumerateFiles(folderPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool CanAccessFile(string filePath)
        {
            try
            {
                File.GetAccessControl(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Name
        {
            get { return _path.Split('\\').Last(x => !string.IsNullOrEmpty(x)); }
        }

        internal void UpdateParentalState()
        {
            bool childUnchecked = false;
            bool childChecked = false;
            bool childHalfChecked = false;

            foreach (BaseViewModel child in Children)
                if (child.IsChecked == true)
                    childChecked = true;
                else if (child.IsChecked == null)
                    childHalfChecked = true;
                else
                    childUnchecked = true;

            if (childHalfChecked || (childUnchecked && childChecked))
            {
                _isChecked = null;
                this.OnPropertyChanged("IsChecked");
            }
            else if (childChecked && !childUnchecked && !childHalfChecked)
            {
                _isChecked = true;
                this.OnPropertyChanged("IsChecked");
            }
            else
            {
                _isChecked = false;
                this.OnPropertyChanged("IsChecked");
            }

            // continue up the parent chain
            if (Parent != null)
                ((DirectoryViewModel)Parent).UpdateParentalState();
        }
    }
}
