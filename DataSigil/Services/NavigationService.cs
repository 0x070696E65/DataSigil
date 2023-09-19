using System.Diagnostics;

namespace DataSigil.Services;

public interface INavigationService {
    //Task NavigateToSecondPage();
    Task NavigateBack();
}

public class NavigationService : INavigationService {

    readonly IServiceProvider _services;

    protected INavigation Navigation {
        get {
            INavigation navigation = Application.Current?.MainPage?.Navigation;
            if (navigation is not null)
                return navigation;
            if (Debugger.IsAttached)
                Debugger.Break();
            throw new Exception();
        }
    }

    //使用するクラスを外部から渡す(Dependency Injection)
    public NavigationService(IServiceProvider services) => _services = services;

    //FirstPage へ遷移する
    //public Task NavigateToFirstPage() => NavigateToPage<FirstPage>();

    private Task NavigateToPage<T>() where T : Page {
        var page = ResolvePage<T>();
        if (page is not null)
            //ページ遷移の実施
            return Navigation.PushAsync(page, true);
        throw new InvalidOperationException($"Unable to resolve type {typeof(T).FullName}");
    }

    private T ResolvePage<T>() where T : Page => _services.GetService<T>();

    //前ページへ戻る
    public Task NavigateBack() {
        if (Navigation.NavigationStack.Count > 1)
            //前ページへ戻る
            return Navigation.PopAsync();
        throw new InvalidOperationException("No pages to navigate back to!");
    }
}