namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Validation result for configuration checks
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public ValidationErrorType ErrorType { get; set; } = ValidationErrorType.None;
    }

    public enum ValidationErrorType
    {
        None,
        MissingConfiguration,
        InvalidCredentials,
        NetworkError,
        ModelNotFound,
        UnsupportedFormat,
        Other
    }
}
