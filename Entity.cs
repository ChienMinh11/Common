using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class Entity
{
    public Texture2D Texture { get; set; }
    public Vector2 Position { get; set; }
    public float Speed { get; set; }
    public bool IsExpired { get; set; } = false;

    public Rectangle Bounds
    {
        get
        {
            if(this.Texture == null) return Rectangle.Empty;
            // Ép kiểu Position về int vì Rectangle trong MonoGame sử dụng tọa độ nguyên (int)
            return new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
        }
    }
    
    public Entity(Vector2 position, float speed, string assetName, Func<string, Texture2D> loadTextureFunc)
    {
        Position = position;
        Speed = speed;
       
        if (loadTextureFunc != null && !string.IsNullOrEmpty(assetName))
        {
            this.Texture = loadTextureFunc(assetName);
        }
    }
    public virtual void Update(GameTime gameTime){}

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (this.Texture != null)
        {
            // Làm tròn vị trí thực tế về số nguyên gần nhất trước khi render lên màn hình
            Vector2 renderPosition = new Vector2(
                (float)Math.Round(Position.X),
                (float)Math.Round(Position.Y)
            );

            spriteBatch.Draw(Texture, renderPosition, Color.White);
        }
    }
}
    