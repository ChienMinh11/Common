using System;

namespace ChieChie.MVP
{
    /// <summary>
    /// Base Presenter generic dùng chung cho mọi feature.
    /// TView  : interface contract của View (ví dụ ICounterView).
    /// TModel : Model thuần dữ liệu tương ứng (ví dụ CounterModel).
    ///
    /// Cách dùng: mỗi feature tạo 1 class kế thừa
    ///     public class CounterPresenter : BasePresenter&lt;ICounterView, CounterModel&gt;
    /// và override OnInitialize()/OnDispose() để subscribe/unsubscribe sự kiện.
    /// </summary>
    public abstract class BasePresenter<TView, TModel> : IPresenter
        where TView : class, IView
        where TModel : IModel
    {
        protected TView View { get; private set; }
        protected TModel Model { get; private set; }

        private bool _isInitialized;

        protected BasePresenter(TView view, TModel model)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// Gọi 1 lần khi View sẵn sàng (thường trong Awake/Start của View).
        /// Idempotent: gọi nhiều lần cũng không lặp lại logic khởi tạo.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            OnInitialize();
        }

        /// <summary>
        /// Gọi khi View bị hủy (thường trong OnDestroy). Dùng để unsubscribe
        /// event, tránh memory leak khi Model sống lâu hơn View.
        /// </summary>
        public void Dispose()
        {
            if (!_isInitialized) return;
            OnDispose();
            _isInitialized = false;
        }

        /// <summary>Override: subscribe sự kiện Model, đẩy trạng thái ban đầu ra View.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>Override: unsubscribe sự kiện, giải phóng tham chiếu.</summary>
        protected virtual void OnDispose() { }
    }
}
