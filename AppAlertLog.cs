using System;
using System.Collections.Generic;
using System.Linq;

namespace WTF
{
    public enum AppAlertSeverity
    {
        Information,
        Warning,
        Error
    }

    public sealed class AppAlertEntry
    {
        public Guid Id { get; set; }
        public AppAlertSeverity Severity { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsConfirmed { get; set; }

        public string SeverityText
        {
            get
            {
                switch (Severity)
                {
                    case AppAlertSeverity.Information:
                        return LocalizationService.GetText("Common.Information");
                    case AppAlertSeverity.Warning:
                        return LocalizationService.GetText("Common.Warning");
                    case AppAlertSeverity.Error:
                        return LocalizationService.GetText("Common.Error");
                    default:
                        return Severity.ToString();
                }
            }
        }

        public string CreatedAtText
        {
            get { return CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"); }
        }

        public string ConfirmedText
        {
            get { return IsConfirmed ? LocalizationService.GetText("Common.Yes") : LocalizationService.GetText("Common.No"); }
        }
    }

    public static class AppAlertLog
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<AppAlertEntry> Entries = new List<AppAlertEntry>();

        public static event EventHandler Changed;

        public static void AddInformation(string category, string message)
        {
            Add(AppAlertSeverity.Information, category, message, null);
        }

        public static void AddInformation(string category, string message, string details)
        {
            Add(AppAlertSeverity.Information, category, message, details);
        }

        public static void AddWarning(string category, string message)
        {
            Add(AppAlertSeverity.Warning, category, message, null);
        }

        public static void AddWarning(string category, string message, string details)
        {
            Add(AppAlertSeverity.Warning, category, message, details);
        }

        public static void AddError(string category, string message)
        {
            Add(AppAlertSeverity.Error, category, message, null);
        }

        public static void AddError(string category, string message, string details)
        {
            Add(AppAlertSeverity.Error, category, message, details);
        }

        public static void Add(AppAlertSeverity severity, string category, string message)
        {
            Add(severity, category, message, null);
        }

        public static void Add(AppAlertSeverity severity, string category, string message, string details)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lock (SyncRoot)
            {
                Entries.Add(new AppAlertEntry
                {
                    Id = Guid.NewGuid(),
                    Severity = severity,
                    Category = string.IsNullOrWhiteSpace(category) ? LocalizationService.GetText("Common.General") : category,
                    Message = message,
                    Details = details,
                    CreatedAt = DateTime.Now,
                    IsConfirmed = false
                });
            }

            OnChanged();
        }

        public static List<AppAlertEntry> GetEntries()
        {
            lock (SyncRoot)
            {
                return Entries
                    .OrderByDescending(entry => entry.CreatedAt)
                    .Select(Clone)
                    .ToList();
            }
        }

        public static int GetUnconfirmedCount(AppAlertSeverity severity)
        {
            lock (SyncRoot)
            {
                return Entries.Count(entry => entry.Severity == severity && !entry.IsConfirmed);
            }
        }

        public static void Confirm(IEnumerable<Guid> entryIds)
        {
            if (entryIds == null)
                return;

            HashSet<Guid> ids = new HashSet<Guid>(entryIds);

            lock (SyncRoot)
            {
                foreach (AppAlertEntry entry in Entries)
                {
                    if (ids.Contains(entry.Id))
                    {
                        entry.IsConfirmed = true;
                    }
                }
            }

            OnChanged();
        }

        public static void Delete(IEnumerable<Guid> entryIds)
        {
            if (entryIds == null)
                return;

            HashSet<Guid> ids = new HashSet<Guid>(entryIds);

            lock (SyncRoot)
            {
                Entries.RemoveAll(entry => ids.Contains(entry.Id));
            }

            OnChanged();
        }

        public static void ConfirmAll()
        {
            lock (SyncRoot)
            {
                foreach (AppAlertEntry entry in Entries)
                {
                    entry.IsConfirmed = true;
                }
            }

            OnChanged();
        }

        public static void DeleteAll()
        {
            lock (SyncRoot)
            {
                Entries.Clear();
            }

            OnChanged();
        }

        private static AppAlertEntry Clone(AppAlertEntry entry)
        {
            return new AppAlertEntry
            {
                Id = entry.Id,
                Severity = entry.Severity,
                Category = entry.Category,
                Message = entry.Message,
                Details = entry.Details,
                CreatedAt = entry.CreatedAt,
                IsConfirmed = entry.IsConfirmed
            };
        }

        private static void OnChanged()
        {
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}
