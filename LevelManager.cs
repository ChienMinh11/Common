using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class LevelManager
{
    private readonly EntityManager _entityManager;
    
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
        Func<string, Texture2D> loader = ResourceManager.Instance.GetTexture;

        Player player = new Player(new Vector2(0,0), 300f, "Player", loader);
        player.OnSpawnEntity = newEntity => 
        { 
            _entityManager.AddEntityRuntime(newEntity); 
        };
        _entityManager.AddEntity(player);
        _entityManager.AddEntity(new Enemy(new Vector2(0,100), 50f, "e1", loader));
        _entityManager.AddEntity(new Enemy(new Vector2(50, 100), 70f, "e2", loader));
        _entityManager.AddEntity(new Enemy(new Vector2(-50,100), 40f, "e3", loader));
    }
    
}