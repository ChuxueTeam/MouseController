namespace MouseController;

public enum ClickButton
{
    Left = 0,
    Middle = 1,
    Right = 2,
}

/// <summary>连点器：后台异步循环模拟点击，达到次数自动停止。</summary>
public sealed class AutoClicker
{
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts != null;

    /// <summary>每次点击进度（1 起的当前次数）。</summary>
    public event Action<int>? Progress;

    /// <summary>自然完成或被停止后触发。</summary>
    public event Action? Finished;

    public async Task RunAsync(ClickButton button, int count, int intervalMs)
    {
        if (_cts != null) return;

        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        try
        {
            for (int i = 1; i <= count; i++)
            {
                token.ThrowIfCancellationRequested();

                (uint down, uint up) = button switch
                {
                    ClickButton.Left => (Native.MOUSEEVENTF_LEFTDOWN, Native.MOUSEEVENTF_LEFTUP),
                    ClickButton.Middle => (Native.MOUSEEVENTF_MIDDLEDOWN, Native.MOUSEEVENTF_MIDDLEUP),
                    _ => (Native.MOUSEEVENTF_RIGHTDOWN, Native.MOUSEEVENTF_RIGHTUP),
                };
                Native.ClickMouse(down, up);

                Progress?.Invoke(i);

                if (i < count)
                    await Task.Delay(intervalMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // 被停止
        }
        finally
        {
            _cts = null;
            cts.Dispose();
            Finished?.Invoke();
        }
    }

    public void Stop() => _cts?.Cancel();
}
