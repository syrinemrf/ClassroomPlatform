using ITBS_Classroom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip"
    };

    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env) => _env = env;

    public bool IsAllowedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName));

    public async Task<(string RelativePath, string StoredFileName)> SaveAsync(
        IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (!IsAllowedExtension(file.FileName))
            throw new InvalidOperationException("Type de fichier non autorisé.");

        var ext = Path.GetExtension(file.FileName);
        var generated = $"{Guid.NewGuid()}{ext}";
        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullFolder = Path.Combine(root, "uploads", folder);
        Directory.CreateDirectory(fullFolder);

        var fullPath = Path.Combine(fullFolder, generated);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        var relative = Path.Combine("uploads", folder, generated).Replace("\\", "/");
        return (relative, generated);
    }
}
