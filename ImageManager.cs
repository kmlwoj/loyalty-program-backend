namespace lojalBackend
{
    public class ImageManager
    {
        private static readonly string currentDirectory = string.Concat(Directory.GetCurrentDirectory(), "/Images/");
        public static bool CheckFileExtension(IFormFile file) => 
               file.ContentType.Equals("image/png") 
            || file.ContentType.Equals("image/jpeg") 
            || file.ContentType.Equals("image/gif");
        public static bool CheckFileSize(IFormFile file) => file.Length < 4000000;
        public static bool CheckFileExistence(string fileName) => Directory.GetFiles(currentDirectory, string.Concat(fileName, ".*")).Length > 0;
        public static string[] GetFileNames(string fileName) => Directory.GetFiles(currentDirectory, string.Concat(fileName, ".*"));
        public static string MakePath(string fileName) => string.Concat(currentDirectory, fileName);
        public static async Task<string> SaveFile(IFormFile file, string fileName)
        {
            string fileExtension = file.ContentType[6..];
            try
            {
                if (!CheckFileExistence(fileName))
                {
                    string path = MakePath(string.Concat(fileName, ".", fileExtension));
                    using (Stream stream = File.Create(path))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
            return string.Concat(fileName, ".", fileExtension);
        }
        public static bool DeleteFile(string fileName)
        {
            string[] file = GetFileNames(fileName);
            if (file.Length > 0)
            {
                File.Delete(MakePath(file.First()));
                return true;
            }
            return false;
        }
        public static FileStream GetFile(string fileName)
        {
            string[] file = GetFileNames(fileName);
            if (file.Length > 0)
            {
                return File.OpenRead(MakePath(file.First()));
            }
            throw new Exception("File does not exist!");
        }
    }
}
