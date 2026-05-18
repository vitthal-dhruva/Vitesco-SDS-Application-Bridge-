using Hiemdall_bridge.Interface;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Hiemdall_bridge.Interface.ILogger;
using MonitoredItem = Opc.Ua.Client.MonitoredItem;

namespace Hiemdall_bridge
{

    class OPCUA : IOpcClient
    {
        public event Action<string>? OnCommandReceived;
        public event Action<bool>? OnConnectionChange;
        private string? _lastCommand = null;
        public List<ParameterSpec> paraSpecs = new List<ParameterSpec>();
        private Session? session;        
        private string? endpointUrl;
        private string? eventNode;
        private Subscription? _subscription;
        private readonly ILogger _logger;
        private ApplicationConfiguration? _appConfig;
        private bool InstantConnection;
        private bool _isConnecting = false;       
        private Timer timer;
       
        public OPCUA(ILogger Ilogger)
        {
            _logger = Ilogger;
        }


        public async Task ConnectAsync(string endpointUrl, string eventNode)
        {
            this.endpointUrl = endpointUrl;
            this.eventNode = eventNode;
            _ = Task.Run(async () =>
            {
                await ConnectInternalAsync();
            });
            timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(20));

        }
        private void TimerCallback(object state)
        {
            if (_isConnecting)
            {
                _logger.Info("Timer already in progress, skipping");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    // ✅ Safe null + connection check
                    if (session == null || !session.Connected)
                    {
                        await HandleReconnect("Session null or disconnected");
                        return;
                    }

                    // ✅ Health check read
                    session.ReadValue("ns=4;i=187");
                }
                catch (NullReferenceException ex)
                {
                    await HandleReconnect("Session null");
                    _logger.Error("NullReference: " + ex.Message);
                }
                catch (Opc.Ua.ServiceResultException ex)
                {
                    // ✅ Proper OPC error handling
                    if (ex.StatusCode == StatusCodes.BadSecureChannelClosed ||
                        ex.StatusCode == StatusCodes.BadNotConnected)
                    {
                        await HandleReconnect($"OPC Error: {ex.StatusCode}");
                    }
                    else
                    {
                        _logger.Error("OPC Error: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("TimerCallback General Error: " + ex.Message);
                }
            });
        }
        private async Task HandleReconnect(string reason)
        {
            if (_isConnecting)
                return;

            _logger.Info($"Reconnecting OPC... Reason: {reason}");

            await ConnectInternalAsync();

            if (session == null || !session.Connected)
            {
                OnConnectionChange?.Invoke(false);
            }
           
        }
        private async Task ConnectInternalAsync()
        {

            // Prevent multiple simultaneous connection attempts
            if (_isConnecting)
            {
                _logger.Info("Connection already in progress, skipping");
                return;
            }

            try
            {
                _isConnecting = true;
                if(InstantConnection)
                {
                    OnConnectionChange?.Invoke(false);
                }

                // Clean up old connection
                await DisconnectOPCSessionAsync();

                if (_appConfig == null)
                {
                    var basePath = @"C:\OPC\Certificates";

                    _appConfig = new ApplicationConfiguration
                    {
                        ApplicationName = $"Hiemdall_Station{GetIconNumber()}",
                        ApplicationType = ApplicationType.Client,

                        SecurityConfiguration = new SecurityConfiguration
                        {
                            ApplicationCertificate = new CertificateIdentifier
                            {
                                StoreType = "Directory",
                                StorePath = Path.Combine(basePath, "Own"),
                                SubjectName = "CN=MyClient"
                            },

                            TrustedPeerCertificates = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = Path.Combine(basePath, "TrustedPeers")
                            },

                            TrustedIssuerCertificates = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = Path.Combine(basePath, "TrustedIssuers")
                            },

                            RejectedCertificateStore = new CertificateTrustList
                            {
                                StoreType = "Directory",
                                StorePath = Path.Combine(basePath, "Rejected")
                            },

                            AutoAcceptUntrustedCertificates = true
                        },

                        TransportQuotas = new TransportQuotas
                        {
                            OperationTimeout = 60000
                        },

                        ClientConfiguration = new ClientConfiguration
                        {
                            DefaultSessionTimeout = 60000
                        }
                    };

                    // 🔥 VERY IMPORTANT
                    await _appConfig.Validate(ApplicationType.Client);
                }                // Create session without certificate
                session = await Session.Create(
                  _appConfig,
                  new ConfiguredEndpoint(null, new EndpointDescription(endpointUrl)),
                  true,
                  $"Hiemdall{GetIconNumber()}",
                  60000,
                  new UserIdentity(new AnonymousIdentityToken()),
                  null);

                if (session != null && session.Connected)
                {
                    CreateSubscription();
                    _logger.Info($"Connected to OPC server ");
                    _logger.Info($"Subscription count: {session.SubscriptionCount.ToString()} ");
                    OnConnectionChange?.Invoke(true);
                    InstantConnection=false;
                    
                    
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Connection failed: {ex.Message}");

            }
            finally
            {
                _isConnecting = false;
            }
        }
        private string GetIconNumber()
        {
            try
            {
                string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];
                if (int.TryParse(iconNumberStr, out int number))
                    return number.ToString();
            }
            catch { }
            return "1";
        }

        private void CreateSubscription()
        {
            try
            {
                if (session == null || !session.Connected || string.IsNullOrEmpty(eventNode))
                    return;
                if (_subscription != null)
                    return;

                if (session.DefaultSubscription == null)
                    return;

                _subscription = new Subscription(session.DefaultSubscription)
                {
                    PublishingInterval = 300,
                    PublishingEnabled = true,
                    DisplayName = $"HiemdallSubscription{GetIconNumber()}"
                };

                session.AddSubscription(_subscription);
                _subscription.Create();

                var item = new MonitoredItem(_subscription.DefaultItem)
                {
                    DisplayName = $"PLC_Command{GetIconNumber()}",
                    StartNodeId = NodeId.Parse(eventNode),
                    AttributeId = Attributes.Value,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = 200,   // 🔥 faster                              
                };
                // Add filter to only notify on actual value changes
                item.Filter = new DataChangeFilter
                {
                    Trigger = DataChangeTrigger.StatusValue,
                    DeadbandType = 0,
                    DeadbandValue = 0
                };
                item.Notification += CommandItem_Notification;
                _subscription.AddItem(item);
                _subscription.ApplyChanges();
               
                _logger.Info("Subscription created");
            }
            catch (Exception ex)
            {
                _logger.Error($"CreateSubscription Error: {ex.Message}");
            }
        }

        private void CleanupSubscription()
        {
            try
            {
                if (_subscription != null)
                {
                    foreach (var item in _subscription.MonitoredItems)
                        item.Notification -= CommandItem_Notification;
                    _subscription.RemoveItems(_subscription.MonitoredItems);
                    _subscription.Delete(true);
                    _subscription.Dispose();
                    _subscription = null;
                    _logger.Info("Subscription Cleaned up");
                }
            }
            catch (Exception ex)
            {
                _subscription = null;
                _logger.Error("CleanupSubscription Error : " + ex.Message);
            }
        }

        private void CommandItem_Notification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                foreach (var dataValue in item.DequeueValues())
                {
                    if (dataValue?.Value == null) continue;

                    var raw = dataValue.Value.ToString();
                    if (_lastCommand == raw) continue;

                    _lastCommand = raw;
                    OnCommandReceived?.Invoke(raw!);
                   
                }
               
            }
            catch (Exception ex)
            {
                _logger.Error($"CommandItem_Notification Error: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, object>> ReadValuesCollection(List<string> nodeIds)
        {
            if (_isConnecting || session == null || InstantConnection)
                return null;

            try
            {
                if (nodeIds == null || nodeIds.Count == 0)
                    return null;

                ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                foreach (string nodeId in nodeIds)
                {
                    nodesToRead.Add(new ReadValueId
                    {
                        NodeId = new NodeId(nodeId),
                        AttributeId = Attributes.Value
                    });
                }

                using var cts = new CancellationTokenSource(5000);
                var response = await session.ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Both,
                    nodesToRead,
                    cts.Token);

                if (response?.Results == null)
                    return null;

                var result = new Dictionary<string, object>();
                for (int i = 0; i < response.Results.Count && i < nodeIds.Count; i++)
                {
                    result[nodeIds[i]] = response.Results[i].Value;
                }
                return result;
            }
            //catch (Opc.Ua.ServiceResultException ex)
            //{
            //    // ✅ Proper OPC error handling
            //    if (ex.StatusCode == StatusCodes.BadSecureChannelClosed ||
            //        ex.StatusCode == StatusCodes.BadNotConnected)
            //    {
            //        await HandleReconnect($"OPC Error: {ex.StatusCode}");
            //    }
            //    return null;
            //}
            catch (Exception ex)
            {
                _logger.Error($"ReadValuesCollection Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> WriteValuesCollection(Dictionary<string, object> values)
        {
            if (_isConnecting || session == null || InstantConnection)
                return false;

            try
            {
                if (values == null || values.Count == 0)
                    return false;

                using var cts = new CancellationTokenSource(5000);
                WriteValueCollection writeCollection = new WriteValueCollection();

                foreach (var item in values)
                {
                    WriteValue writeValue = new WriteValue
                    {
                        NodeId = NodeId.Parse(item.Key),
                        AttributeId = Attributes.Value,
                        Value = new DataValue { Value = item.Value }
                    };
                    writeCollection.Add(writeValue);
                }

                WriteResponse writeResponse = await session.WriteAsync(
                    null,
                    writeCollection,
                    cts.Token);

                if (writeResponse?.Results == null)
                    return false;

                bool allGood = true;
                for (int i = 0; i < writeResponse.Results.Count; i++)
                {
                    if (StatusCode.IsBad(writeResponse.Results[i]))
                    {
                        allGood = false;
                        string failedNode = values.Keys.ElementAt(i);
                        _logger.Error($"Write failed for Node: {failedNode}, Status: {writeResponse.Results[i]}");
                    }
                }
                return allGood;
            }
            catch (Opc.Ua.ServiceResultException ex)
            {
                // ✅ Proper OPC error handling
                if (ex.StatusCode == StatusCodes.BadSecureChannelClosed ||
                    ex.StatusCode == StatusCodes.BadNotConnected ||
                    ex.StatusCode == StatusCodes.BadRequestInterrupted || !InstantConnection)
                {
                    InstantConnection = true;
                    await HandleReconnect($"OPC Error: {ex.StatusCode}");
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"WriteValuesCollection Error: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectOPCSessionAsync()
        {
            try
            {
                CleanupSubscription();

                if (session != null)
                {
                    await Task.Run(() => session.Close());
                    session.Dispose();
                    session = null;
                }
                _logger.Info(" DisconnectOPCSessionAsync:  Successfully disconnected");
            }
            catch (Exception ex)
            {
                session = null;
                _logger.Error($"Disconnect Error: {ex.Message}");
            }
        }

    }


    public enum PlcDataType
    {
        Bool,
        Word,
        DWord,
        Int,
        DInt,
        Real,
        Lreal,
        String,
        Char,
        Date_and_time
    }

    public class ParameterSpec
    {
        public string CommandName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
        public PlcDataType Data_Type { get; set; }
    }
}
