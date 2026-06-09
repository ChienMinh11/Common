using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ChieChie;

public class Player: Entity
{
    public Action<Entity> OnSpawnEntity;
    private readonly Func<string, Texture2D> _loadTextureFunc;
    private Rectangle _mapBounds;
    
    public Player(Vector2 position, float speed, string assetName, Func<string, Texture2D> loadTextureFunc) 
        : base(position, speed, assetName, loadTextureFunc)
    {
        _loadTextureFunc = loadTextureFunc;
        _mapBounds = new Rectangle(0, 0, 800, 800);
    }
    
    public void SetMapBounds(Rectangle bounds)
    {
        _mapBounds = bounds;
    }
    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
     
        if (InputManager.IsKeyDown(Keys.Up) || InputManager.IsKeyDown(Keys.W))
            Position = Position with { Y = Position.Y - Speed * deltaTime };
            
        if (InputManager.IsKeyDown(Keys.Down) || InputManager.IsKeyDown(Keys.S))
            Position = Position with { Y = Position.Y + Speed * deltaTime };
            
        if (InputManager.IsKeyDown(Keys.Left) || InputManager.IsKeyDown(Keys.A))
            Position = Position with { X = Position.X - Speed * deltaTime };
            
        if (InputManager.IsKeyDown(Keys.Right) || InputManager.IsKeyDown(Keys.D))
            Position = Position with { X = Position.X + Speed * deltaTime };
 
        // 3. THÊM GIỚI HẠN DI CHUYỂN TẠI ĐÂY
        // Lấy chiều rộng và cao của nhân vật, nếu chưa load xong texture thì mặc định là 0
        float playerWidth = Texture != null ? Texture.Width : 0f;
        float playerHeight = Texture != null ? Texture.Height : 0f;

        // Giới hạn trục X: từ cạnh trái bản đồ đến cạnh phải bản đồ (trừ đi bề rộng nhân vật)
        float clampedX = MathHelper.Clamp(Position.X, _mapBounds.Left, _mapBounds.Right - playerWidth);
        
        // Giới hạn trục Y: từ cạnh trên bản đồ đến cạnh dưới bản đồ (trừ đi bề cao nhân vật)
        float clampedY = MathHelper.Clamp(Position.Y, _mapBounds.Top, _mapBounds.Bottom - playerHeight);

        // Cập nhật lại vị trí đã được bo góc an toàn
        Position = new Vector2(clampedX, clampedY);

        // --- Xử lý bắn đạn giữ nguyên ---
        if (InputManager.IsKeyPressed(Keys.Space))
        {
            Vector2 bulletSpawnPos = new Vector2(Position.X + (playerWidth / 2f) - 4f, Position.Y);
            Bullet newBullet = new Bullet(bulletSpawnPos,
                500f,
                "Bullet1",
                _loadTextureFunc, 
                new Vector2(0, -1),
                2
                );
            OnSpawnEntity?.Invoke(newBullet);
        }
        
        base.Update(gameTime);
    }
}