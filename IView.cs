namespace ChieChie.MVP
{
    /// <summary>
    /// Interface gốc cho mọi View. Mỗi feature nên định nghĩa một interface
    /// riêng kế thừa từ IView (ví dụ ICounterView, ILoginView...) để khai báo
    /// rõ những gì Presenter được phép gọi trên View — đây chính là "contract".
    /// View (thường là MonoBehaviour) chỉ hiển thị dữ liệu và chuyển tiếp input
    /// người dùng cho Presenter, không tự chứa logic nghiệp vụ.
    /// </summary>
    public interface IView
    {
        
    }
}
