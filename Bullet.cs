using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Bullet: Entity
{
    private Vector2 _direction;

    public Bullet(Vector2 position, float speed, string assetName,
        Func<string, Texture2D> loadTextureFunc, Vector2 direction) : base(position, speed, assetName,loadTextureFunc)
    {
        _direction = direction;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Position += _direction*Speed*deltaTime;

        if (Position.X < -20 || Position.X > 820 || Position.Y < -20 || Position.Y > 500)
        {
            this.IsExpired = true;
        }
        base.Update(gameTime);
    }
}