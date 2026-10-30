using System;
using BagElimination.Rules.Board;
using BagElimination.Rules.Config;
using UnityEngine;

namespace BagElimination.Presentation
{
    /// <summary>
    /// Single presentation entry point. It owns the startup sequence and nothing else:
    /// build the config, run the full validation (structure, no opening match, at least one
    /// legal swap), build the board and hand it to <see cref="BoardView"/> for display.
    ///
    /// On validation failure it logs the concrete reasons and stops; it never draws a fake
    /// "normal" board. BoardView stays a passive renderer and is never created here twice.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (UnityEngine.Object.FindFirstObjectByType<BoardView>() != null)
            {
                // A BoardView already exists (for example wired into a scene by hand); do not duplicate it.
                Debug.LogWarning("[GameBootstrap] 场景中已存在 BoardView，跳过自动初始化。");
                return;
            }

            BoardState board;
            try
            {
                LevelConfig config = DemoLevelConfig.Create();

                LevelConfigValidationResult validation = LevelConfigValidator.Validate(config);
                if (!validation.IsValid)
                {
                    foreach (LevelConfigError error in validation.Errors)
                    {
                        Debug.LogError("[GameBootstrap] 关卡配置校验失败: " + error);
                    }

                    return; // Stop initialization: a board that failed validation must not be shown.
                }

                board = BoardFactory.Create(config);
            }
            catch (Exception exception)
            {
                // A top-level boundary: report the failure instead of letting the startup hang silently.
                Debug.LogError("[GameBootstrap] 初始化异常: " + exception);
                return;
            }

            var root = new GameObject("BoardPresentation");
            root.AddComponent<BoardView>().Render(board);
        }
    }
}