public interface IFilePathService
{
    string GetJsonFilePath(string f);
}

public class FilePathService : IFilePathService
{
    private readonly IWebHostEnvironment _env;

    public FilePathService(IWebHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public string GetJsonFilePath(string filename)
    {
        string basePath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string filePath = Path.Combine(basePath, "textdb", filename);

        // Erstellen des Ordners, falls er nicht existiert
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // Erstellen der Datei, falls sie nicht existiert
        if (!System.IO.File.Exists(filePath))
        {
            System.IO.File.WriteAllText(filePath, "[]"); // Leere JSON-Liste als Startwert
        }

        return filePath;
    }
}
