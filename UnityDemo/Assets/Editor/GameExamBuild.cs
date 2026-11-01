using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class GameExamBuild
{
    [MenuItem("GameExam/Build Windows Release")]
    public static void Windows()
    {
        string output = Environment.GetEnvironmentVariable("GAMEEXAM_BUILD_OUTPUT");
        if (string.IsNullOrEmpty(output))
            output = Path.GetFullPath("Builds/Windows/GameExam.exe");
        output = Path.GetFullPath(output);
        string folder = Path.GetDirectoryName(output);
        Directory.CreateDirectory(folder);

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Build failed: " + report.summary.result
                + "; errors=" + report.summary.totalErrors);

        string data = Path.Combine(folder, Path.GetFileNameWithoutExtension(output) + "_Data");
        if (!File.Exists(output) || !Directory.Exists(data)
            || !File.Exists(Path.Combine(folder, "UnityPlayer.dll")))
            throw new InvalidOperationException("Build output is missing executable, data directory or UnityPlayer.dll.");

        File.WriteAllText(Path.Combine(folder, "请先阅读.txt"),
            "背包消除 Demo Windows 64 位免安装版\r\n\r\n"
            + "将压缩包完整解压到普通文件夹，双击 " + Path.GetFileName(output) + " 启动。\r\n"
            + "无需安装 Unity、Visual Studio 或开发用 .NET SDK。\r\n"
            + "请保留全部文件及目录，不要只复制 exe，也不要在压缩软件内直接启动。\r\n\r\n"
            + "操作：点击两个相邻棋子交换；底部按钮可重开、提示、自动演示。\r\n"
            + "快捷键：R 重开当前关卡，F2 切换两套关卡；关闭游戏窗口退出。\r\n"
            + "自动演示在主关按六步推荐路线推进。修改窗口大小可检查布局适配。\r\n\r\n"
            + "验证参数：-verify-route；-verify-route -no-animation；-second-level。\r\n"
            + "本包通过 Unity 构建及必需文件检查。实际运行验收结果应以另附记录为准。\r\n",
            new UTF8Encoding(false));

        // Keep every runtime file. Only Unity's explicitly non-shipping debug backups are excluded.
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(path => !path.Substring(folder.Length).Split(Path.DirectorySeparatorChar)
                .Any(part => part.EndsWith("_BackUpThisFolder_ButDontShipItWithYourGame", StringComparison.Ordinal)
                    || part.EndsWith("_BurstDebugInformation_DoNotShip", StringComparison.Ordinal)))
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        string archivePath = Path.Combine(Path.GetDirectoryName(folder),
            "GameExam-Windows-x64-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            foreach (string file in files)
                archive.CreateEntryFromFile(file,
                    "GameExam/" + file.Substring(folder.Length + 1).Replace('\\', '/'),
                    CompressionLevel.Optimal);
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(archivePath))
            File.WriteAllText(archivePath + ".sha256",
                BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "")
                + "  " + Path.GetFileName(archivePath) + Environment.NewLine);

        UnityEngine.Debug.Log("[Delivery] Windows build succeeded: " + output);
        UnityEngine.Debug.Log("[Delivery] Portable package created: " + archivePath);
        if (!UnityEngine.Application.isBatchMode)
            EditorUtility.RevealInFinder(archivePath);
    }
}
