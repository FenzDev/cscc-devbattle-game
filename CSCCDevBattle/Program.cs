using System;

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
  Console.Error.WriteLine();
  Console.Error.WriteLine("========== UNHANDLED EXCEPTION ==========");
  Console.Error.WriteLine(e.ExceptionObject);
  Console.Error.WriteLine("=========================================");
};

try
{
  using var game = new Game1();
  game.Run();
}
catch (Exception ex)
{
  Console.Error.WriteLine();
  Console.Error.WriteLine("========== FATAL EXCEPTION ==========");
  Console.Error.WriteLine(ex.ToString());
  Console.Error.WriteLine("=====================================");
  throw;
}