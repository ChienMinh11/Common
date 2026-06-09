using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Camera
{
    public Matrix Transform { get; private set; }
    
    public void Follow(Vector2 targetPosition, Viewport viewport, Rectangle mapBounds)
    {
        // 1. Tính toán biên tối thiểu và tối đa mà tâm Camera được phép di chuyển tới
        float minX = mapBounds.Left + viewport.Width / 2f;
        float maxX = mapBounds.Right - viewport.Width / 2f;
        float minY = mapBounds.Top + viewport.Height / 2f;
        float maxY = mapBounds.Bottom - viewport.Height / 2f;

        // 2. Nếu kích thước bản đồ nhỏ hơn hoặc bằng màn hình (như trường hợp 800x800 hiện tại)
        // Thì cố định Camera nằm chính giữa bản đồ, không cho di chuyển tự do nữa.
        float clampedX = maxX < minX ? (mapBounds.Left + mapBounds.Right) / 2f : MathHelper.Clamp(targetPosition.X, minX, maxX);
        float clampedY = maxY < minY ? (mapBounds.Top + mapBounds.Bottom) / 2f : MathHelper.Clamp(targetPosition.Y, minY, maxY);

        // Dịch chuyển thế giới dựa trên tọa độ Camera đã được giới hạn (clamped)
        Matrix positionMatrix = Matrix.CreateTranslation(-clampedX, -clampedY, 0);
        Matrix offsetMatrix = Matrix.CreateTranslation(viewport.Width / 2f, viewport.Height / 2f, 0);

        Transform = positionMatrix * offsetMatrix;
    }
}