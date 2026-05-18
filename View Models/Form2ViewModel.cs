using Hiemdall_bridge;
using Hiemdall_bridge.Helpers;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Interfaces;
using Hiemdall_bridge.Models;
using Hiemdall_bridge.View_Models;
using Hiemdall_bridge.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using static Hiemdall_bridge.MessageBoxCustom;
using ILogger = Hiemdall_bridge.Interface.ILogger;


public class Form2ViewModel : BaseViewModel
{
    #region Private Fields
    private int srNoTimer = 0;
    private int MSGIDCommnad = 1;
    private DispatcherTimer? _timer;
    private Brush _mesLedBrush = Brushes.Red;
    private Brush _plcLedBrush = Brushes.Red;
    private UserModel _currentUser;
    public string? Equipment = "";
    public string? IPAddress = "";
    public int port = 0;
    private bool _opcconnection = false;
    public int Timercount = 0;
    public int maxrecord = 100;
    public Action? RequestClose { get; set; }
    private readonly IBusinessLayer _bl;
    private readonly IOpcClient _opc;
    private readonly ITcpClient _tcp;
    private readonly ILogger _logger;
    private readonly INavigationService _navigation;
    public readonly IMessageBoxService _messageBoxService;
    public readonly IAppConfiguration _configuration;
    List<TagModel> tags;
    private bool _isTimerRunning = false;
    private System.Timers.Timer _dbCleanupTimer;
    // new method added
    // Thread-safe async message queue
    private readonly Channel<string> _tcpMessageChannel =
    Channel.CreateBounded<string>(new BoundedChannelOptions(2000)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private CancellationTokenSource _tcpProcessorCts;
    private Task _tcpProcessorTask;
    private readonly SemaphoreSlim _pingLock = new SemaphoreSlim(1, 1);
    //new method
    #endregion

    #region Global Properties
    public int MSGIDTimer { get; set; } = 1;
    //public int MaxRecords { get; set; } = 100;
    public ConfigurationModel Config { get; set; }

    public UserModel CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(Username));
        }
    }

    public string Role => CurrentUser?.Role;
    public string Username => CurrentUser?.Username;
    #endregion

    #region Tab-Specific ViewModels
    // Tab 1: Configuration / Main
    public LogViewModel LogVM { get; }

    // Tab 2: Tag Configuration
    public TagConfigViewModel TagConfigVM { get; }

    // Tab 3: Sequence Logs
    public SequenceLogViewModel SequenceLogVM { get; set; }

    // Tab 4: User Management
    public UserAuthViewModel UserAuthVM { get; }
    #endregion

    #region UI Property Status (LEDs/Brushes)
    public Brush PLCLedBrush
    {
        get => _plcLedBrush;
        set
        {
            _plcLedBrush = value;
            OnPropertyChanged();
        }
    }

    public Brush MesLedBrush
    {
        get => _mesLedBrush;
        set
        {
            _mesLedBrush = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Commands
    public ICommand SaveConfigCommand { get; private set; }
    public ICommand ResetConfigCommand { get; private set; }
    public ICommand logoutCommand { get; private set; }
    #endregion

    #region Constructor
    public Form2ViewModel(
        IBusinessLayer bl, SequenceLogViewModel sequenceLogVM,
        IOpcClient opc, TagConfigViewModel tagConfigVM, UserAuthViewModel userAuthVM,
        ITcpClient tcp, IMessageBoxService messageBoxService, IAppConfiguration appConfiguration,
        ILogger logger, INavigationService navigation)
    {
        _bl = bl;
        _opc = opc;
        _tcp = tcp;
        TagConfigVM = tagConfigVM;
        SequenceLogVM = sequenceLogVM;
        _configuration = appConfiguration;
        UserAuthVM = userAuthVM;
        _logger = logger;
        _messageBoxService = messageBoxService;
        _navigation = navigation;

        Config = new ConfigurationModel();
        LogVM = new LogViewModel();

        MESconfdatabinding();
        _ = InitializeCommands();
    }
    #endregion

    #region Initialization Methods
    private async Task InitializeCommands()
    {
        try
        {

            SaveConfigCommand = new RelayCommand(_ => SaveConfiguration());
            ResetConfigCommand = new RelayCommand(_ => ResetConfiguration());
            logoutCommand = new RelayCommand(_ => Logout());
            _opc.OnCommandReceived += Opc_OnCommandReceived;
            _opc.OnConnectionChange += ConnectionChange;

            _tcp.OnMessageReceived += Tcp_OnMessageReceived;
            _tcp.OnDisconnected += Tcp_OnDisconnected;
            _tcp.OnReconnected += Tcp_OnReconnected;
            if (string.IsNullOrEmpty(_configuration.Current.OPCUrl) || string.IsNullOrEmpty(_configuration.Current.CommandID))
            {
                _messageBoxService.Show("OPC URL is empty. Please check App.config.", MessageType.Error, MessageButtons.Ok);

                return;
            }
            await _opc.ConnectAsync(_configuration.Current.OPCUrl, _configuration.Current.CommandID);

            string ip = Config.IPAddress.Trim();
            int port = Config.Port;
            if (string.IsNullOrEmpty(ip) || port == 0)
            {
                _messageBoxService.Show("Please enter valid IP Address and Port", MessageType.Error, MessageButtons.Ok);
                return;
            }
            await _tcp.ConnectAsync(ip, Convert.ToInt32(port));
            //_ = ProcessTcpMessagesAsync();
            StartTcpProcessor();
            StartSendTimer();
            StartDatabaseCleanup();
        }
        catch (Exception ex)
        {

            _logger.Error("Initialize Method : Need To Restart" + ex.Message);
        }
    }

    private void MESconfdatabinding()
    {
        try
        {
            DataTable dt = _bl.GetConfugurationValues();
            if (dt != null && dt.Rows.Count > 0)
            {
                Equipment = Config.StationName = dt.Rows[0]["StationName"].ToString();
                IPAddress = Config.IPAddress = dt.Rows[0]["IpAddress"].ToString();
                port = Config.Port = Convert.ToInt32(dt.Rows[0]["PortNO"].ToString());
                maxrecord = Config.MaxRows = Convert.ToInt32(dt.Rows[0]["RecordDisplay"].ToString());
                Timercount = Config.Timer = Convert.ToInt32(dt.Rows[0]["TimerTime"].ToString());
            }
            maxrecord = maxrecord > 100 ? 100 : maxrecord;
            Timercount = Timercount <= 0 ? 10 : Timercount;
        }
        catch (Exception ex)
        {
            _logger.Error("Error in MESconfdatabinding " + ex.Message);
        }
    }
    #endregion

    #region Logic: Timer & TCP Communication
    private void StartSendTimer()
    {
        try
        {
            // int intervalSeconds = Config.Timer;
            // int intervalSeconds = Timercount;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(Timercount);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        catch (Exception ex)
        {
            _logger.Error("StartSendTimer Method: " + ex.Message);
        }
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        if (_isTimerRunning)
            return;

        _isTimerRunning = true;
        try
        {
            if (_tcp.IsConnected)
            {
                string TiSt = DateTime.Now.ToString("yyyyMMddHHmmss");
                string count = MSGIDTimer.ToString();
                if (string.IsNullOrEmpty(Equipment))
                {
                    _messageBoxService.Show("Please enter valid Equipment ID", MessageType.Warning, MessageButtons.Ok);
                    return;
                }
                MSGIDTimer++;
                string dynamicMessage = $"PING,{Equipment},{count},{TiSt}";

                MSGIDTimer = MSGIDTimer >= 1000 ? 1 : MSGIDTimer;

                _ = Task.Run(async () =>
                {
                    // 🔥 If already sending → skip (NO WAIT)
                    if (!await _pingLock.WaitAsync(0))
                        return;

                    try
                    {
                        await _tcp.SendAsync(dynamicMessage);
                        AddLog("Machine to MES", dynamicMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("PING send failed: " + ex.Message);
                    }
                    finally
                    {
                        _pingLock.Release();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Timer_Tick: " + ex.Message);
        }
        finally
        {
            _isTimerRunning = false;
        }
    }
    private void Tcp_OnMessageReceived(string message)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(message))
                return;

            // Remove control characters
            message = new string(message
                .Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t')
                .ToArray())
                .Trim();

            // 🔥 HANDLE PING SEPARATELY (DO NOT QUEUE)
            if (message.Contains("PING"))
            {
                _ = Task.Run(() => HandlePingAsync(message)); // fire & forget
                return;
            }

            // ✅ ONLY IMPORTANT MESSAGES GO TO QUEUE
            // Push message into async queue
            _ = _tcpMessageChannel.Writer.WriteAsync(message);
        }
        catch (Exception ex)
        {
            _logger.Error("Tcp_OnMessageReceived Error: " + ex.Message);
        }
    }
    private async Task HandlePingAsync(string message)
    {
        try
        {
            if (!_opcconnection)
                return;

            var ackValue = ConvertToDataType("True", _configuration.Current.PingDatatype);
            var timerValue = ConvertToDataType(Timercount.ToString(), _configuration.Current.pingtimedatatype);

            await _opc.WriteValuesCollection(new Dictionary<string, object>
        {
            { _configuration.Current.PingNodeID, ackValue },
            { _configuration.Current.pingtimer, timerValue }
        });

            AddLog("MES to Machine ", message);
        }
        catch (Exception ex)
        {
            _logger.Error("HandlePingAsync Error: " + ex.Message);
        }
    }
    private async Task HandleTcpMessageAsync(string message)
    {
        try
        {
            if (!_opcconnection)
            {
                _logger.Error($"OPC is not connected and message is {message}");
                return;
            }

            // -----------------------
            // Handle ACK (No XML)
            // -----------------------
            if (message.Contains("ACK") && !message.Contains("<"))
            {
                var ackValue = ConvertToDataType("ACK", _configuration.Current.Statusdatatype);

                bool write = await _opc.WriteValuesCollection(new Dictionary<string, object>
            {
                { _configuration.Current.Status, ackValue }
            });

                if (write)
                {
                    LogToTab3("MES to Machine", message);
                    _bl.InsertMessageValues("MES to Machine", message);
                }
                else
                {
                    LogToTab3("Status OPC write failed.", message);
                    _logger.Error("Status OPC write failed.  " + message);
                }
                _logger.Info($"message end: {message}  {DateTime.Now.ToString("yyyyMMdd HH:mm:ss ff")}");
                return;
            }

            // -----------------------
            // Handle XML Response
            // -----------------------
            int start = message.IndexOf("<");
            int end = message.LastIndexOf(">");

            if (start < 0 || end <= start)
                return;

            string xml = message.Substring(start, end - start + 1);

            XElement elem = XElement.Parse(xml);

            bool isError = elem.Name.LocalName.Equals("Error", StringComparison.OrdinalIgnoreCase);
            if (tags == null || tags.Count == 0)
                return;
            var relevantTags = tags
                .Where(t => t.CommandType == "Response" &&
                           (isError
                               ? t.CommandName.Equals("Error", StringComparison.OrdinalIgnoreCase)
                               : !t.CommandName.Equals("Error", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Dictionary<string, object> writeValues = new();

            foreach (var attr in elem.Attributes())
            {
                if (attr.Name.LocalName.Equals("tokens", StringComparison.OrdinalIgnoreCase))
                    continue;

                var tag = relevantTags.FirstOrDefault(t =>
                    t.ParamName.Equals(attr.Name.LocalName, StringComparison.OrdinalIgnoreCase));

                if (tag == null)
                {
                    _logger.Error($"Tag not found for parameter when write {attr.Name}  ; Command name: {attr.Parent.Name.ToString()} ");
                    continue;
                }

                object converted = ConvertToDataType(attr.Value, tag.DataType);
                writeValues[tag.NodeID] = converted;
            }

            if (writeValues.Count > 0)
            {
                bool result = await _opc.WriteValuesCollection(writeValues);

                if (!result)
                {
                    LogToTab3("MES to Machine", "Parameter write in PLC failed.");
                    _logger.Error($"Parameter OPC write failed for message {message}");
                    return;
                }
            }
            else
            {
                _logger.Info("No parameters to write for this message.");
            }

            string statusCode = isError ? "Error" : "ACK";

            var statusValue = ConvertToDataType(statusCode, _configuration.Current.Statusdatatype);

            bool statusResult = await _opc.WriteValuesCollection(new Dictionary<string, object>
        {
            { _configuration.Current.Status, statusValue }
        });

            if (statusResult)
            {
                LogToTab3("MES to Machine", message);
                _bl.InsertMessageValues("MES to Machine", message);
            }
            else
            {
                LogToTab3("Status OPC write failed.", message);
                _logger.Error("Status OPC write failed.  " + message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("HandleTcpMessageAsync Error: " + ex.Message);
        }

    }

    private void StartTcpProcessor()
    {
        if (_tcpProcessorTask != null && !_tcpProcessorTask.IsCompleted)
            return;

        _tcpProcessorCts = new CancellationTokenSource();

        _tcpProcessorTask = Task.Run(() =>
            ProcessTcpMessagesAsync(_tcpProcessorCts.Token));
    }
    private async Task ProcessTcpMessagesAsync(CancellationToken token)
    {
        try
        {
            await foreach (var message in _tcpMessageChannel.Reader.ReadAllAsync(token))
            {
                await HandleTcpMessageAsync(message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Info("TCP processor stopped.");
        }
        catch (Exception ex)
        {
            _logger.Error("ProcessTcpMessagesAsync Error: " + ex.Message);
        }
    }
    private object ConvertToDataType(string value, string dataType)
    {
        string cleanValue = value?.Trim() ?? "";

        switch (dataType?.ToUpper()) // Using Upper to match your list
        {
            case "BOOL":
                return cleanValue == "1" || cleanValue.Equals("true", StringComparison.OrdinalIgnoreCase);

            case "WORD": // 16-bit Unsigned Integer
                return ushort.TryParse(cleanValue, out ushort u16) ? u16 : (ushort)0;

            case "INT": // 16-bit Signed Integer
                return short.TryParse(cleanValue, out short s16) ? s16 : (short)0;

            case "DWORD": // 32-bit Unsigned Integer
                return uint.TryParse(cleanValue, out uint u32) ? u32 : (uint)0;

            case "DINT": // 32-bit Signed Integer
                return int.TryParse(cleanValue, out int i32) ? i32 : 0;

            case "REAL": // 32-bit Floating Point
                return float.TryParse(cleanValue, out float f32) ? f32 : 0f;

            case "LREAL": // 64-bit Floating Point
                return double.TryParse(cleanValue, out double d64) ? d64 : 0.0;

            case "STRING":
                return cleanValue;

            case "CHAR":
                return cleanValue.Length > 0 ? cleanValue[0] : '\0';

            case "DATE_AND_TIME":
                return DateTime.TryParse(cleanValue, out DateTime dt) ? dt : DateTime.MinValue;

            default:
                return cleanValue;
        }
    }
    #endregion

    #region Logic: OPC UA & Commands
    private void Opc_OnCommandReceived(object commandID)
    {
        _ = ProcessOpcCommandAsync(commandID);
    }
    private async Task ProcessOpcCommandAsync(object commandID)
    {
        await App.Current.Dispatcher.BeginInvoke(async () =>
          {
              try
              {
                  if (string.IsNullOrEmpty(commandID.ToString()) || string.IsNullOrWhiteSpace(Config.StationName))
                      return;
                  string dynamicMessage = "";
                  tags = new List<TagModel>();
                  // Assuming _db.FetchDataTable is your method to get data from SQL
                  DataTable dt = _bl.GetAllCommandWithParameter(Convert.ToInt32(commandID));
                  if (dt.Rows.Count == 0)
                  {
                      _logger.Error("Command not found");
                      return;
                  }

                  string TiSt = DateTime.Now.ToString("yyyyMMddHHmmss");
                  string count = MSGIDCommnad.ToString();
                  string EQP = Config.StationName.Trim();

                  var actionRow = dt.AsEnumerable().FirstOrDefault(r => !r["CommandName"].ToString().Equals("Error", StringComparison.OrdinalIgnoreCase));
                  if (actionRow == null)
                  {
                      _logger.Error("Command not found");
                      return;

                  }
                  string? commandName = actionRow != null ? actionRow["CommandName"].ToString() : "";
                  string? rootElement = actionRow != null ? actionRow["RootElement"].ToString() : "";
                  if (string.IsNullOrEmpty(EQP) || string.IsNullOrEmpty(commandName))
                  {
                      _logger.Error($"{EQP} or {commandName} is not found");
                      return;
                  }
                  foreach (DataRow row in dt.Rows)
                  {
                      tags.Add(new TagModel
                      {
                          Id = Convert.ToInt32(row["Id"]),
                          CommandName = row["CommandName"].ToString()??"",
                          CommandType = row["CommandType"].ToString()??"",
                          ParamName = row["ParamName"].ToString()??"",
                          DataType = row["DataType"].ToString()??"",
                          NodeID = row["NodeID"].ToString()??"",
                          IsActive = Convert.ToBoolean(row["IsActive"])
                      });
                  }
                  if (!string.IsNullOrEmpty(rootElement) && tags.Count > 0)
                  {
                      if(tags.Any(x => string.IsNullOrWhiteSpace(x.NodeID)))
                      {
                          _logger.Error($"One or more tags have null or empty NodeIDs. Command: {commandName}");
                          return;
                      }
                      List<string> requestNodeIds = tags.Where(x => x.CommandType == "Request" && x.CommandName != "Error").Select(x => x.NodeID).ToList();

                      if (requestNodeIds.Count > 0)
                      {
                          Dictionary<string, object> response = await _opc.ReadValuesCollection(requestNodeIds);

                          if (response == null)
                          {
                              _logger.Error(
                                  $"OPC Read failed (Server not connected or invalid node). " +
                                  $"Command: {commandName}, NodeIDs: {string.Join(", ", requestNodeIds)}");

                              return;
                          }

                          // NodeIDs not returned by OPC
                          var missingNodeIds = requestNodeIds
                              .Where(id => !response.ContainsKey(id))
                              .ToList();

                          // NodeIDs returned but value is null
                          var nullNodeIds = response
                              .Where(x => x.Value == null)
                              .Select(x => x.Key)
                              .ToList();

                          if (missingNodeIds.Any() || nullNodeIds.Any())
                          {
                              _logger.Error(
                                  $"OPC issue detected. Command: {commandName}, " +
                                  $"Missing NodeIDs: {string.Join(", ", missingNodeIds)}, " +
                                  $"Null NodeIDs: {string.Join(", ", nullNodeIds)}");

                              return;
                          }

                          // Count parameters first
                          int parameterCount = tags.Count(t => response.ContainsKey(t.NodeID));

                          // Create XML
                          XElement xml = new XElement(rootElement);

                          // 🔥 Add token FIRST
                          xml.Add(new XAttribute("tokens", parameterCount + 1));
                          // 4. Assign the results back to each specific Tag object
                          foreach (var tag in tags.Where(t => response.ContainsKey(t.NodeID)))
                          {
                              xml.SetAttributeValue(tag.ParamName, response[tag.NodeID]);
                          }

                          dynamicMessage = $"{commandName},{EQP},{count},{TiSt},{xml}";
                      }
                      else
                      {
                          dynamicMessage = $"{commandName},{EQP},{count},{TiSt}";
                      }
                  }
                  else
                  {
                      dynamicMessage = $"{commandName},{EQP},{count},{TiSt}";
                  }
                  LogToTab3("Machine to MES", dynamicMessage);
                  _bl.InsertMessageValues("Machine to MES", dynamicMessage);
                  MSGIDCommnad++;
                  MSGIDCommnad = MSGIDCommnad >= 1000 ? 1 : MSGIDCommnad;

                  await _tcp.SendAsync(dynamicMessage);


              }
              catch (Exception ex)
              {
                  _logger.Error("Opc_OnCommandReceived Method: " + ex.Message);
              }
          });
    }
    
    private async void ConnectionChange(bool obj)
    {
        await App.Current.Dispatcher.BeginInvoke(() =>
         {
             _opcconnection = obj;
             if (obj)
             {
                 PLCLedBrush = Brushes.Green;
                 AddLog("OPC UA", "Connected to OPC UA server.");

             }
             else
             {
                 PLCLedBrush = Brushes.Red;
                 AddLog("OPC UA", "DisConnected to OPC UA server. Trying to Reconnect");

             }
         });
    }
    #endregion

    #region Helper Methods (Logging & User)

    public void LogToTab3(string state, string message)
    {
        App.Current.Dispatcher.BeginInvoke(() =>
        {
            SequenceLogVM.AddLog(state, message);
        });
    }

    public void AddLog(string state, string message)
    {
        try
        {


            if (LogVM?.LogItems == null)
                return;

            App.Current.Dispatcher.BeginInvoke(() =>
            {
                LogVM.LogItems.Insert(0, new LogItem
                {
                    SrNo = ++srNoTimer,
                    DateTime = DateTime.Now,
                    State = state,
                    Message = message
                });

                while (LogVM.LogItems.Count > maxrecord)//Config.MaxRows
                {
                    LogVM.LogItems.RemoveAt(LogVM.LogItems.Count - 1);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error("AddLog Method: " + ex.Message);
        }
    }
    #endregion

    #region Connection Events (TCP)
    private void Tcp_OnDisconnected()
    {
        App.Current.Dispatcher.BeginInvoke(() =>
        {
            MesLedBrush = Brushes.Red;
            AddLog("DISCONNECTED", "Disconnected from server. Reconnecting...");
            _logger.Info("DISCONNECTED , Disconnected from server. Reconnecting...");

        });
    }

    private void Tcp_OnReconnected()
    {
        App.Current.Dispatcher.BeginInvoke(() =>
        {
            MesLedBrush = Brushes.Green;
            AddLog("RECONNECTED", "Reconnected to server.");
            _logger.Info("TCP Reconnected.");

        });
    }
    #endregion

    #region Configuration Actions
    private void SaveConfiguration()
    {

        try
        {

            if (_currentUser.Role != "Admin")
            {
                _messageBoxService.Show("Access Denied: Admin privileges required.", MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok);
                return;
            }
            if (string.IsNullOrEmpty(Config.StationName) || string.IsNullOrEmpty(Config.IPAddress) || Config.Port == 0 || Config.Timer == 0 || Config.MaxRows == 0)
            {
                _messageBoxService.Show("Please enter valid Data", MessageType.Warning, MessageButtons.Ok);
                return;
            }
            if(Config.Port > 65535)
            {
                _messageBoxService.Show("Port should be less than 65535", MessageType.Warning, MessageButtons.Ok);
                return;
            }
            if (Config.MaxRows > 100)
            {
                _messageBoxService.Show("MaxRows should be less than 100", MessageType.Warning, MessageButtons.Ok);
                return;
            }

            _bl.UpdtateCinfugurationValues(Config.StationName, Config.IPAddress, Config.Port, Config.MaxRows, Config.Timer);
            _messageBoxService.Show("Changes will be reflected after restart", MessageType.Success, MessageButtons.Ok);
            OnPropertyChanged(nameof(Config));
        }
        catch (Exception ex)
        {
            _logger.Error("Save_Button Method: " + ex.Message);
        }
    }

    private void ResetConfiguration()
    {
        try
        {
            Config.StationName = string.Empty;
            Config.IPAddress = string.Empty;
            Config.Port = 0;
            Config.MaxRows = 0;
            Config.Timer = 0;
            OnPropertyChanged(nameof(Config));
        }
        catch (Exception ex)
        {
            _logger.Error("Reset_Button Method: " + ex.Message);
        }
    }


    // Inside Form2ViewModel.cs

    private void Logout()
    {
        // 1. Stop the local UI timer
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
            _timer = null;
        }

        // 2. CRITICAL: Unsubscribe from Singleton events
        // This prevents "Double Execution" on next login
        _opc.OnCommandReceived -= Opc_OnCommandReceived;
        _opc.OnConnectionChange -= ConnectionChange;
        _tcp.OnMessageReceived -= Tcp_OnMessageReceived;
        _tcp.OnDisconnected -= Tcp_OnDisconnected;
        _tcp.OnReconnected -= Tcp_OnReconnected;

        // 2. Stop TCP processor properly
        if (_tcpProcessorCts != null)
        {
            _tcpProcessorCts.Cancel();
            _tcpMessageChannel.Writer.TryComplete(); // ← Add this line

            try
            {
                _tcpProcessorTask?.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException)
            {
                // Expected during cancellation
            }

            _tcpProcessorCts.Dispose();
            _tcpProcessorCts = null;
            _tcpProcessorTask = null;
        }

        // 3. Clear UI data
        LogVM.LogItems.Clear();
        SequenceLogVM.MesLogs.Clear();
        try
        {
            _tcpProcessorCts?.Cancel();
            _tcpProcessorTask = null;
        }
        catch (Exception ex)
        {
            _logger.Error("StopTcpProcessor Error: " + ex.Message);
        }
        if (_dbCleanupTimer != null)
        {
            _dbCleanupTimer.Stop();
            _dbCleanupTimer.Dispose();
            _dbCleanupTimer = null;
        }
        // 4. Navigate
        _navigation.OpenLogin();
    }
    public void SetUser(UserModel user)
    {
        _currentUser = user;
        bool adminStatus = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        CurUser = user.Username; // Set the current username
        // Set for current VM
        this.IsAdmin = adminStatus;


        // Pass to children
        UserAuthVM.IsAdmin = adminStatus;
        TagConfigVM.IsAdmin = adminStatus;
    }
    #endregion
    private void StartDatabaseCleanup()
    {
        _dbCleanupTimer = new System.Timers.Timer();

        _dbCleanupTimer.Interval = TimeSpan.FromHours(24).TotalMilliseconds;

        _dbCleanupTimer.Elapsed += (s, e) =>
        {
            try
            {
                int deleted = _bl.DeleteOldMessageLogs();
                _logger.Info($"Cleanup job deleted {deleted} records.");
            }
            catch (Exception ex)
            {
                _logger.Error("Cleanup job error: " + ex.Message);
            }
        };

        _dbCleanupTimer.AutoReset = true;
        _dbCleanupTimer.Start();
        // ⭐ Run once immediately
        try
        {
            int deleted = _bl.DeleteOldMessageLogs();
            _logger.Info($"Startup cleanup deleted {deleted} records.");
        }
        catch (Exception ex)
        {
            _logger.Error("Startup cleanup error: " + ex.Message);
        }
    }
}