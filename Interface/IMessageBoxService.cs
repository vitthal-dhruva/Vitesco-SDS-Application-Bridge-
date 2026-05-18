using Hiemdall_bridge;

public interface IMessageBoxService
{
    bool? Show(string message,
               MessageBoxCustom.MessageType messageType,
               MessageBoxCustom.MessageButtons messageButtons = MessageBoxCustom.MessageButtons.Ok);
}
