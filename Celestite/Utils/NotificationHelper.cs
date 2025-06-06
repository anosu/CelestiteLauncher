﻿using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Celestite.I18N;

namespace Celestite.Utils
{
    public class NotificationHelper
    {
        private static INotificationManager? _notificationManager;

        public static void Init()
        {
            if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime t) return;
            var notification = Dispatcher.UIThread.Invoke(() => new WindowNotificationManager(t.MainWindow)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 10
            });
            _notificationManager = notification;
        }

        private static void EnsureNotification()
        {
            if (LaunchHelper.IsInGuiMode() && _notificationManager == null)
                Init();
        }

        public static void Info(string message)
        {
            EnsureNotification();
            if (_notificationManager != null)
                Dispatcher.UIThread.Invoke(() =>
                _notificationManager!.Show(new Notification(Localization.InfoBarDefault, message)));
            else
                Console.WriteLine(message);
        }

        public static void Error(string message)
        {
            WindowTrayHelper.RequestShow();
            EnsureNotification();
            if (_notificationManager != null)
                Dispatcher.UIThread.Invoke(() =>
                    _notificationManager!.Show(new Notification(Localization.InfoBarError, message, NotificationType.Error)));
            else
                Console.WriteLine(message);
        }

        public static void Warn(string message)
        {
            WindowTrayHelper.RequestShow();
            EnsureNotification();
            if (_notificationManager != null)
                Dispatcher.UIThread.Invoke(() =>
                _notificationManager!.Show(new Notification(Localization.InfoBarDefault, message, NotificationType.Warning)));
            else
                Console.WriteLine(message);
        }

        public static void Warn(string message, Action clickAction)
        {
            WindowTrayHelper.RequestShow();
            EnsureNotification();
            if (_notificationManager != null)
                Dispatcher.UIThread.Invoke(() =>
                _notificationManager!.Show(new Notification(Localization.InfoBarDefault, message, NotificationType.Warning, onClick: clickAction)));
            else
                Console.WriteLine(message);
        }

        public static void Success(string message)
        {
            WindowTrayHelper.RequestShow();
            EnsureNotification();
            if (_notificationManager != null)
                Dispatcher.UIThread.Invoke(() =>
                _notificationManager!.Show(new Notification(Localization.InfoBarDefault, message, NotificationType.Success)));
            else
                Console.WriteLine(message);
        }
    }
}
