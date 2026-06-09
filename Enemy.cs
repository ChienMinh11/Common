using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Enemy: Entity
{
    private float _direction = 1f;
  
    public Enemy(Vector2 position, float speed, string assetName, Func<string, Texture2D> loadTextureFunc) 
        : base(position, speed, assetName, loadTextureFunc)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (this.IsExpired) return;
        // float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //
        // Position = new Vector2(Position.X + (Speed * _direction * deltaTime), Position.Y);
        //
        // if (Position.X > 700) _direction = -1f;
        // else if (Position.X < 50) _direction = 1f;
      
        base.Update(gameTime);
    }
}