using ASC.Model.BaseTypes;
using ASC.Web.Areas.Accounts.Models;
using ASC.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASC.Web.Areas.Accounts.Controllers
{
    [Authorize]
    [Area("Accounts")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ServiceEngineers()
        {
            var serviceEngineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString());

            HttpContext.Session.SetSession("ServiceEngineers", serviceEngineers);

            return View(new ServiceEngineerViewModel
            {
                ServiceEngineers = serviceEngineers == null ? null : serviceEngineers.ToList(),
                Registration = new ServiceEngineerRegistrationViewModel()
                {
                    IsEdit = false,
                    IsActive = true
                }
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceEngineers(ServiceEngineerViewModel serviceEngineer)
        {
            serviceEngineer.ServiceEngineers =
                HttpContext.Session.GetSession<List<IdentityUser>>("ServiceEngineers");

            if (!ModelState.IsValid)
            {
                return View(serviceEngineer);
            }

            if (serviceEngineer.Registration.IsEdit)
            {
                var user = await _userManager.FindByEmailAsync(serviceEngineer.Registration.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Service Engineer not found.");
                    return View(serviceEngineer);
                }

                user.UserName = serviceEngineer.Registration.UserName;

                IdentityResult result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult passwordResult = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    serviceEngineer.Registration.Password);

                if (!passwordResult.Succeeded)
                {
                    passwordResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                user = await _userManager.FindByEmailAsync(serviceEngineer.Registration.Email);
                var identity = await _userManager.GetClaimsAsync(user);
                var isActiveClaim = identity.SingleOrDefault(p => p.Type == "IsActive");

                if (isActiveClaim != null)
                {
                    var removeClaimResult = await _userManager.RemoveClaimAsync(
                        user,
                        new System.Security.Claims.Claim(isActiveClaim.Type, isActiveClaim.Value));

                    if (!removeClaimResult.Succeeded)
                    {
                        removeClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                        return View(serviceEngineer);
                    }
                }

                var addClaimResult = await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim("IsActive", serviceEngineer.Registration.IsActive.ToString()));

                if (!addClaimResult.Succeeded)
                {
                    addClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }
            }
            else
            {
                IdentityUser user = new IdentityUser
                {
                    UserName = serviceEngineer.Registration.UserName,
                    Email = serviceEngineer.Registration.Email,
                    EmailConfirmed = true
                };

                IdentityResult result = await _userManager.CreateAsync(user, serviceEngineer.Registration.Password);

                await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim(
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
                        serviceEngineer.Registration.Email));

                await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim("IsActive", serviceEngineer.Registration.IsActive.ToString()));

                if (!result.Succeeded)
                {
                    result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
                if (!roleResult.Succeeded)
                {
                    roleResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(serviceEngineer);
                }
            }

            if (serviceEngineer.Registration.IsActive)
            {
                await _emailSender.SendEmailAsync(
                    serviceEngineer.Registration.Email,
                    "Account Created/Modified",
                    $"Email : {serviceEngineer.Registration.Email} <br /> Password : {serviceEngineer.Registration.Password}");
            }
            else
            {
                await _emailSender.SendEmailAsync(
                    serviceEngineer.Registration.Email,
                    "Account Deactivated",
                    "Your account has been deactivated.");
            }

            return RedirectToAction("ServiceEngineers");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ProfileModel()
            {
                UserName = user.UserName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileModel profile)
        {
            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Không tìm thấy email người dùng hiện tại.");
                return View(profile);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản người dùng.");
                return View(profile);
            }

            user.UserName = profile.UserName;

            IdentityResult result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                return View(profile);
            }

            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var customers = await _userManager.GetUsersInRoleAsync(Roles.User.ToString());

            // Hold all customers in session
            HttpContext.Session.SetSession("Customers", customers);

            return View(new CustomerViewModel
            {
                Customers = customers == null ? null : customers.ToList(),
                Registration = new CustomerRegistrationViewModel()
                {
                    IsEdit = false
                }
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Customers(CustomerViewModel customer)
        {
            customer.Customers = HttpContext.Session.GetSession<List<IdentityUser>>("Customers");

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            if (customer.Registration.IsEdit)
            {
                var user = await _userManager.FindByEmailAsync(customer.Registration.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Customer not found.");
                    return View(customer);
                }

                var identity = await _userManager.GetClaimsAsync(user);
                var isActiveClaim = identity.SingleOrDefault(p => p.Type == "IsActive");

                if (isActiveClaim != null)
                {
                    var removeClaimResult = await _userManager.RemoveClaimAsync(
                        user,
                        new System.Security.Claims.Claim(isActiveClaim.Type, isActiveClaim.Value));

                    if (!removeClaimResult.Succeeded)
                    {
                        removeClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                        return View(customer);
                    }
                }

                var addClaimResult = await _userManager.AddClaimAsync(
                    user,
                    new System.Security.Claims.Claim("IsActive", customer.Registration.IsActive.ToString()));

                if (!addClaimResult.Succeeded)
                {
                    addClaimResult.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                    return View(customer);
                }
            }

            if (customer.Registration.IsActive)
            {
                await _emailSender.SendEmailAsync(
                    customer.Registration.Email,
                    "Account Modified",
                    $"Your account has been activated, Email : {customer.Registration.Email}");
            }
            else
            {
                await _emailSender.SendEmailAsync(
                    customer.Registration.Email,
                    "Account Deactivated",
                    "Your account has been deactivated.");
            }

            return RedirectToAction("Customers");
        }
    }
}