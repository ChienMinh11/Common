using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class EntityManager
{
    private readonly List<Entity> _entities = new();
    private readonly List<Entity> _entitiesToAdd = new();
    private readonly CollisionManager _collisionManager = new();

    public Player GetPlayer()
    {
        return _entities.OfType<Player>().FirstOrDefault();
    }
    public void AddEntity(Entity entity)
    {
        if(entity ==null) return;
        _entities.Add(entity);
    }
    
    public void AddEntityRuntime(Entity entity)
    {
        if (entity == null) return;
        _entitiesToAdd.Add(entity); 
    }
    
    public void Update(GameTime gameTime)
    {
       
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            if (i < _entities.Count && _entities[i] != null)
            {
                _entities[i].Update(gameTime);
            }
        }
     
        if (_entitiesToAdd.Count > 0)
        {
            _entities.AddRange(_entitiesToAdd);
            _entitiesToAdd.Clear();
        }

        _collisionManager.CheckCollisions(_entities);
        _entities.RemoveAll(entity => entity.IsExpired);
    }
    public void Draw(SpriteBatch spriteBatch)
    {       
        foreach (var entity in _entities)
        {
            entity.Draw(spriteBatch);
        }
    }
  
    public void ClearAll()
    {
        _entities.Clear();
        _entitiesToAdd.Clear();
    }
}