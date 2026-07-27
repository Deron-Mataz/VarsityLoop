namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// Generic operation result wrapper so services never need to throw exceptions
    /// for expected failure paths (validation errors, not-found, permission denied, etc).
    /// Controllers translate this into the appropriate view/response.
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public List<string> ValidationErrors { get; private set; } = new();

        public static OperationResult Ok() => new() { Success = true };

        public static OperationResult Fail(string errorMessage) => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };

        public static OperationResult FailValidation(IEnumerable<string> errors) => new()
        {
            Success = false,
            ValidationErrors = errors.ToList(),
            ErrorMessage = "Validation failed."
        };
    }

    /// <summary>
    /// Generic operation result carrying a payload of type T.
    /// </summary>
    public class OperationResult<T>
    {
        public bool Success { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public List<string> ValidationErrors { get; private set; } = new();

        public static OperationResult<T> Ok(T data) => new()
        {
            Success = true,
            Data = data
        };

        public static OperationResult<T> Fail(string errorMessage) => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };

        public static OperationResult<T> FailValidation(IEnumerable<string> errors) => new()
        {
            Success = false,
            ValidationErrors = errors.ToList(),
            ErrorMessage = "Validation failed."
        };
    }

    /// <summary>
    /// Standard paged result for any list query (listings, users, categories, etc).
    /// Firestore pagination is cursor-based, so PageToken/NextPageToken are used
    /// instead of raw offset/skip to keep queries efficient at scale.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public string? NextPageToken { get; set; }
        public bool HasMore { get; set; }
        public int PageSize { get; set; }
    }
}
