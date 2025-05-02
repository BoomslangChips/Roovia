namespace Roovia.Models.Helper
{
    public class ResponseModel
    {
        public object? Response { get; set; }
        public ResponseInfo ResponseInfo { get; set; } = new ResponseInfo();


    }

    public class ResponseInfo
    {
        public string? Message { get; set; }
        public bool Success { get; set; }

    }
}
