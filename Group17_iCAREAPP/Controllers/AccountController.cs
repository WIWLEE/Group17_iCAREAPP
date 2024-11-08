﻿using System;
using System.Web.Mvc;
using System.Web.Security;
using Group17_iCAREAPP.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Group17_iCAREAPP.Controllers
{
    public class AccountController : Controller
    {
        private readonly Group17_iCAREDBEntities db = new Group17_iCAREDBEntities();

        //For user login
        // GET: Account/Login
        public ActionResult Login(string returnUrl)
        {
            //Checks if user is in database and if they are, sends to home screen
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        //Method to check the Login credentials of user
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                //Includes the people that have passwords and can find from UserPassword model
                var userPassword = db.UserPassword
                    .Include(u => u.iCAREUser)
                    .Include(u => u.iCAREUser.iCAREAdmin)
                    .Include(u => u.iCAREUser.iCAREWorker)
                    .FirstOrDefault(u => u.userName == model.Username);

                //Validation that password exists
                if (userPassword != null)
                {
                    // Check if account is expired
                    if (userPassword.userAccountExpiryDate != null &&
                        userPassword.userAccountExpiryDate <= DateTime.Now)
                    {
                        ModelState.AddModelError("", "Your account has expired. Please contact an administrator.");
                        return View(model);
                    }

                    // Verify password
                    var hashedPassword = HashPassword(model.Password);
                    if (hashedPassword == userPassword.encryptedPassword)
                    {
                        // Determine user role
                        // If user is in admin table, they have role as administrator
                        string role = userPassword.iCAREUser.iCAREAdmin != null ?
                            "Administrator" :
                            userPassword.iCAREUser.iCAREWorker?.UserRole?.roleName ?? "Worker";

                        // This FormsAuthenticationTicket class allows for there to be the option to 
                        // store the login in cookies so the person is logged out after 30 minutes
                        // automatically, but will stay logged in even if the site is closed
                        FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                            1,
                            userPassword.userName,
                            DateTime.Now,
                            DateTime.Now.AddMinutes(30),
                            model.RememberMe,
                            role,
                            FormsAuthentication.FormsCookiePath
                        );

                        // Adding a level of encryption for cookies and login
                        string encryptedTicket = FormsAuthentication.Encrypt(ticket);
                        var cookie = new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

                        // If the Remember Me box is selected, remembers login for 2 weeks
                        if (model.RememberMe)
                            cookie.Expires = DateTime.Now.AddDays(14);

                        Response.Cookies.Add(cookie);

                        if (Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
        }

        // POST: Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Method allows the user to log out and redirects back to login page to login as another user
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        //This method allows the user to change their password
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // First must find the user's password
                var userPassword = db.UserPassword.FirstOrDefault(u => u.userName == User.Identity.Name);
                if (userPassword != null)
                {
                    // Checks that the password entered matches the password in the database
                    var currentHashedPassword = HashPassword(model.CurrentPassword);
                    if (currentHashedPassword == userPassword.encryptedPassword)
                    {
                        // If the entered password matches, user can change password
                        userPassword.encryptedPassword = HashPassword(model.NewPassword);
                        db.SaveChanges();

                        TempData["Success"] = "Password changed successfully.";
                        return RedirectToAction("Index", "Home");
                    }
                    ModelState.AddModelError("", "Current password is incorrect.");
                }
            }
            return View(model);
        }

        // Method hashes the password with SHA - 256 hash formula
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                // Returns the hashed password as a string
                return Convert.ToBase64String(hashedBytes);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Model added here to be able to temporarily store information for the view
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    // Another model that temporarily stores data to allow user to change password
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}