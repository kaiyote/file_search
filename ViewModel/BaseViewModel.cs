using System.Collections.ObjectModel;
using System.ComponentModel;

namespace File_Search.ViewModel
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.  
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        #region Data

        static readonly BaseViewModel DummyChild = new BaseViewModel();

        ObservableCollection<BaseViewModel> _children;
        BaseViewModel _parent;

        bool _isExpanded;
        bool _isSelected;
        internal bool? _isChecked = true;
        internal string _path;

        #endregion // Data

        #region Constructors

        internal BaseViewModel(BaseViewModel parent, bool lazyLoadChildren)
        {
            _parent = parent;

            _children = new ObservableCollection<BaseViewModel>();

            if (lazyLoadChildren)
                _children.Add(DummyChild);
        }

        // This is used to create the DummyChild instance.
        private BaseViewModel()
        {   // fix the issue where updating children iterates before children exist
            _children = new ObservableCollection<BaseViewModel>();
        }

        #endregion // Constructors

        #region Presentation Members

        #region Children

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<BaseViewModel> Children
        {
            get { return _children; }
        }

        #endregion // Children

        #region HasLoadedChildren

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        #endregion // HasLoadedChildren

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                    this.LoadChildren();
                }
            }
        }

        #endregion // IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region IsChecked

        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (value != _isChecked)
                {
                    _isChecked = value;
                    this.OnPropertyChanged("IsChecked");
                    UpdateState();
                }
            }
        }

        #endregion // IsChecked

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        public virtual void LoadChildren()
        {
        }

        #endregion // LoadChildren

        #region Parent

        public BaseViewModel Parent
        {
            get { return _parent; }
        }

        #endregion // Parent

        #region Path

        public string Path
        {
            get { return _path; }
        }

        #endregion

        #endregion // Presentation Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        internal virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members


        protected void UpdateState(bool updateParent = true)
        {
            foreach (BaseViewModel child in Children)
            {
                child._isChecked = IsChecked;
                child.UpdateState(false);
                child.OnPropertyChanged("IsChecked");
            }

            if (updateParent && Parent != null)
                // we know the parent is a directory (files don't have children)
                ((DirectoryViewModel)Parent).UpdateParentalState();
        }
    }
}
