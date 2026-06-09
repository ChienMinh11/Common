using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ChieChie;

public class MainGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private EntityManager _entityManager;
    private LevelManager _levelManager;
    private Camera _camera;

    public MainGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _entityManager = new EntityManager();
        _levelManager = new LevelManager(_entityManager);
        _camera = new Camera();
        ResourceManager.Instance.Initialize(this.Content);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _levelManager.LoadLevel(1);
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
      
        _entityManager.Update(gameTime);
        
        Player player = _entityManager.GetPlayer();
        if (player != null)
        {
            Vector2 targetPos = player.Position;
          
            if (player.Texture != null)
            {
                targetPos += new Vector2(player.Texture.Width / 2f, player.Texture.Height / 2f);
            }
            
            _camera.Follow(targetPos, GraphicsDevice.Viewport);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);

        _spriteBatch.Begin(transformMatrix: _camera.Transform);

        _entityManager.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}