using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConventionMobile.Helpers
{
    public enum NotificationLevel
    {
        Low = 0,
        Medium,
        High,
        Critical
    }

    public class Notification
    {
        public string Message { get; set; }
        public int ShowTimeInMilliseconds { get; set; } 
        public NotificationLevel Level { get; set; } 
    }

    public static class NotificationBox
    {
        private static readonly Queue<Notification> NotificationQueue = new Queue<Notification>();
        //private static readonly Timer Timer = new Timer(async e => await ProcessNotificationsAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
        private static readonly object LockObject = new object();
        private static bool _currentlyProcessing = false;

        public static async Task AddNotificationAsync(string message, NotificationLevel level = NotificationLevel.Low,
            int timeoutInMilliseconds = 1000)
        {
            await AddNotificationAsync(new Notification
            {
                Message = message,
                Level = level,
                ShowTimeInMilliseconds = timeoutInMilliseconds
            });
        }

        public static async Task AddNotificationAsync(Notification notification)
        {
            NotificationQueue.Enqueue(notification);
            await ProcessNotificationsAsync();
        }
        
        public static async Task ProcessNotificationsAsync()
        {
            if (!_currentlyProcessing)
            {
                lock (LockObject)
                {
                    _currentlyProcessing = true;
                }

                var count = NotificationQueue.Count;
                while (count > 0)
                {
                    var notification = NotificationQueue.Dequeue();
                    await ProcessNotificationAsync(notification);
                    count = NotificationQueue.Count;
                }

                lock (LockObject)
                {
                    _currentlyProcessing = false;
                }
            }
        }

        private static async Task ProcessNotificationAsync(Notification notification)
        {
            await Task.Run(async () =>
            {
                GlobalVars.ToastType type;

                switch (notification.Level)
                {
                    case NotificationLevel.Low:
                    case NotificationLevel.Medium:
                        type = GlobalVars.ToastType.Green;
                        break;
                    case NotificationLevel.High:
                        type = GlobalVars.ToastType.Yellow;
                        break;
                    case NotificationLevel.Critical:
                        type = GlobalVars.ToastType.Red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                GlobalVars.DoToast(notification.Message, type, notification.ShowTimeInMilliseconds);
                await Task.Delay(notification.ShowTimeInMilliseconds);
            });
        }
    }
}
