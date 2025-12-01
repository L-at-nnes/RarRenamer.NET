using System.ComponentModel;

namespace RarRenamer.Models
{
    public class QueueItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _currentName = string.Empty;
        private string _newName = string.Empty;
        private string _fullPath = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string CurrentName
        {
            get => _currentName;
            set
            {
                if (_currentName != value)
                {
                    _currentName = value;
                    OnPropertyChanged(nameof(CurrentName));
                }
            }
        }

        public string NewName
        {
            get => _newName;
            set
            {
                if (_newName != value)
                {
                    _newName = value;
                    OnPropertyChanged(nameof(NewName));
                }
            }
        }

        public string FullPath
        {
            get => _fullPath;
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    OnPropertyChanged(nameof(FullPath));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
