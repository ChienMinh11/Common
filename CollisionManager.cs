using System.Collections.Generic;
using System.Linq;

namespace ChieChie;

public class CollisionManager
{
    public void CheckCollisions(List<Entity> entities)
    {
        // 1. Phân loại các Entity hiện có trong Game để xử lý cho mượt
        var players = entities.OfType<Player>().ToList();
        var enemies = entities.OfType<Enemy>().ToList();
        var bullets = entities.OfType<Bullet>().ToList();

        // 2. XỬ LÝ VA CHẠM: ĐẠN TRÚNG KẺ ĐỊCH
        foreach (var bullet in bullets)
        {
            // Nếu đạn đã biến mất từ vòng loop trước thì bỏ qua
            if (bullet.IsExpired) continue; 

            foreach (var enemy in enemies)
            {
                if (enemy.IsExpired) continue;

                // Kiểm tra hai hình hộp có đè lên nhau không
                if (bullet.Bounds.Intersects(enemy.Bounds))
                {
                    bullet.IsExpired = true;  // Hủy viên đạn
                    enemy.IsExpired = true;   // Hủy kẻ địch
                    
                    // Thao tác này tối ưu: Đạn đã nổ thì không cần check với các Enemy khác nữa
                    break; 
                }
            }
        }

        // 3. XỬ LÝ VA CHẠM: KẺ ĐỊCH ĐÂM TRÚNG NGƯỜI CHƠI
        foreach (var enemy in enemies)
        {
            if (enemy.IsExpired) continue;

            foreach (var player in players)
            {
                if (player.IsExpired) continue;

                if (enemy.Bounds.Intersects(player.Bounds))
                {
                    enemy.IsExpired = true; // Kẻ địch nổ
                    
                    // Tạm thời in ra Console hoặc bạn có thể xử lý trừ máu Player ở đây
                    System.Diagnostics.Debug.WriteLine("Player bị trúng đòn!");
                    // player.IsExpired = true; // Nếu muốn chết luôn thì uncomment dòng này
                }
            }
        }
    }
}