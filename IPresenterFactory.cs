using ChieChie.MVP;
using UnityEngine;

namespace ChieChie.Core
{
    /// <summary>
    /// Factory tạo Presenter, để DI container (Zenject, VContainer, hoặc container tự viết)
    /// chịu trách nhiệm "new" Presenter (và các dependency bên trong nó), thay vì
    /// View tự gọi "new CounterPresenter(...)" trực tiếp.
    ///
    /// TPresenter : kiểu Presenter cụ thể cần tạo (ví dụ CounterPresenter).
    /// TView      : kiểu contract View mà Presenter đó cần (ví dụ ICounterView).
    ///
    /// Lợi ích:
    /// - Nếu Model/Presenter cần dependency khác (service, repository, API client...),
    ///   dependency đó được inject vào constructor của Factory, DI container tự resolve,
    ///   View hoàn toàn không cần biết.
    /// - View chỉ cần gọi factory.Create(this) mà không quan tâm Model được tạo thế nào.
    /// - Dễ tạo Factory giả (mock/fake) khi viết unit test cho View logic.
    ///
    /// Đăng ký với DI container (ví dụ):
    ///   Zenject  : Container.Bind&lt;IPresenterFactory&lt;CounterPresenter, ICounterView&gt;&gt;()
    ///                       .To&lt;CounterPresenterFactory&gt;().AsSingle();
    ///   VContainer: builder.Register&lt;IPresenterFactory&lt;CounterPresenter, ICounterView&gt;,
    ///                       CounterPresenterFactory&gt;(Lifetime.Singleton);
    /// </summary>
    public interface IPresenterFactory<TPresenter, in TView>
        where TPresenter : IPresenter
        where TView : IView
    {
        TPresenter Create(TView view);
    }
}
