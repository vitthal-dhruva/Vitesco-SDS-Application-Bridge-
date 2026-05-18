#region OLD
//using System;
//using System.Collections.ObjectModel;
//using System.Data;
//using System.Windows.Input;
//using System.Windows;
//using Hiemdall_bridge.Models;
//using Hiemdall_bridge.Interface;
//using Hiemdall_bridge.Helpers; // Ensure your RelayCommand class is here

//namespace Hiemdall_bridge.ViewModels
//{
//    public class UserAuthViewModel : BaseViewModel
//    {
//        private readonly IBusinessLayer _bl;
//        IMessageBoxService _messageBoxService;
//        private readonly ILogger _logger;
//        private int _editingUserId = 0;
//        private string _saveButtonContent = "Save";

//        public ObservableCollection<UserAuthModel> Users { get; set; } = new ObservableCollection<UserAuthModel>();

//        public string SaveButtonContent
//        {
//            get => _saveButtonContent;
//            set { _saveButtonContent = value; OnPropertyChanged(); }
//        }

//        // --- Commands ---
//        public ICommand LoadUsersCommand { get; }
//        public ICommand DeleteUserCommand { get; }

//        public UserAuthViewModel(IBusinessLayer bl, ILogger logger, IMessageBoxService messageBoxService)
//        {
//            _bl = bl;
//            _logger = logger;


//            LoadUsersCommand = new RelayCommand(_ => LoadUsersGrid());
//            DeleteUserCommand = new RelayCommand(param =>
//            {
//                if (param is UserAuthModel user) ExecuteDelete(user);
//            });
//            _messageBoxService = messageBoxService;
//        }

//        public void LoadUsersGrid()
//        {
//            try
//            {
//                DataSet ds = _bl.GetAllUsers();
//                App.Current.Dispatcher.BeginInvoke(() =>
//                {
//                    Users.Clear();
//                    if (ds != null && ds.Tables.Count > 0)
//                    {
//                        foreach (DataRow row in ds.Tables[0].Rows)
//                        {
//                            Users.Add(new UserAuthModel
//                            {
//                                Id = row["ID"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : 0,
//                                Username = row["UserName"]?.ToString() ?? string.Empty,
//                                RoleType = row["UserType"]?.ToString() ?? string.Empty
//                            });
//                        }
//                    }
//                });
//            }
//            catch (Exception ex) { _logger.Error("LoadUsersGrid Error: " + ex.Message); }
//        }

//        public void ExecuteSaveOrUpdate(string username, string password, string role, string currentUserRole)
//        {
//            try
//            {
//                if (!string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase))
//                {
//                    _messageBoxService.Show("Access Denied: Admin privileges required.",MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok);
//                    return;
//                }
//                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
//                {
//                    _messageBoxService.Show("Provide valid details.", MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok);
//                    return;
//                }
//                if (_editingUserId != 0)
//                {
//                    _bl.UpdateUser(_editingUserId, username, password, role);
//                    _editingUserId = 0;
//                    SaveButtonContent = "Save";
//                }
//                else
//                {
//                    _bl.InsertUSerValues(username, password, role);
//                }
//                LoadUsersGrid();
//            }
//            catch (Exception ex) { _logger.Error("Save/Update Error: " + ex.Message); }
//        }

//        private void ExecuteDelete(UserAuthModel user)
//        {
//            if (_messageBoxService.Show($"Delete {user.Username}?", MessageBoxCustom.MessageType.Confirmation, MessageBoxCustom.MessageButtons.Ok) == true)
//        {
//                _bl.DeleteUser(user.Id);
//                LoadUsersGrid();
//            }
//        }

//        public void StartEditMode(UserAuthModel user)
//        {
//            _editingUserId = user.Id;
//            SaveButtonContent = "Update";
//        }

//        public void ResetMode()
//        {
//            _editingUserId = 0;
//            SaveButtonContent = "Save";
//        }
//    }
//} 
#endregion

using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;
using System.Windows;
using Hiemdall_bridge.Models;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Helpers;
using System.Linq;

namespace Hiemdall_bridge.ViewModels
{
    public class UserAuthViewModel : BaseViewModel
    {
        #region Fields
        private readonly IBusinessLayer _bl;
        private readonly IMessageBoxService _messageBoxService;
        private readonly ILogger _logger;
        private int _editingUserId = 0;
        private string _saveButtonContent = "Save";
        private string _userRole;
        #endregion

        #region Collections
        public ObservableCollection<UserAuthModel> Users { get; set; } = new ObservableCollection<UserAuthModel>();
        #endregion

        #region Properties
        private bool _isAdmin;
        /// <summary>
        /// Property used for UI DataBinding to enable/disable buttons based on role
        /// </summary>
        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(); }
        }

        public string SaveButtonContent
        {
            get => _saveButtonContent;
            set { _saveButtonContent = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        public ICommand LoadUsersCommand { get; }
        public ICommand DeleteUserCommand { get; }
        #endregion

        #region Constructor
        public UserAuthViewModel(IBusinessLayer bl, ILogger logger, IMessageBoxService messageBoxService)
        {
            _bl = bl;
            _logger = logger;
            _messageBoxService = messageBoxService;

            // Initialize Commands
            LoadUsersCommand = new RelayCommand(_ => LoadUsersGrid());
            DeleteUserCommand = new RelayCommand(param =>
            {
                if (param is UserAuthModel user) ExecuteDelete(user);
            });
        }
        #endregion

        #region Role Management
        /// <summary>
        /// Updates the current role and sets the IsAdmin flag for the UI
        /// </summary>
        public void SetRole(string role)
        {
            _userRole = role;
            IsAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region Data Loading Methods
        /// <summary>
        /// Fetches user data from the database and updates the observable collection
        /// </summary>
        public void LoadUsersGrid()
        {
            try
            {
                DataSet ds = _bl.GetAllUsers();

                // Using Dispatcher because database calls often happen on background threads
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    Users.Clear();
                    if (ds != null && ds.Tables.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            Users.Add(new UserAuthModel
                            {
                                Id = row["ID"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : 0,
                                Username = row["UserName"]?.ToString() ?? string.Empty,
                                RoleType = row["UserType"]?.ToString() ?? string.Empty
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error("LoadUsersGrid Error: " + ex.Message);
            }
        }
        #endregion

        #region Command Actions (Save, Update, Delete)
        /// <summary>
        /// Handles both New User creation and Existing User updates.
        /// Verifies Admin privileges before execution.
        /// </summary>
        public void ExecuteSaveOrUpdate(string username, string password, string role, string currentUserRole)
        {
            try
            {
                // Access Control Check
                if (!string.Equals(currentUserRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    _messageBoxService.Show("Access Denied: Admin privileges required.", MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok);
                    return;
                }
                
                // Validation Check
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role) | role=="----Select----")
                {
                    _messageBoxService.Show("Provide valid details.", MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok);
                    return;
                }

                if (_editingUserId != 0)
                {
                    // Update existing record
                    _bl.UpdateUser(_editingUserId, username, password, role);
                    _editingUserId = 0;
                    SaveButtonContent = "Save";
                }
                else
                {
                    DataSet ds = _bl.GetAllUsers();
                    bool exists = ds.Tables[0].AsEnumerable().Any(row => row["UserName"].ToString() == username.ToString());

                    if (exists)
                    {
                        _messageBoxService.Show("User already exists.", MessageBoxCustom.MessageType.Error, MessageBoxCustom.MessageButtons.Ok); // user already exists
                    return;
                    }// Insert new record
                    _bl.InsertUSerValues(username, password, role);
                }

                LoadUsersGrid();
            }
            catch (Exception ex)
            {
                _logger.Error("Save/Update Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Removes a user from the system after confirmation
        /// </summary>
        private void ExecuteDelete(UserAuthModel user)
        {
            // Only admins should be able to trigger this via UI binding, but we check role for safety
            if (!IsAdmin) return;

            if (_messageBoxService.Show($"Delete {user.Username}?", MessageBoxCustom.MessageType.Confirmation, MessageBoxCustom.MessageButtons.YesNo) == true)
            {
               var a= _bl.DeleteUser(user.Id);
                if (a == 0)
                    _messageBoxService.Show($"You cannot delete the only admin user in the system.", MessageBoxCustom.MessageType.Warning, MessageBoxCustom.MessageButtons.Ok);
                LoadUsersGrid();
            }
        }
        #endregion

        #region Mode Switching Methods
        /// <summary>
        /// Prepares the ViewModel to update an existing user
        /// </summary>
        public void StartEditMode(UserAuthModel user)
        {
            _editingUserId = user.Id;
            SaveButtonContent = "Update";
        }

        /// <summary>
        /// Returns the ViewModel to 'New Entry' mode
        /// </summary>
        public void ResetMode()
        {
            _editingUserId = 0;
            SaveButtonContent = "Save";
        }
        #endregion
    }
}