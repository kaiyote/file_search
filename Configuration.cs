using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace File_Search
{
    [Serializable]
    public class Configuration
    {
        readonly private static string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "File Search");
        readonly private static string _filename = Path.Combine(_directory, "settings.dat");
        
        private string _rootDirectory;
        private string _notepadPath;
        private List<string> _excludedFolders = new List<string>();
        private List<string> _extensions = new List<string>();

        public string RootDirectory
        {
            get { return _rootDirectory; }
            set { _rootDirectory = value; }
        }

        public string NotepadPath
        {
            get { return _notepadPath; }
            set { _notepadPath = value; }
        }

        public List<string> ExcludedFolders
        {
            get { return _excludedFolders; }
            set { _excludedFolders = value; }
        }

        public List<string> Extensions
        {
            get { return _extensions; }
            set { _extensions = value; }
        }

        public void Save()
        {
            
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);
            using (var file = File.Open(_filename, FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(file, this);
            }
        }

        public static Configuration Load()
        {
            try
            {
                if (File.Exists(_filename))
                {
                    Configuration config;
                    using (var file = File.Open(_filename, FileMode.Open))
                    {
                        var binaryFormatter = new BinaryFormatter();
                        config = (Configuration)binaryFormatter.Deserialize(file);
                    }
                    return config;
                }
            }
            catch (Exception e)
            {
            }

            return new Configuration();
        }
    }
}
