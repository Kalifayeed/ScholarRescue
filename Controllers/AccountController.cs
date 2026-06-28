using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Account;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller responsible for user authentication: registration, login, logout,
    /// and access denied handling.
    /// Also handles the extended writer/tutor application submitted during registration.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ScholarRescueDbContext _context;
        private readonly IWriterApplicationService _writerApplicationService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AccountController> _logger;
        private readonly IVerificationService _verificationService;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ScholarRescueDbContext context,
            IWriterApplicationService writerApplicationService,
            IWebHostEnvironment environment,
            ILogger<AccountController> logger,
            IVerificationService verificationService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _writerApplicationService = writerApplicationService;
            _environment = environment;
            _logger = logger;
            _verificationService = verificationService;
            _emailService = emailService;
        }

        /// <summary>
        /// Displays the registration form.
        /// If the user is already authenticated, redirect to their dashboard.
        /// </summary>
        [HttpGet]
        public IActionResult Register(string? userType = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("RedirectToDashboard");
            }
            ViewData["PreselectedUserType"] = userType;
            return View();
        }

        /// <summary>
        /// Handles new user registration. Creates the user, assigns the selected role,
        /// signs them in automatically upon success, and (for writers) captures the
        /// extended writer application.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                // Honor the user-selected account type. Fall back to Client if invalid.
                string requestedType = (viewModel.UserType ?? "Client").Trim();
                string roleToAssign = requestedType == "Writer" ? "Writer" : "Client";

                // Check if the email is already taken — prevents cross-role duplicate registration
                var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty,
                        "An account with this email address already exists. Please log in instead.");
                    return View(viewModel);
                }

                var user = new ApplicationUser
                {
                    UserName = viewModel.Email,
                    Email = viewModel.Email,
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    UserType = roleToAssign,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, viewModel.Password);

                if (result.Succeeded)
                {
                    // Ensure the role exists before assigning
                    if (!await _roleManager.RoleExistsAsync(roleToAssign))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
                    }

                    await _userManager.AddToRoleAsync(user, roleToAssign);

                    // For writer registrations, create the application and DO NOT auto-assign full
                    // writer privileges - the user must be approved by an administrator.
                    bool isWriter = roleToAssign == "Writer";
                    if (isWriter)
                    {
                        // Server-side validation: max 5 specializations
                        if (!string.IsNullOrWhiteSpace(viewModel.SelectedSpecializations))
                        {
                            var specializations = viewModel.SelectedSpecializations
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();
                            if (specializations.Count > 5)
                            {
                                ModelState.AddModelError(nameof(viewModel.SelectedSpecializations),
                                    "Choose up to 5 specializations.");
                                await _userManager.DeleteAsync(user);
                                return View(viewModel);
                            }
                            viewModel.Specialization = string.Join(", ", specializations);
                        }

                        try
                        {
                            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
                            await _writerApplicationService.CreateWriterApplicationAsync(
                                user, viewModel, uploadsRoot);

                            // Log audit
                            _context.AuditLogs.Add(new AuditLog
                            {
                                Action = "Writer Application Submitted",
                                PerformedById = user.Id,
                                TargetUserId = user.Id,
                                Description = $"Writer application submitted by {user.Email}.",
                                CreatedDate = DateTime.UtcNow
                            });
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create writer application for {Email}.", user.Email);
                            ModelState.AddModelError(string.Empty, ex.Message);

                            // Roll back the user since the application couldn't be persisted
                            await _userManager.DeleteAsync(user);
                            return View(viewModel);
                        }
                    }

                    _logger.LogInformation(
                        "User {Email} registered successfully with role {Role}.",
                        viewModel.Email, roleToAssign);

                    // Sign the user in automatically after registration
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    if (isWriter)
                    {
                        TempData["SuccessMessage"] =
                            $"Welcome, {user.FirstName}! Your writer application has been submitted. We'll review your details and get back to you shortly.";
                        return RedirectToAction("Dashboard", "Writers");
                    }

                    TempData["SuccessMessage"] =
                        $"Welcome, {user.FirstName}! Your account has been created successfully.";
                    return RedirectToAction("Index", "Orders");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for {Email}.", viewModel.Email);
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred during registration. Please try again.");
                return View(viewModel);
            }
        }

        /// <summary>
        /// Displays the login form.
        /// If the user is already authenticated, redirect to their dashboard.
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("RedirectToDashboard");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Handles user login authentication.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                // Attempt to sign in with the provided credentials
                var result = await _signInManager.PasswordSignInAsync(
                    viewModel.Email,
                    viewModel.Password,
                    viewModel.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(viewModel.Email);
                    _logger.LogInformation("User {Email} logged in successfully.", viewModel.Email);

                    TempData["SuccessMessage"] = "You have successfully logged in.";

                    // Redirect to the original requested URL if valid, otherwise to role-appropriate dashboard
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("RedirectToDashboard");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account is locked out.", viewModel.Email);
                    ModelState.AddModelError(string.Empty,
                        "Your account has been locked out due to multiple failed attempts. Please try again later.");
                    return View(viewModel);
                }

                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty,
                        "You have not confirmed your email. Please check your inbox.");
                    return View(viewModel);
                }

                // Generic failure message for security (don't reveal if user exists or password is wrong)
                ModelState.AddModelError(string.Empty,
                    "Invalid login attempt. Please check your email and password.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for {Email}.", viewModel.Email);
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred during login. Please try again.");
                return View(viewModel);
            }
        }

        /// <summary>
        /// Handles user logout. Signs the user out and redirects to the home page.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                string? userName = User.Identity?.Name;
                await _signInManager.SignOutAsync();
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                _logger.LogInformation("User {UserName} logged out successfully.", userName ?? "Unknown");

                TempData["SuccessMessage"] = "You have been logged out successfully.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout.");
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Redirects authenticated users to the appropriate dashboard based on their role.
        /// </summary>
        [HttpGet]
        public IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Administrator"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            if (User.IsInRole("Writer"))
            {
                return RedirectToAction("Dashboard", "Writers");
            }
            // Default to client dashboard
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ════════════════════════════════════════════════
        // PASSWORD RESET
        // ════════════════════════════════════════════════

        /// <summary>
        /// GET: /Account/ForgotPassword
        /// Displays the forgot password form.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// POST: /Account/ForgotPassword
        /// If user exists, generates reset token and sends email.
        /// Always shows same confirmation to prevent email enumeration.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = Uri.EscapeDataString(token);
                    var resetLink = Url.Action("ResetPassword", "Account",
                        new { email = model.Email, token = encodedToken },
                        protocol: Request.Scheme);

                    var body = $@"
                        <h2>Password Reset Request</h2>
                        <p>Hello {user.FirstName},</p>
                        <p>You requested a password reset for your ScholarRescue account.</p>
                        <p><a href='{resetLink}'>Click here to reset your password</a></p>
                        <p>If you did not request this, please ignore this email.</p>
                        <p>This link expires in 24 hours.</p>
                        <hr />
                        <p style='color:#888;font-size:0.85em;'>ScholarRescue Academic Support Platform</p>";

                    await _emailService.SendEmailAsync(model.Email, "Password Reset Request", body);
                }

                // Always show same confirmation
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}.", model.Email);
                return RedirectToAction("ForgotPasswordConfirmation");
            }
        }

        /// <summary>
        /// GET: /Account/ForgotPasswordConfirmation
        /// Shows generic confirmation that an email was sent if the account exists.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        /// <summary>
        /// GET: /Account/ResetPassword
        /// Validates token and displays reset form.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        /// <summary>
        /// POST: /Account/ResetPassword
        /// Resets the password using the Identity token.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal whether email exists
                    TempData["ErrorMessage"] = "Password reset failed. The link may have expired or is invalid.";
                    return RedirectToAction("Login");
                }

                var token = Uri.UnescapeDataString(model.Token);
                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for {Email}.", model.Email);
                    return RedirectToAction("ResetPasswordConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}.", model.Email);
                TempData["ErrorMessage"] = "An error occurred while resetting your password. Please try again.";
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// GET: /Account/ResetPasswordConfirmation
        /// Shows success message and link to login.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            TempData["SuccessMessage"] = "Your password has been reset. Please log in.";
            return View();
        }

        // ════════════════════════════════════════════════
        // EMAIL VERIFICATION
        // ════════════════════════════════════════════════

        /// <summary>
        /// Verify a user's email via token from verification email link.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Invalid verification link.";
                return RedirectToAction("Login");
            }

            var (success, message) = await _verificationService.VerifyEmailAsync(userId, token);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// Send verification email for the current user (resend).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (user.EmailConfirmed)
            {
                TempData["InfoMessage"] = "Your email is already verified.";
                return RedirectToAction("Dashboard", "Writers");
            }

            await _verificationService.SendVerificationEmailAsync(
                user.Id, user.Email!, $"{user.FirstName} {user.LastName}");

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Verification Email Sent",
                PerformedById = user.Id,
                TargetUserId = user.Id,
                Description = $"Verification email resent to {user.Email}.",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Verification email sent. Please check your inbox.";
            return RedirectToAction("Dashboard", "Writers");
        }
    }
}
