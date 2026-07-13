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

        /// <summary>Gọi trong Awake() của View con, sau khi đã tạo Presenter.</summary>
        protected void BindPresenter(TPresenter presenter)
        {
            Presenter = presenter;
            Presenter.Initialize();
        }
        protected virtual void OnDestroy()
        {
            Presenter?.Dispose();
        }
    }
}
