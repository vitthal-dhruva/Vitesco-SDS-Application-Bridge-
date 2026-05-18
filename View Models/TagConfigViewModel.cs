
using Hiemdall_bridge.Helpers;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Data;
using System.Windows.Input;



namespace Hiemdall_bridge.ViewModels
{
    public class TagConfigViewModel : BaseViewModel
    {
        #region Fields
        private readonly IBusinessLayer _bl;
        private readonly IMessageBoxService _messageBox;
        private readonly ILogger _logger;

        private string _userRole;
        private bool isEditMode = false;
        private int editId = -1;
        #endregion

        #region Collections
        public Array PlcDataTypes { get; } = Enum.GetValues(typeof(PlcDataType));
        public ObservableCollection<TagConfigModel> TagConfigs { get; set; } = new ObservableCollection<TagConfigModel>();
        public ObservableCollection<string> CommandTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<CommandModel> Commands { get; set; } = new ObservableCollection<CommandModel>();
        #endregion

        #region Properties


        private PlcDataType _selectedPlcDataType;
        public PlcDataType SelectedPlcDataType
        {
            get => _selectedPlcDataType;
            set { _selectedPlcDataType = value; OnPropertyChanged(); }
        }

        private string _selectedCommandType;
        public string SelectedCommandType
        {
            get => _selectedCommandType;
            set
            {
                _selectedCommandType = value;
                OnPropertyChanged();
                LoadGrid();
            }
        }

        private CommandModel _selectedCommand;
        public CommandModel SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                _selectedCommand = value;
                OnPropertyChanged();
                LoadGrid();
            }
        }

        private string _parameterName;
        public string ParameterName
        {
            get => _parameterName;
            set { _parameterName = value; OnPropertyChanged(); }
        }

        private string _nodeID;
        public string NodeID
        {
            get => _nodeID;
            set { _nodeID = value; OnPropertyChanged(); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        private string _saveButtonText = "Save";
        public string SaveButtonText
        {
            get => _saveButtonText;
            set { _saveButtonText = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand SaveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ResetCommand { get; }
        #endregion

        #region Constructor
        public TagConfigViewModel(IBusinessLayer bl, IMessageBoxService messageBox, ILogger logger)
        {
            _bl = bl;
            _messageBox = messageBox;

            // Initialize Commands
            SaveCommand = new RelayCommand(Save);
            EditCommand = new RelayCommand(Edit);
            DeleteCommand = new RelayCommand(Delete);
            ResetCommand = new RelayCommand(_ => Clear());

            // Initialize Lists
            CommandTypes.Add("Request");
            CommandTypes.Add("Response");
            LoadCommand();
            _logger = logger;
        }
        #endregion

        #region Role Management
        /// <summary>
        /// Sets the user role and updates the IsAdmin flag for UI bindings
        /// </summary>
        public void SetRole(string role)
        {
            _userRole = role;
            // Update the boolean property so XAML Buttons (IsEnabled) react immediately
            IsAdmin = (role == "Admin");
        }
        #endregion

        #region Data Loading Methods
        /// <summary>
        /// Fetches all available commands from the business layer
        /// </summary>
        private void LoadCommand()
        {
            try
            {


                Commands.Clear();
                var ds = _bl.GetAllCommand();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Commands.Add(new CommandModel
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        CommandName = row["CommandName"].ToString(),
                        CommandType = row["RootElement"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("LoadCommand Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Loads the DataGrid based on the selected Command and Type
        /// </summary>
        private void LoadGrid()
        {
            try
            {


                if (string.IsNullOrEmpty(SelectedCommandType) || SelectedCommand == null)
                {
                    TagConfigs.Clear();
                    return;
                }

                TagConfigs.Clear();
                var ds = _bl.GetAllCommandParameter(SelectedCommand.Id.ToString(), SelectedCommandType);
                int Srno = 1;
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    TagConfigs.Add(new TagConfigModel
                    {
                        SrNo = Srno,
                        Id = Convert.ToInt32(row["Id"]),
                        CommandId = row["CommandId"].ToString(),
                        CommandType = row["CommandType"].ToString(),
                        CommandName = row["CommandName"].ToString(),
                        ParameterName = row["ParamName"].ToString(),
                        DataType = row["DataType"].ToString(),
                        NodeID = row["NodeID"].ToString(),
                        IsActive = Convert.ToBoolean(Convert.ToInt32(row["IsActive"]))
                    });
                    Srno++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("LoadGrid Error: " + ex.Message);
            }
        }
        #endregion

        #region Command Actions (Save, Edit, Delete, Clear)
        /// <summary>
        /// Logic for Edit button within the DataGrid
        /// </summary>
        private void Edit(object obj)
        {
            try
            {


                if (obj is TagConfigModel model)
                {
                    isEditMode = true;
                    editId = model.Id;

                    ParameterName = model.ParameterName;
                    NodeID = model.NodeID;
                    IsActive = model.IsActive;
                    SelectedCommandType = model.CommandType;

                    // Match the selected command from the list
                    SelectedCommandType = model.CommandType;
                    if (Enum.TryParse(model.DataType, out PlcDataType dtype))
                        SelectedPlcDataType = dtype;

                    SaveButtonText = "Update";
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Edit Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Logic for Save/Update button. Restricts access to Admin only.
        /// </summary>
        private void Save(object obj)
        {
            try
            {
                if (!IsAdmin)
                {
                    _messageBox.Show("Permission denied", MessageBoxCustom.MessageType.Warning);
                    return;
                }

                if (SelectedCommand == null || string.IsNullOrWhiteSpace(ParameterName) || string.IsNullOrWhiteSpace(NodeID)
                    || string.IsNullOrWhiteSpace(SelectedCommandType))
                {
                    _messageBox.Show("Please Enter all the details", MessageBoxCustom.MessageType.Warning);
                    return;
                }
                bool correct=IsValidNodeId(NodeID);
                if (!correct)
                {
                    _messageBox.Show("Node Address is not valid", MessageBoxCustom.MessageType.Warning);
                    return;
                }
                if (isEditMode)
                {
                    _bl.UpdateParameter(editId, SelectedCommand.Id.ToString(), SelectedCommandType, ParameterName, NodeID, SelectedPlcDataType.ToString(), IsActive);
                    _messageBox.Show("Updated Successfully", MessageBoxCustom.MessageType.Success);
                }
                else
                {
                    _bl.InsertCommandValues(SelectedCommand.Id.ToString(), SelectedCommandType, ParameterName, SelectedPlcDataType.ToString(), NodeID, IsActive);
                    _messageBox.Show("Inserted Successfully", MessageBoxCustom.MessageType.Success);
                }

                LoadGrid();
                Clear();
            }
            catch (Exception ex)
            {
                _logger.Error("Save Error: " + ex.Message);

            }
        }
        private bool IsValidNodeId(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return false;

            // Must start with ns= (namespace index)
            if (!nodeId.StartsWith("ns="))
                return false;

            // Must contain ;s= or ;i=
            if (!(nodeId.Contains(";s=") || nodeId.Contains(";i=")))
                return false;

            return true;
        }
        /// <summary>
        /// Logic for Delete button within the DataGrid. Restricts access to Admin only.
        /// </summary>
        private void Delete(object obj)
        {
            try
            {
                if (obj is TagConfigModel model)
                {
                    if (_messageBox.Show("Delete?", MessageBoxCustom.MessageType.Confirmation, MessageBoxCustom.MessageButtons.YesNo) == true)
                    {
                        _bl.DeleteParameter(model.Id);
                        LoadGrid();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Save Error: " + ex.Message);

            }
        }

        /// <summary>
        /// Resets the input fields to default state
        /// </summary>
        private void Clear()
        {
            ParameterName = string.Empty;
            NodeID = string.Empty;
            IsActive = false;
            isEditMode = false;
            editId = -1;
            SaveButtonText = "Save";
        }
        #endregion
    }

    #region Supporting Models
    public class CommandModel
    {
        public int Id { get; set; }
        public string CommandName { get; set; }
        public string CommandType { get; set; }
    }
    #endregion
}

