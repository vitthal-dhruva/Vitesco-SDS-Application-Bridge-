using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiemdall_bridge.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _isAdmin;
        public string? _CurUser { get; set; }
        public string? CurUser { get => _CurUser; set { _CurUser = value; OnPropertyChanged(); } }
        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
