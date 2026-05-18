using Hiemdall_bridge.Models;
using Hiemdall_bridge.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hiemdall_bridge.View_Models
{
    public class LogViewModel : BaseViewModel
    {
        public ObservableCollection<LogItem> LogItems { get; }
            = new ObservableCollection<LogItem>();
    }

}
