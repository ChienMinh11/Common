using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ChieChie;

public class Player: Entity
{
    public Action<Entity> OnSpawnEntity;
    private readonly Func<string, Texture2D> _loadTextureFunc;
    
    public Player(Vector2 position, float speed, string assetName, Func<string, Texture2D> loadTextureFunc) 
        : base(position, speed, assetName, loadTextureFunc)
    {
        _loadTextureFunc = loadTextureFunc;
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
 
        if (InputManager.IsKeyPressed(Keys.Space))
        {
            Vector2 bulletSpawnPos = new Vector2(Position.X + 16, Position.Y); 
            Bullet newBullet = new Bullet(
                position: bulletSpawnPos, 
                speed: 500f, 
                assetName: "Bullet1", 
                loadTextureFunc: _loadTextureFunc, 
                direction: new Vector2(0, -1)
            );
            OnSpawnEntity?.Invoke(newBullet);
        }
        
        base.Update(gameTime);
    }
}