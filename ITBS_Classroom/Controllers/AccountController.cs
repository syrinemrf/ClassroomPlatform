using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ITBS_Classroom.Controllers;

public class AccountController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AccountController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _env = env;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Compte verrouille temporairement."
            : "Email ou mot de passe invalide.");
        return View(model);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Mot de passe mis a jour.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Student), HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return View(new ManageAccountViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CurrentProfileImagePath = user.ProfileImagePath
        });
    }

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Student),
     HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ManageAccountViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        if (model.ProfileImage is not null)
        {
            var ext = Path.GetExtension(model.ProfileImage.FileName);
            if (!AllowedImageExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Format image non autorise.");
                model.CurrentProfileImagePath = user.ProfileImagePath;
                return View(model);
            }
            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Image trop volumineuse (max 2 MB).");
                model.CurrentProfileImagePath = user.ProfileImagePath;
                return View(model);
            }
            user.ProfileImagePath = await SaveProfileImageAsync(model.ProfileImage, user.Id, ct);
        }
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Profil mis a jour.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<string> SaveProfileImageAsync(IFormFile image, string userId, CancellationToken ct)
    {
        var ext = Path.GetExtension(image.FileName);
        var fileName = Guid.NewGuid().ToString() + ext;
        var web = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var folder = Path.Combine(web, "uploads", "profiles", userId);
        Directory.CreateDirectory(folder);
        foreach (var old in Directory.GetFiles(folder)) System.IO.File.Delete(old);
        var full = Path.Combine(folder, fileName);
        await using var s = new FileStream(full, FileMode.Create);
        await image.CopyToAsync(s, ct);
        return Path.Combine("uploads", "profiles", userId, fileName).Replace("\\", "/");
    }
}
