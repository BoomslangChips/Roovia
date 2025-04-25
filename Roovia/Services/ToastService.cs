using System;
using System.Collections.Generic;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Roovia.Services
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Message { get; set; }
        public ToastType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int DurationSeconds { get; set; } = 5; // Default to 5 seconds
        public string Icon { get; set; }
        public bool AutoHide { get; set; } = true;
        public bool ShowProgress { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        
        // Return the appropriate icon based on toast type
        public string GetIcon()
        {
            if (!string.IsNullOrEmpty(Icon))
                return Icon;
            
            return Type switch
            {
                ToastType.Success => "far fa-check-circle",
                ToastType.Error => "far fa-times-circle",
                ToastType.Warning => "far fa-exclamation-triangle",
                ToastType.Info => "far fa-info-circle",
                _ => "far fa-bell"
            };
        }
        
        // Return the appropriate CSS class based on toast type
        public string GetCssClass()
        {
            return Type switch
            {
                ToastType.Success => "roovia-toast-success",
                ToastType.Error => "roovia-toast-error",
                ToastType.Warning => "roovia-toast-warning",
                ToastType.Info => "roovia-toast-info",
                _ => ""
            };
        }
    }

    public class ToastService : IDisposable
    {
        public event Action<ToastMessage> OnShow;
        public event Action<Guid> OnHide;
        public event Action OnClearAll;
        
        private Dictionary<Guid, System.Timers.Timer> _timers = new Dictionary<Guid, Timer>();
        
        // Show a toast with auto-generated title based on type
        public void Show(string message, ToastType type = ToastType.Info, int durationSeconds = 5, bool autoHide = true)
        {
            string title = type switch
            {
                ToastType.Success => "Success",
                ToastType.Error => "Error",
                ToastType.Warning => "Warning",
                ToastType.Info => "Information",
                _ => "Notification"
            };
            
            ShowToast(title, message, type, durationSeconds, autoHide);
        }
        
        // Show a success toast
        public void ShowSuccess(string message, string title = "Success", int durationSeconds = 5)
        {
            ShowToast(title, message, ToastType.Success, durationSeconds);
        }
        
        // Show an error toast
        public void ShowError(string message, string title = "Error", int durationSeconds = 8)
        {
            ShowToast(title, message, ToastType.Error, durationSeconds);
        }
        
        // Show a warning toast
        public void ShowWarning(string message, string title = "Warning", int durationSeconds = 6)
        {
            ShowToast(title, message, ToastType.Warning, durationSeconds);
        }
        
        // Show an info toast
        public void ShowInfo(string message, string title = "Information", int durationSeconds = 5)
        {
            ShowToast(title, message, ToastType.Info, durationSeconds);
        }
        
        // Show a custom toast
        public void ShowToast(string title, string message, ToastType type, int durationSeconds = 5, bool autoHide = true, bool showProgress = true, string customIcon = null)
        {
            var toast = new ToastMessage
            {
                Title = title,
                Message = message,
                Type = type,
                DurationSeconds = durationSeconds,
                AutoHide = autoHide,
                ShowProgress = showProgress,
                Icon = customIcon
            };
            
            OnShow?.Invoke(toast);
            
            if (autoHide && durationSeconds > 0)
            {
                StartTimer(toast.Id, durationSeconds);
            }
        }
        
        // Hide a specific toast
        public void HideToast(Guid id)
        {
            OnHide?.Invoke(id);
            
            if (_timers.ContainsKey(id))
            {
                StopTimer(id);
            }
        }
        
        // Clear all toasts
        public void ClearAll()
        {
            OnClearAll?.Invoke();
            
            foreach (var id in _timers.Keys)
            {
                StopTimer(id);
            }
            
            _timers.Clear();
        }
        
        // Start a timer for auto-hiding
        private void StartTimer(Guid id, int durationSeconds)
        {
            var timer = new Timer(durationSeconds * 1000);
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => OnTimerElapsed(id);
            timer.Start();
            
            _timers[id] = timer;
        }
        
        // Stop a specific timer
        private void StopTimer(Guid id)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _timers.Remove(id);
            }
        }
        
        // Timer elapsed event handler
        private void OnTimerElapsed(Guid id)
        {
            HideToast(id);
        }
        
        // Dispose method to clean up timers
        public void Dispose()
        {
            foreach (var timer in _timers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            
            _timers.Clear();
        }
    }
}