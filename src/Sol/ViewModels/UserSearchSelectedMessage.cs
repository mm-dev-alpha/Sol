using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Sol.ViewModels;

public class UserSearchSelectedMessage : ValueChangedMessage<string>
{
    public UserSearchSelectedMessage(string samAccountName) : base(samAccountName)
    {
    }
}
