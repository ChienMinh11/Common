namespace ChieChie.MVP
{
    /// <summary>
    /// Interface gốc, không generic, cho mọi Presenter.
    /// Tách riêng interface này (thay vì chỉ dùng class generic) để View
    /// có thể tham chiếu Presenter mà không cần biết kiểu generic cụ thể,
    /// hữu ích khi cần quản lý danh sách Presenter (ví dụ PresenterManager).
    /// </summary>
    public interface IPresenter
    {
        void Initialize();
        void Dispose();
    }
}
