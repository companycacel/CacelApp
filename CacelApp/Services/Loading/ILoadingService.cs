namespace CacelApp.Services.Loading;

public interface ILoadingService
{
    bool IsLoading { get; }
    event Action<bool> LoadingStateChanged;
    void StartLoading();
    void StopLoading();
}
