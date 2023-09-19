using CommunityToolkit.Mvvm.Messaging.Messages;
namespace DataSigil.Messages;

public class CultureChangeMessage : ValueChangedMessage<string>
{
    public CultureChangeMessage(string value) : base(value)
    {
    }
}
