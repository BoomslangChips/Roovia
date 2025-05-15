namespace Roovia.Models.Helper
{
    public static class FileNameExtensions
    {
        public static string InsertBeforeExtension(this string fileName, string insert)
        {
            var lastDot = fileName.LastIndexOf('.');
            if (lastDot < 0)
                return fileName + insert;

            return fileName.Substring(0, lastDot) + insert + fileName.Substring(lastDot);
        }
    }
}
