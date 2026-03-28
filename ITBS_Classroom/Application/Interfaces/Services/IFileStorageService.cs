using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<(string RelativePath, string StoredFileName)> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    bool IsAllowedExtension(string fileName);
}
