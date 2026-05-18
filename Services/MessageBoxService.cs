using Hiemdall_bridge;
using Hiemdall_bridge.Interface;
using System.Windows;

public class MessageBoxService : IMessageBoxService
{
    public bool? Show(string message,
                      MessageBoxCustom.MessageType messageType,
                      MessageBoxCustom.MessageButtons messageButtons)
    {
        var msg = new MessageBoxCustom();

        msg.Owner = Application.Current.MainWindow;
        msg.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        msg.SetMessage(message, messageType, messageButtons);

        return msg.ShowDialog();
    }
}
