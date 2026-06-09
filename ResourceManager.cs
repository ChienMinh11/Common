using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ChieChie;

public class ResourceManager
{
    private static ResourceManager _instance;
    public static ResourceManager Instance => _instance ??= new ResourceManager();
    private ContentManager _content;
    private Dictionary<string, Texture2D> _textures;
    
    private ResourceManager()
    {
        _textures = new Dictionary<string, Texture2D>();
    }
    public void Initialize(ContentManager content)
    {
        _content = content;
    }
    public Texture2D GetTexture(string assetName)
    {
        if (_textures.TryGetValue(assetName, out var texture))
        {
            return texture;
        }
     
        Texture2D loadedTexture = _content.Load<Texture2D>(assetName);
        _textures.Add(assetName, loadedTexture);
        return loadedTexture;
    }
    public void Unload()
    {
        _textures.Clear();
    }
}