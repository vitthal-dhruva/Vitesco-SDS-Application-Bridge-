
using Hiemdall_bridge.Models;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hiemdall_bridge
{
    /// <summary>
    /// Interaction logic for Form2.xaml
    /// </summary>
    public partial class Form2 : Window//, INotifyPropertyChanged
    {
       
        private readonly Regex DigitsOnlyRegex = new(@"^\d+$", RegexOptions.Compiled);

        public Form2(Form2ViewModel vm)
        {
            InitializeComponent();
             DataContext = vm;
            // Link the VM's request to the Window's Close method
            vm.RequestClose = () => this.Close();

        }
        
        public async void form2_loaded(object sender, RoutedEventArgs e)
        {

         

        }
     
        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // allow only digits
            e.Handled = !DigitsOnlyRegex.IsMatch(e.Text);
        }

        private void Integer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // block space and decimal keys
            if (e.Key == Key.Space || e.Key == Key.OemPeriod || e.Key == Key.Decimal)
                e.Handled = true;
        }

        private void Integer_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = (e.DataObject.GetData(DataFormats.Text) as string) ?? string.Empty;
            if (!DigitsOnlyRegex.IsMatch(text.Trim()))
                e.CancelCommand();
        }

        private void Integer_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var text = tb.Text ?? string.Empty;
            if (string.IsNullOrEmpty(text)) return;

            // Remove non-digits (defensive) and trim leading zeros if desired
            var digits = new string(text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
            {
                tb.Text = string.Empty;
                return;
            }

            // Optional: clamp to int range safely
            if (long.TryParse(digits, out var longVal))
            {
                if (longVal > int.MaxValue) digits = int.MaxValue.ToString();
            }

            tb.Text = digits;
        }
        private void TabUser_Selected(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as Form2ViewModel;
            vm?.UserAuthVM.LoadUsersCommand.Execute(null);
        }

        private void Save_Button_TAB4(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as Form2ViewModel;
            if (vm == null) return;

            string role = (cbRoleTypeTab4.SelectedItem as ComboBoxItem)?.Content.ToString();
            
            // Pass UI data to the VM logic
            vm.UserAuthVM.ExecuteSaveOrUpdate(txtUsernameTab4.Text, txtPasswordTab4.Text, role, vm.Role);

            // Reset UI
            Reset_Button_Tab4(null, null);
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as Button)?.Tag as UserAuthModel;
            var vm = DataContext as Form2ViewModel;

            if (user != null && vm != null)
            {
                vm.UserAuthVM.StartEditMode(user);
                txtUsernameTab4.Text = user.Username;
                txtPasswordTab4.Text = ""; // Clear password for security

                // Select Role in ComboBox
                foreach (ComboBoxItem item in cbRoleTypeTab4.Items)
                {
                    if (item.Content.ToString() == user.RoleType)
                    {
                        cbRoleTypeTab4.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void Reset_Button_Tab4(object sender, RoutedEventArgs e)
        {
            txtUsernameTab4.Clear();
            txtPasswordTab4.Clear();
            cbRoleTypeTab4.SelectedIndex = 0;
            (DataContext as Form2ViewModel)?.UserAuthVM.ResetMode();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Form2ViewModel vm)
            {
                // Calling the ViewModel method directly or via Command
                vm.logoutCommand.Execute(null);
            }
        }
    }
}


