using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Bullet: Entity
{
    private Vector2 _direction;
    private float _lifetime;

    public Bullet(Vector2 position, float speed, string assetName,
        Func<string, Texture2D> loadTextureFunc, Vector2 direction,float lifetime) : base(position, speed, assetName,loadTextureFunc)
    {
        _direction = direction;
        _lifetime = lifetime;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. Di chuyển đạn theo hướng và vận tốc
        Position += _direction * Speed * deltaTime;

        // 2. Giảm thời gian sống dựa trên thời gian thực trôi qua
        _lifetime -= deltaTime;

        // 3. Nếu hết thời gian sống, đánh dấu Expired để EntityManager tự động xóa xóa đạn khỏi game
        if (_lifetime <= 0)
        {
            this.IsExpired = true;
        }

        base.Update(gameTime);
    }
}