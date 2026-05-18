using Hiemdall_bridge.Helpers;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static Hiemdall_bridge.MessageBoxCustom;
namespace Hiemdall_bridge.ViewModels
{
    public class SequenceLogViewModel : BaseViewModel
    {
        private readonly IBusinessLayer _bl;
        private readonly ILogger _logger;
        private int _srNo = 0;
        private bool _isSearchMode = false;
        public readonly IMessageBoxService _messageBoxService;
        // Buffers to handle live logs during a search
        private readonly List<LogModel> _backupMes = new List<LogModel>();
        private readonly List<LogModel> _incomingDuringSearch = new List<LogModel>();

        // 1. Properties for DataGrid and DatePickers
        public ObservableCollection<LogModel> MesLogs { get; set; } = new ObservableCollection<LogModel>();

        private DateTime? _fromDate = DateTime.Now.AddDays(-1);
        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(); }
        }

        private DateTime? _toDate = DateTime.Now;
        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(); }
        }

        // 2. Commands for Buttons
        public ICommand SearchCommand { get; }
        public ICommand ResetCommand { get; }

        public SequenceLogViewModel(IBusinessLayer bl, ILogger logger, IMessageBoxService messageBoxService)
        {
            _bl = bl;

            // Initialize Commands
            SearchCommand = new RelayCommand(_ => ExecuteSearch());
            ResetCommand = new RelayCommand(_ => ExecuteReset());
            _logger = logger;
            _messageBoxService = messageBoxService;
        }

        // 3. Logic for Live Logging
        public void AddLog(string state, string message)
        {
            try
            {
                var newItem = new LogModel
                {
                    SrNo = ++_srNo,
                    DateTime = DateTime.Now,
                    State = state,
                    Message = message
                };

                if (_isSearchMode)
                {
                    // Buffer logs so we don't mess up the search results
                    _incomingDuringSearch.Insert(0, newItem);
                    if (_incomingDuringSearch.Count > 1000) _incomingDuringSearch.RemoveAt(_incomingDuringSearch.Count - 1);
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MesLogs.Insert(0, newItem);
                    if (MesLogs.Count > 500) MesLogs.RemoveAt(MesLogs.Count - 1);
                });
            }
            catch (Exception ex)
            { _logger.Error("Error in AddLog Method: " + ex.Message);
            }
        }

        // 4. Search Logic
        private void ExecuteSearch()
        {
            try
            {
                if (!FromDate.HasValue || !ToDate.HasValue) return;

                // Normalize dates

                var dateOnly = FromDate.Value;
                TimeSpan fromtime = FromDate.Value.TimeOfDay;

                var dateOnly1 = ToDate.Value;
                TimeSpan totime = ToDate.Value.TimeOfDay;

                DateTime from = dateOnly + fromtime;
                DateTime to = dateOnly1 + totime;

                if (!_isSearchMode)
                {
                    _backupMes.Clear();
                    _backupMes.AddRange(MesLogs);
                    _isSearchMode = true;
                }

                _incomingDuringSearch.Clear();
                MesLogs.Clear();

                DataSet ds = _bl.GetfilterLogs(dateOnly, dateOnly1);
                if (ds != null && ds.Tables.Count > 0)
                { if (ds.Tables[0].Rows.Count > 5000)
                    {
                        _messageBoxService.Show("The total number of rows exceeds 5000. Please reduce the date range.", MessageType.Warning, MessageButtons.Ok); return;
                    }
                    int sr = 1;
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MesLogs.Add(new LogModel
                        {
                            SrNo = sr++,
                            DateTime = Convert.ToDateTime(row["CreatedAt"]),
                            State = row["Type"]?.ToString(),
                            Message = row["Message"]?.ToString()
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Error("Search_Button Method: " + ex.Message);

            }
        }
        // 5. Reset Logic
        private void ExecuteReset()
        {
            try
            {
                FromDate = DateTime.Now.AddDays(-1);
                ToDate = DateTime.Now;

                if (!_isSearchMode) return;

                // Merge buffered logs and previous live logs
                var final = new List<LogModel>(_incomingDuringSearch);
                final.AddRange(_backupMes);

                if (final.Count > 500) final = final.GetRange(0, 500);

                MesLogs.Clear();
                foreach (var item in final) MesLogs.Add(item);

                _incomingDuringSearch.Clear();
                _backupMes.Clear();
                _isSearchMode = false;
            }

            catch (Exception ex)
            {
                _logger.Error("Reset_Button Method: " + ex.Message);

            }
        }
    }
}

