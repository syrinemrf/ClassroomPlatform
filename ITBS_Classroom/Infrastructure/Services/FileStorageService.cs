using ITBS_Classroom.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".ppt",
        ".pptx"
    };

    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension);
    }

    public async Task<(string RelativePath, string StoredFileName)> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (!IsAllowedExtension(file.FileName))
        {
            throw new InvalidOperationException("File type not allowed.");
        }

        var extension = Path.GetExtension(file.FileName);
        var generatedName = $"{Guid.NewGuid()}{extension}";
        var root = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullFolder = Path.Combine(root, "uploads", folder);
        Directory.CreateDirectory(fullFolder);

        var fullPath = Path.Combine(fullFolder, generatedName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        var relativePath = Path.Combine("uploads", folder, generatedName).Replace("\\", "/");
        return (relativePath, generatedName);
    }
}
