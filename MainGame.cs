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
        
        // --- CẤU HÌNH MÀN HÌNH VUÔNG CHO PIXEL ART ---
        _graphics.PreferredBackBufferWidth = 320;  // Chiều rộng
        _graphics.PreferredBackBufferHeight = 320; // Chiều cao (bằng chiều rộng để tạo hình vuông)
        // Nếu muốn game chạy mượt mà theo tần số quét màn hình, bạn có thể giữ nguyên hoặc bật VSync
        _graphics.SynchronizeWithVerticalRetrace = true;
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
    
            _camera.Follow(targetPos, GraphicsDevice.Viewport, _levelManager.CurrentMapBounds);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkSlateGray);

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp, // <--- ĐÂY LÀ CHÌA KHÓA: Giúp ảnh pixel không bị nhòe!
            depthStencilState: null,
            rasterizerState: null,
            effect: null,
            transformMatrix: _camera.Transform
        );

        _entityManager.Draw(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}