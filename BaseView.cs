using ChieChie.Core;
using UnityEngine;

namespace ChieChie.MVP
{
    /// <summary>
    /// Base View generic (MonoBehaviour) dùng chung cho mọi feature.
    /// TPresenter: kiểu Presenter cụ thể, phải implement IPresenter.
    ///
    /// Cách dùng: mỗi feature tạo 1 class kế thừa
    ///     public class CounterView : BaseView&lt;CounterPresenter&gt;, ICounterView
    /// rồi trong Awake() tự khởi tạo Model + Presenter và gọi BindPresenter(presenter).
    ///
    /// Lý do không tự new TPresenter() ở đây: Presenter thường cần Model làm
    /// tham số constructor, nên việc khởi tạo được để lại cho View cụ thể.
    /// </summary>
    public abstract class BaseView<TPresenter> : MonoBehaviour, IView
        where TPresenter : IPresenter
    {
        protected TPresenter Presenter { get; private set; }
 
        /// <summary>Gọi trong Awake()/Start() của View con, sau khi đã tự tạo Presenter bằng "new".</summary>
        protected void BindPresenter(TPresenter presenter)
        {
            Presenter = presenter;
            Presenter.Initialize();
        }
 
        /// <summary>
        /// Overload dùng khi Presenter được tạo qua DI factory thay vì View tự "new".
        /// DI container inject <paramref name="factory"/> vào View (qua [Inject] hoặc method injection),
        /// View chỉ cần gọi BindPresenter(_presenterFactory, this).
        /// </summary>
        protected void BindPresenter<TView>(IPresenterFactory<TPresenter, TView> factory, TView view)
            where TView : IView
        {
            BindPresenter(factory.Create(view));
        }
      
 
        protected virtual void OnDestroy()
        {
            Presenter?.Dispose();
        }
    }
}
