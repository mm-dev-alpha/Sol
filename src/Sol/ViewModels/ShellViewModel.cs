using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sol.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public UserWorkspaceViewModel UserWorkspace { get; }
    public GlobalSearchViewModel Search { get; }

    public ShellViewModel(UserWorkspaceViewModel userWorkspace, GlobalSearchViewModel search)
    {
        UserWorkspace = userWorkspace;
        Search = search;
    }
}
