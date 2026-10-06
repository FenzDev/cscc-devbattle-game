using MonoGame.Extended;
using MonoGame.Extended.Collisions;

public abstract class Entity : ICollisionActor
{
  private static int _nextId;

  public int Id { get; } = _nextId++;

  public abstract CollisionShape2D Shape { get; }

  public void Update()
  {
    
  }

  public void Draw()
  {
    
  }
}