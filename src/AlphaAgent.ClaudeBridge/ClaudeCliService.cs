using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaAgent.ClaudeBridge;

public class ClaudeCliService
{
    private readonly ClaudeCliConfig _config;
    private readonly ConcurrentDictionary<Guid, string> _conversationSessions = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _conversationLocks = new();
    private readonly string _sessionsFilePath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public ClaudeCliService(ClaudeCliConfig config)
    {
        _config = config;
        _sessionsFilePath = Path.Combine(
            config.WorkingDirectory ?? AppContext.BaseDirectory,
            ".claudebridge",
            "sessions.json");
        LoadSessions();
    }

    private void LoadSessions()
    {
        try
        {
            if (File.Exists(_sessionsFilePath))
            {
                var json = File.ReadAllText(_sessionsFilePath);
                var sessions = JsonSerializer.Deserialize<Dictionary<Guid, string>>(json);
                if (sessions != null)
                {
                    foreach (var (convId, sessionId) in sessions)
                    {
                        _conversationSessions[convId] = sessionId;
                    }
                    Console.WriteLine($"[信息] 已加载 {sessions.Count} 个会话映射");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 加载会话映射失败: {ex.Message}");
        }
    }

    private async Task SaveSessionsAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_sessionsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sessions = new Dictionary<Guid, string>();
            foreach (var (convId, sessionId) in _conversationSessions)
            {
                sessions[convId] = sessionId;
            }

            var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_sessionsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 保存会话映射失败: {ex.Message}");
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public async Task<string> SendMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
    {
        var semaphore = _conversationLocks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await SendMessageCoreAsync(conversationId, message, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<string> SendMessageCoreAsync(Guid conversationId, string message, CancellationToken cancellationToken)
    {
        bool isFirstMessage;
        string sessionId;

        if (_conversationSessions.TryGetValue(conversationId, out var existingId))
        {
            isFirstMessage = false;
            sessionId = existingId;
        }
        else
        {
            isFirstMessage = true;
            sessionId = Guid.NewGuid().ToString();
            _conversationSessions[conversationId] = sessionId;
            _ = SaveSessionsAsync(); // 异步持久化，不阻塞当前请求
        }

        var arguments = BuildArguments(message, sessionId, isFirstMessage);

        var startInfo = new ProcessStartInfo
        {
            FileName = _config.CliPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _config.WorkingDirectory ?? Environment.CurrentDirectory,
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[stderr] {e.Data}");
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[错误] 无法启动 Claude CLI: {ex.Message}");
            Console.ResetColor();
            return $"[错误] 无法启动 Claude CLI: {ex.Message}";
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_config.ResponseTimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[超时] Claude CLI 在 {_config.ResponseTimeoutSeconds}s 内未响应，已终止");
            Console.ResetColor();

            try { process.Kill(entireProcessTree: true); } catch { }

            return $"[错误] Claude CLI 响应超时 ({_config.ResponseTimeoutSeconds}s)";
        }

        var output = outputBuilder.ToString().TrimEnd('\n', '\r');
        var error = errorBuilder.ToString().Trim();

        if (process.ExitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[错误] Claude CLI 退出码: {process.ExitCode}");
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($"  stderr: {error}");
            Console.ResetColor();

            return $"[错误] Claude CLI 执行失败 (退出码 {process.ExitCode}): {error}";
        }

        return output;
    }

    private string BuildArguments(string message, string sessionId, bool isFirstMessage)
    {
        var args = new StringBuilder();

        args.Append($"-p {EscapeArgument(message)}");

        if (isFirstMessage)
        {
            args.Append($" --session-id {sessionId}");

            if (!string.IsNullOrWhiteSpace(_config.Model))
                args.Append($" --model {EscapeArgument(_config.Model)}");

            if (!string.IsNullOrWhiteSpace(_config.SystemPrompt))
                args.Append($" --system-prompt {EscapeArgument(_config.SystemPrompt)}");

            if (!string.IsNullOrWhiteSpace(_config.AllowedTools))
                args.Append($" --allowedTools {EscapeArgument(_config.AllowedTools)}");
        }
        else
        {
            args.Append($" --resume {sessionId}");
        }

        args.Append($" --max-budget-usd {_config.MaxBudgetUsd}");
        args.Append(" --output-format text");

        return args.ToString();
    }

    private static string EscapeArgument(string arg)
    {
        if (OperatingSystem.IsWindows())
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        else
            return $"'{arg.Replace("'", "'\\''")}'";
    }

    public void ClearSession(Guid conversationId)
    {
        _conversationSessions.TryRemove(conversationId, out _);
        _ = SaveSessionsAsync();
    }
}
