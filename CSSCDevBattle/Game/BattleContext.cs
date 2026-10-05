public class BattleContext {
  public BattleContext Singleton { get; }

  public BattleContext()
  {
    Singleton = this;
  }
  
}