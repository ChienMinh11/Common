using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class LevelManager
{
    private readonly EntityManager _entityManager;
    public Rectangle CurrentMapBounds { get; private set; }
    
    public LevelManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    public void LoadLevel(int levelNumber)
    {
        _entityManager.ClearAll();
   
        switch (levelNumber)
        {
            case 1:
                SetupLevelOne();
                break;
            case 2:
                // SetupLevelTwo(texture);
                break;
        }
      
    }
    
    
    private void SetupLevelOne()
    {
        // 1. Định nghĩa biên bản đồ duy nhất tại đây cho Level 1
        CurrentMapBounds = new Rectangle(0, 0, 400, 400);

        Func<string, Texture2D> loader = ResourceManager.Instance.GetTexture;

        // Tận dụng CurrentMapBounds để tính tâm màn hình luôn
        float mapCenterX = CurrentMapBounds.Width / 2f;
        float mapCenterY = CurrentMapBounds.Height / 2f;

        Player player = new Player(new Vector2(mapCenterX - 16f, mapCenterY + 80f), 300f, "Player", loader);
        
        // 2. Truyền biên bản đồ vừa tạo vào cho Player
        player.SetMapBounds(CurrentMapBounds);

        player.OnSpawnEntity = newEntity => 
        { 
            _entityManager.AddEntityRuntime(newEntity); 
        };
        _entityManager.AddEntity(player);
        
        _entityManager.AddEntity(new Enemy(new Vector2(mapCenterX - 16f, mapCenterY - 60f), 50f, "e1", loader));
        _entityManager.AddEntity(new Enemy(new Vector2(mapCenterX + 32f, mapCenterY - 60f), 70f, "e2", loader));
        _entityManager.AddEntity(new Enemy(new Vector2(mapCenterX - 64f, mapCenterY - 60f), 40f, "e3", loader));
    }
    
}