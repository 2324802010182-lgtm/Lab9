#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ASC.Model.BaseTypes;
using ASC.Web.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ASC.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Chuyển hướng sang Google để xác thực
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Nếu tài khoản đã từng liên kết Google trước đó thì đăng nhập luôn
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.",
                    info.Principal.Identity?.Name, info.LoginProvider);

                return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // Nếu chưa có tài khoản, hiện form xác nhận tạo tài khoản
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;

            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                Input = new InputModel
                {
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                ReturnUrl = returnUrl;
                return Page();
            }

            // Lấy thông tin trả về từ Google
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                var roles = await _userManager.GetRolesAsync(existingUser);

                // Theo yêu cầu lab: nếu email thuộc Admin hoặc Engineer thì báo lỗi
                if (roles.Contains("Admin") || roles.Contains("Engineer"))
                {
                    ModelState.AddModelError(string.Empty, "Email này đã được sử dụng cho tài khoản Admin hoặc Engineer.");
                    ProviderDisplayName = info.ProviderDisplayName;
                    ReturnUrl = returnUrl;
                    return Page();
                }

                // Nếu đã có user thường thì cũng không tạo lại
                ModelState.AddModelError(string.Empty, "Email này đã tồn tại trong hệ thống.");
                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }

            // Tạo tài khoản customer / user
            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }

            // Thêm claim cơ bản
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, user.Email));
            await _userManager.AddClaimAsync(user, new Claim("IsActive", "True"));

            // Gán role User
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.User.ToString());
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }

            // Liên kết tài khoản nội bộ với Google login
            result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ProviderDisplayName = info.ProviderDisplayName;
                ReturnUrl = returnUrl;
                return Page();
            }

            // Đăng nhập luôn sau khi tạo thành công
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created an account using {LoginProvider} provider.", info.LoginProvider);

            return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
        }
    }
}