
using CommunityToolkit.Mvvm.ComponentModel;

namespace CacelApp.Services.Loading;

public class LoadingService : ObservableObject, ILoadingService
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadingStateChanged?.Invoke(_isLoading);
            }
        }
    }
    public event Action<bool> LoadingStateChanged;

    public void StartLoading() => IsLoading = true;
    public void StopLoading() => IsLoading = false;
}
