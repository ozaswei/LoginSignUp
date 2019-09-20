using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProgrammersGuide.Models;
using System.Net;
using System.Net.Mail;
using System.Web.Security;

namespace ProgrammersGuide.Controllers
{
    public class UserController : Controller
    {
        //Registration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        //Registration Post Action
        [HttpPost]
        [ValidateAntiForgeryToken]//it is used to prevent foregery of a request
        public ActionResult Registration([Bind(Exclude = "hEmailVerified,ActivationCode")] User user)
        {
            bool Status = false;
            string Message = "";
            //
            //Model Validation 
            if (ModelState.IsValid)
            {
                #region Email already exists
                var IsExist = IsEmailExist(user.EmailID);
                if (IsExist)
                {
                    ModelState.AddModelError("EmailExists", "Email Already Exist");
                    return View(user);//we should show this message in view
                }
                #endregion


                #region Generate Activation Code
                user.ActivationCode = Guid.NewGuid();
                #endregion


                #region Password Hashing 
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);//as it will validate again after step changes ,so we also hased the confirm password
                #endregion
                user.hEmailVerified = false; // initially we need tomake this false


                #region Save data to Database and send verification link to email
                using (MyDatabaseEntities dc = new MyDatabaseEntities()) //creating connection with database
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();

                    //Send email to user 
                    SendVerificationEmailLink(user.EmailID, user.ActivationCode.ToString());
                    Message = "Registration sucessful. Account activation link has been sent to your Email Id: "+user.EmailID;
                    Status = true;
                }
                #endregion
            }
            else
            {
                Message = "Invalid Request";
            }

            ViewBag.Message = Message;
            ViewBag.Status = Status;

            //Send email to user 

            return View(user);
        }


        //Verify Account
        [HttpGet]
        public ActionResult VerifyAccount(string id) //this id is the activation code
        {
            bool Status = false;
            using (MyDatabaseEntities dc =new MyDatabaseEntities())//here MyDatabaseEntities is our datacontext
            {
                dc.Configuration.ValidateOnSaveEnabled = false; //This line is written to avoid Confirm Password doesn't match issue on save changes
                var v= dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if(v!=null)
                {
                    v.hEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
                ViewBag.Status = Status;
                return View();
        }


        //Login
        [HttpGet] // this action is used to get the login form
        public ActionResult Login() 
        {
            return View(); // Here while creating this view, we need to click on use refernce for client side validation 
        }


        //Login Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login,string ReturnUrl)
        {
            string message = "";
            using (MyDatabaseEntities dc=new MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID == login.EmailID).FirstOrDefault();
                if (v != null)
                {
                    if(string.Compare(Crypto.Hash(login.Password),v.Password)==0)//comparing pasword typed with database password 
                    {
                        //here we create a cookie too if user selects rememeber me
                        int timeout = login.RememberMe ? 525600 : 20; //525600 is 1 year
                        var ticket = new FormsAuthenticationTicket(login.EmailID, login.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true; // we make this true if we dont want to acces it from javascript
                        Response.Cookies.Add(cookie);

                        if(Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index","Home");
                        }
                    }
                    else
                    {
                        message = "Invalid Credential provided.";
                    }
                }
                else
                {
                    message = "Invalid Credential provided.";
                }
            }
                ViewBag.Message = message;
            return View();
        }


        //Logout
        [Authorize] // without [Authorize] nobody can access this action
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login","User");
        }

        [NonAction] // this represents that this controller method isnt an action method
        public bool IsEmailExist(string emailID)
        {
            using (MyDatabaseEntities dc = new MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                return v != null;//if not equals to null that means its true 
            }
        }

        [NonAction] // this method is to send verification link to the email
        public void SendVerificationEmailLink(string emailID, string ActivationCode,string emailFor="VerifyAccount")
        {
            var verifyUrl = "/User/"+emailFor+"/" + ActivationCode;

            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("1programmersguide2@gmail.com","Programming's Fun"); // the email used to send the mail

            var fromEmailPassword = "1trunks2"; // here you need to provide the password of your email

            var toEmail = new MailAddress(emailID);

            string subject = "";
            string body = "";
            if (emailFor=="VerifyAccount") // this is for verify account
            {

                 subject = "Your account has been successfully create.";

                 body = "<br/><br/>We are exicted to tell you that your ProgrammersGuide account has been created.Give yourself a pat on your back ^_^ " + "Please click on the below link to verify your account" + " <br/><br/><a href='" + link + "'>" + link + "</a>";

            }
            else if (emailFor=="ResetPassword")
            {
                subject = "Reset Password";
                body = "Hi,<br/><br/>We got a request to rest your password.Please click on the below link to reset your password"+"<br/><br/><a href="+link+">Reset Password Link</a>";

            }

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address,fromEmailPassword)
            };
            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            smtp.Send(message);
        }

        //Forgot Password
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]//to verify the email of the user who forgot the password
        public ActionResult ForgotPassword(string EmailID)
        {
            //Verify the email ID
            string message = "";
            bool status = false;
            using (MyDatabaseEntities dc =new MyDatabaseEntities())
            {
                var account = dc.Users.Where(a => a.EmailID == EmailID).FirstOrDefault();//checking if the email is there at the database or not
                if(account!=null)
                {
                    //Send email for reset password
                    string resetCode = Guid.NewGuid().ToString();
                    SendVerificationEmailLink(account.EmailID, resetCode, "ResetPassword");
                    account.ResetPasswordCode = resetCode; // update the user table with the unique ID of the ResetPasssword link here
                    dc.Configuration.ValidateOnSaveEnabled = false; //to avoid confirm password doesnot match issue, as we have added a confirm password property in our model class.
                    dc.SaveChanges();
                    message = "A link to reset your password has been sent to your E-mail ";
                }
                else
                {
                    message = "Account not found";
                }
            }
            ViewBag.Message = message;
            ViewBag.Status = status;

                //Generate rest Password Link

                //Send Email

                return View();
        }

        public ActionResult ResetPassword(string id)
        {
            //Verify the password link 
            //find account associated with this link
            //redirect to reset password page
            using (MyDatabaseEntities dc=new MyDatabaseEntities())
            {
                var user = dc.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
                if(user !=null)
                {
                    ResetPasswordModel model = new ResetPasswordModel();
                    model.ResetCode = id;
                    return View(model);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            if(ModelState.IsValid)
            {
                using (MyDatabaseEntities dc = new MyDatabaseEntities())
                {
                    var user = dc.Users.Where(a => a.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                    if(user != null)
                    {
                        user.Password = Crypto.Hash(model.NewPassword); // so that no one can copy  this from the database
                        user.ResetPasswordCode = ""; // this is used so that , only one time the user can update their password with this reset password code
                        dc.Configuration.ValidateOnSaveEnabled = false;
                        dc.SaveChanges();
                        message = "Your Password has been successfully updated !!";
                    }
                }
            }
            else
            {
                message = "Something went wrong!!";
            }
            ViewBag.Message = message;
            return View(model);
        }
    }
}


//Dont forget to add:: using ProgrammersGuide.Models;
//Dont forget to add:: using System.Net;
//Dont forget to add:: using System.Net.Mail;;
//Dont forget to add :: using using System.Web.Security;
//use ctrl+. for adding any using System.---; if required
//We use [Bind(Exclude="hEmailVerified,ActivationCode")], because the registration will be successful only when the user email is verified(i.e. hEmailVerified) and when the user enters the activation code (i.e. ActivationCode), so we can prevent the properties that are allowed to be bind automatically using this .
//For Login we create another Model
//we cannot revert the has password to password before , so we check the hash password with hash password.
//Without [Authorize] noone can access the action
//after Logout we write [Authorize] at the home controller public class
//then we go to web.config, and under the </appSettings> there is <system.web>, add there :-
//<authentication mode="Forms">
//<forms cookieless = "UseCookies" loginUrl="~/user/login" slidingExpiration="true"></forms>
//</authentication>