using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Camera
{
    public Matrix Transform { get; private set; }
    
    public void Follow(Vector2 targetPosition, Viewport viewport)
    {
        // 1. Dịch chuyển ngược lại với vị trí của mục tiêu (đưa thế giới về gốc tọa độ dựa trên mục tiêu)
        Matrix positionMatrix = Matrix.CreateTranslation(-targetPosition.X, -targetPosition.Y, 0);

        // 2. Dịch chuyển một khoảng bằng nửa kích thước màn hình để đưa mục tiêu vào CHÍNH GIỮA màn hình
        Matrix offsetMatrix = Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0);

        // Nhân hai ma trận lại với nhau để tạo ra ma trận biến đổi cuối cùng
        Transform = positionMatrix * offsetMatrix;
    }
}