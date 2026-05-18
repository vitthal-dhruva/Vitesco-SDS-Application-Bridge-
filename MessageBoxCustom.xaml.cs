using Hiemdall_bridge.Interface;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Hiemdall_bridge
{
    /// <summary>
    /// Interaction logic for MessageBoxCustom.xaml
    /// </summary>
    public partial class MessageBoxCustom : Window
    {

        
        public MessageBoxCustom()
        {
            InitializeComponent();
        }
        //public MessageBoxCustom(string message, MessageType messageType, MessageButtons messageButtons)
        //{
        //    InitializeComponent();
        //    txtMessage.Text = message;
        //    switch (messageType)
        //    {
        //        case MessageType.Info:
        //            txtTitle.Text = "Info";
        //            MyPackIcon.Kind = PackIconKind.Information;
        //            break;
        //        case MessageType.Confirmation:
        //            txtTitle.Text = "Confirmation";
        //            MyPackIcon.Kind = PackIconKind.QuestionMarkRhombusOutline;
        //            break;
        //        case MessageType.Success:
        //            txtTitle.Text = "Success";
        //            MyPackIcon.Kind = PackIconKind.CheckUnderlineCircleOutline;
        //            MyPackIcon.Foreground = Brushes.DarkGreen;
        //            break;
        //        case MessageType.Warning:
        //            txtTitle.Text = "Warning";
        //            MyPackIcon.Kind = PackIconKind.Warning;

        //            break;
        //        case MessageType.Error:
        //            txtTitle.Text = "Error";
        //            MyPackIcon.Kind = PackIconKind.Error;
        //            break;
        //    }
        //    // Ensure default visibility, then enable only the requested buttons
        //    btnOk.Visibility = Visibility.Collapsed;
        //    btnCancel.Visibility = Visibility.Collapsed;
        //    btnYes.Visibility = Visibility.Collapsed;
        //    btnNo.Visibility = Visibility.Collapsed;

        //    switch (messageButtons)
        //    {
        //        case MessageButtons.OkCancel:
        //            btnOk.Visibility = Visibility.Visible;
        //            btnCancel.Visibility = Visibility.Visible;
        //            break;

        //        case MessageButtons.YesNo:
        //            btnYes.Visibility = Visibility.Visible;
        //            btnNo.Visibility = Visibility.Visible;
        //            break;

        //        case MessageButtons.Ok:
        //            btnOk.Visibility = Visibility.Visible;
        //            break;

        //        default:
        //            // fallback show OK
        //            btnOk.Visibility = Visibility.Visible;
        //            break;
        //    }

        //    // optional: focus primary button
        //    if (btnOk?.Visibility == Visibility.Visible)
        //        btnOk.Focus();
        //    else if (btnYes?.Visibility == Visibility.Visible)
        //        btnYes.Focus();
        //}
        public enum MessageType
        {
            Info,
            Confirmation,
            Success,
            Warning,
            Error,
        }
        public enum MessageButtons
        {
            OkCancel,
            YesNo,
            Ok,
        }
        public enum MessageBoxImage
        {
            None,
            Error,
            Hand,
            Stop,
            Question,
            Exclamation,
            Warning,
            Asterisk,
            Information
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();

        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //bool? result = new MessageBoxCustom("Are You sure,You want Close application ?", MessageBoxCustom.MessageType.Confirmation, MessageBoxCustom.MessageButtons.YesNo).ShowDialog();
            //if (result.Value)
            //{
            //    this.Close();
            //}
            // this.Close();
        }

        private void btnHeaderClose_Click(object sender, RoutedEventArgs e)
        {
            // Close behaves like Cancel / dismiss
            this.DialogResult = false;
            this.Close();
        } 

        

        // IMessageBoxService implementation
        public bool? Show(string message, MessageType messageType, MessageButtons messageButtons = MessageButtons.Ok)
        {
            // Set message and type
            txtMessage.Text = message;
            switch (messageType)
            {
                case MessageType.Info:
                    txtTitle.Text = "Info";
                    MyPackIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Information;
                    break;
                case MessageType.Warning:
                    txtTitle.Text = "Warning";
                    MyPackIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Warning;
                    break;
                case MessageType.Error:
                    txtTitle.Text = "Error";
                    MyPackIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Error;
                    break;
                    // add other types...
            }

            // Show/hide buttons based on MessageButtons
            btnOk.Visibility = btnCancel.Visibility = btnYes.Visibility = btnNo.Visibility = Visibility.Collapsed;

            switch (messageButtons)
            {
                case MessageButtons.OkCancel:
                    btnOk.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                    break;
                case MessageButtons.YesNo:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    break;
                case MessageButtons.Ok:
                    btnOk.Visibility = Visibility.Visible;
                    break;
            }

            return this.ShowDialog();
        }
        public void SetMessage(string message,
                       MessageType messageType,
                       MessageButtons messageButtons)
        {
            txtMessage.Text = message;

            switch (messageType)
            {
                //    case MessageType.Info:
                //        txtTitle.Text = "Info";
                //        MyPackIcon.Kind = PackIconKind.Information;
                //        break;

                //    case MessageType.Warning:
                //        txtTitle.Text = "Warning";
                //        MyPackIcon.Kind = PackIconKind.Warning;
                //        break;

                //    case MessageType.Error:
                //        txtTitle.Text = "Error";
                //        MyPackIcon.Kind = PackIconKind.Error;
                //        break;
                //}
                case MessageType.Success:
                txtTitle.Text = "Success";
                MyPackIcon.Kind = PackIconKind.CheckCircleOutline; // Right mark
                MyPackIcon.Foreground = Brushes.Green;
                break;

            case MessageType.Error:
                txtTitle.Text = "Error";
                MyPackIcon.Kind = PackIconKind.CloseCircleOutline; // Cross sign for error
                MyPackIcon.Foreground = Brushes.Red;
                break;

            case MessageType.Warning:
                txtTitle.Text = "Warning";
                MyPackIcon.Kind = PackIconKind.AlertOutline; // Danger / Exclamation
                MyPackIcon.Foreground = Brushes.Orange;
                break;

            case MessageType.Confirmation:
                txtTitle.Text = "Confirmation";
                MyPackIcon.Kind = PackIconKind.HelpCircleOutline; // Question mark
                MyPackIcon.Foreground = (Brush)new BrushConverter().ConvertFromString("#2196F3"); // Blue
                break;

            case MessageType.Info:
                txtTitle.Text = "Info";
                MyPackIcon.Kind = PackIconKind.InformationOutline;
                MyPackIcon.Foreground = Brushes.DeepSkyBlue;
                break;
            }
            btnOk.Visibility = btnCancel.Visibility =
            btnYes.Visibility = btnNo.Visibility = Visibility.Collapsed;

            switch (messageButtons)
            {
                case MessageButtons.OkCancel:
                    btnOk.Visibility = btnCancel.Visibility = Visibility.Visible;
                    break;

                case MessageButtons.YesNo:
                    btnYes.Visibility = btnNo.Visibility = Visibility.Visible;
                    break;

                case MessageButtons.Ok:
                    btnOk.Visibility = Visibility.Visible;
                    break;
            }
        }

    }
}
