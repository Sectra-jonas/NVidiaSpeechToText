using System;

namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Provider information for displaying in UI and logging
    /// </summary>
    public class TranscriptionProviderInfo
    {
        public TranscriptionProvider Provider { get; set; }
        public string ProviderName { get; set; } = "";
        public string Version { get; set; } = "";
        public string Status { get; set; } = "";
        public bool RequiresNetwork { get; set; }
        public bool RequiresCredentials { get; set; }
        public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
        public string Description { get; set; } = "";
    }
}
