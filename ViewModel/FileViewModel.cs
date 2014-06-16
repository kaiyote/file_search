using System.Linq;

namespace File_Search.ViewModel
{
    class FileViewModel : BaseViewModel
    {
        public FileViewModel(DirectoryViewModel parent, string path)
            : base(parent, false)
        {
            _path = path;
        }

        public string Name
        {
            get { return _path.Split('\\').Last(); }
        }
    }
}
