using DungeonEscape.Core;
using Raylib_cs;
using System.Runtime.ExceptionServices;

const int InitialWindowWidth = 1280;
const int InitialWindowHeight = 720;

Game? game = null;

AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception ex)
    {
        RuntimeDiagnostics.Error("AppDomain unhandled exception.", ex);
    }
};

TaskScheduler.UnobservedTaskException += (_, args) =>
{
    RuntimeDiagnostics.Error("TaskScheduler unobserved exception.", args.Exception);
    args.SetObserved();
};

RuntimeDiagnostics.Info($"App start. .NET={Environment.Version} OS={Environment.OSVersion}");

if (args.Any(a => string.Equals(a, "--phase7-checks", StringComparison.OrdinalIgnoreCase)))
{
    RuntimeDiagnostics.Info("Phase 7 self-checks requested.");
    var result = Phase7SelfChecks.RunAll();
    if (!result.IsSuccess)
    {
        RuntimeDiagnostics.Warn($"Phase 7 self-checks failed. Failed={result.Failed} Passed={result.Passed}");
        Console.Error.WriteLine($"Phase7Checks: FAILED ({result.Failed} failed, {result.Passed} passed)");
        foreach (var failure in result.Failures)
        {
            Console.Error.WriteLine($" - {failure}");
        }

        Environment.ExitCode = 1;
    }
    else
    {
        RuntimeDiagnostics.Info($"Phase 7 self-checks passed. Passed={result.Passed}");
        Console.WriteLine($"Phase7Checks: PASS ({result.Passed} checks)");
    }

    return;
}

try
{
    Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
    Raylib.InitWindow(
        InitialWindowWidth,
        InitialWindowHeight,
        "Dungeon Escape - PC Prototype");
    Raylib.SetWindowMinSize(1024, 640);
    // Keep ESC available for in-game menus (pause, back, etc.) instead of closing the app.
    Raylib.SetExitKey(KeyboardKey.Null);
    Raylib.SetTargetFPS(60);

    game = new Game();
    while (!Raylib.WindowShouldClose())
    {
        game.Update();

        Raylib.BeginDrawing();
        game.Draw();
        Raylib.EndDrawing();
    }

    RuntimeDiagnostics.Info("App exit requested by user.");
}
catch (Exception ex)
{
    var context = game?.GetDiagnosticsSummary() ?? "Game=<null>";
    RuntimeDiagnostics.Error("Unhandled fatal exception in main loop.", ex, context);
    Console.Error.WriteLine("[FATAL] A runtime error occurred. Check log file:");
    Console.Error.WriteLine($"        {RuntimeDiagnostics.GetCurrentLogPath()}");
    ExceptionDispatchInfo.Capture(ex).Throw();
}
finally
{
    game?.Dispose();
    if (Raylib.IsWindowReady())
    {
        Raylib.CloseWindow();
    }
}

