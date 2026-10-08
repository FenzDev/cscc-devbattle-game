using Microsoft.Xna.Framework.Graphics;

public static class Assets
{
  public static T Get<T>(string assetname)
  {
    return Game1.Singleton.Content.Load<T>(assetname);
  }  
  
  public static Texture2D GetTexture(string assetname)
  {
    return Game1.Singleton.Content.Load<Texture2D>($"Textures/{assetname}");
  }  
}