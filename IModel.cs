namespace ChieChie.MVP
{
    /// <summary>
    /// Interface đánh dấu (marker) cho mọi Model trong kiến trúc MVP.
    /// Model chỉ chứa dữ liệu thuần + logic nghiệp vụ, KHÔNG tham chiếu tới
    /// bất kỳ thành phần nào của Unity (MonoBehaviour, Transform, GameObject...).
    /// Điều này giúp Model có thể unit-test độc lập, không cần chạy trong Unity.
    /// </summary>
    public interface IModel
    {
    }
}
