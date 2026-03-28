using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace ITBS_Classroom.Controllers;

public class AccountController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, IsFrench() ? "Identifiants invalides." : "Invalid credentials.");
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = IsFrench() ? "Mot de passe mis à jour." : "Password updated.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Student}")]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var vm = new ManageAccountViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CurrentProfileImagePath = user.ProfileImagePath
        };

        return View(vm);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Student}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ManageAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;

        if (model.ProfileImage is not null)
        {
            var extension = Path.GetExtension(model.ProfileImage.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProfileImage), IsFrench() ? "Format image non autorisé." : "Image format not allowed.");
                model.CurrentProfileImagePath = user.ProfileImagePath;
                return View(model);
            }

            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), IsFrench() ? "Image trop volumineuse (max 2 MB)." : "Image is too large (max 2 MB).");
                model.CurrentProfileImagePath = user.ProfileImagePath;
                return View(model);
            }

            user.ProfileImagePath = await SaveProfileImageAsync(model.ProfileImage, user.Id, cancellationToken);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.CurrentProfileImagePath = user.ProfileImagePath;
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = IsFrench() ? "Profil mis à jour." : "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static bool IsFrench()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
    }

    private async Task<string> SaveProfileImageAsync(IFormFile image, string userId, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(image.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "profiles", userId);
        Directory.CreateDirectory(folder);

        var oldFiles = Directory.GetFiles(folder);
        foreach (var oldFile in oldFiles)
        {
            System.IO.File.Delete(oldFile);
        }

        var fullPath = Path.Combine(folder, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await image.CopyToAsync(stream, cancellationToken);

        return Path.Combine("uploads", "profiles", userId, fileName).Replace("\\", "/");
    }
}
