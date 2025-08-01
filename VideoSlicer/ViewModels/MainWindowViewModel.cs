using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace VideoSlicer.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly Timer _positionTimer;
    private bool _isUserSeeking;

    [ObservableProperty]
    private MediaPlayer? _mediaPlayer;

    [ObservableProperty]
    private bool _isVideoLoaded;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _position;

    [ObservableProperty]
    private int _volume = 50;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private string _currentTime = "00:00";

    [ObservableProperty]
    private string _totalTime = "00:00";

    public string PlayButtonIcon => IsPlaying 
        ? "M8,5.14V19.14L19,12.14L8,5.14Z" // Play icon
        : "M14,19H18V5H14M6,19H10V5H6V19Z"; // Pause icon

    public string VolumeIcon => IsMuted || Volume == 0
        ? "M12,4L9.91,6.09L12,8.18M4.27,3L3,4.27L7.73,9H3V15H7L12,20V13.27L16.25,17.52C15.58,18.04 14.83,18.46 14,18.7V20.77C15.38,20.45 16.63,19.82 17.68,18.96L19.73,21L21,19.73L12,10.73M19,12C19,12.94 18.8,13.82 18.46,14.64L19.97,16.15C20.62,14.91 21,13.5 21,12C21,7.72 18,4.14 14,3.23V5.29C16.89,6.15 19,8.83 19,12M16.5,12C16.5,10.23 15.5,8.71 14,7.97V10.18L16.45,12.63C16.5,12.43 16.5,12.21 16.5,12Z"
        : Volume < 50
            ? "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.85 14,18.71V20.77C18.01,19.86 21,16.28 21,12C21,7.72 18.01,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16.03C15.5,15.29 16.5,13.77 16.5,12M3,9V15H7L12,20V4L7,9H3Z"
            : "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.85 14,18.71V20.77C18.01,19.86 21,16.28 21,12C21,7.72 18.01,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16.03C15.5,15.29 16.5,13.77 16.5,12M3,9V15H7L12,20V4L7,9H3Z";

    public MainWindowViewModel()
    {
        _libVLC = new LibVLC();
        MediaPlayer = new MediaPlayer(_libVLC);
        
        _positionTimer = new Timer(1000);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _positionTimer.Start();

        MediaPlayer.Playing += OnMediaPlayerPlaying;
        MediaPlayer.Paused += OnMediaPlayerPaused;
        MediaPlayer.Stopped += OnMediaPlayerStopped;
        MediaPlayer.LengthChanged += OnLengthChanged;
        MediaPlayer.TimeChanged += OnTimeChanged;

        Volume = 50;
        MediaPlayer.Volume = Volume;
    }

    partial void OnVolumeChanged(int value)
    {
        if (MediaPlayer != null)
        {
            MediaPlayer.Volume = value;
            OnPropertyChanged(nameof(VolumeIcon));
        }
    }

    partial void OnPositionChanged(double value)
    {
        if (MediaPlayer != null && !_isUserSeeking && MediaPlayer.Length > 0)
        {
            _isUserSeeking = true;
            MediaPlayer.Position = (float)value;
            _isUserSeeking = false;
        }
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow : null);

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Video File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video Files")
                {
                    Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm", "*.m4v", "*.3gp" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var media = new Media(_libVLC, file.Path.LocalPath, FromType.FromPath);
            MediaPlayer?.Play(media);
            IsVideoLoaded = true;
        }
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (MediaPlayer == null || !IsVideoLoaded) return;

        if (MediaPlayer.IsPlaying)
        {
            MediaPlayer.Pause();
        }
        else
        {
            MediaPlayer.Play();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        MediaPlayer?.Stop();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        if (MediaPlayer != null)
        {
            IsMuted = !IsMuted;
            MediaPlayer.Mute = IsMuted;
            OnPropertyChanged(nameof(VolumeIcon));
        }
    }

    [RelayCommand]
    private void ToggleFullScreen()
    {
        // Implementation would depend on the specific window management needs
        // This is a placeholder for full screen functionality
    }

    [RelayCommand]
    private void ExitFullScreen()
    {
        // Implementation for exiting full screen
    }

    [RelayCommand]
    private void Exit()
    {
        //Application.Current?.ApplicationLifetime?.TryShutdown();
    }

    private void OnMediaPlayerPlaying(object? sender, EventArgs e)
    {
        IsPlaying = true;
        OnPropertyChanged(nameof(PlayButtonIcon));
    }

    private void OnMediaPlayerPaused(object? sender, EventArgs e)
    {
        IsPlaying = false;
        OnPropertyChanged(nameof(PlayButtonIcon));
    }

    private void OnMediaPlayerStopped(object? sender, EventArgs e)
    {
        IsPlaying = false;
        Position = 0;
        OnPropertyChanged(nameof(PlayButtonIcon));
    }

    private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        TotalTime = TimeSpan.FromMilliseconds(e.Length).ToString(@"mm\:ss");
    }

    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        CurrentTime = TimeSpan.FromMilliseconds(e.Time).ToString(@"mm\:ss");
    }

    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (MediaPlayer != null && !_isUserSeeking && MediaPlayer.Length > 0)
        {
            Position = MediaPlayer.Position;
        }
    }

    public void Dispose()
    {
        _positionTimer?.Dispose();
        MediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }
}