using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public ActionResult AdminLogin()
        {
            //If no admin password is set in the database, set one.
            var db = new JamesEntities();
            var check = db.AdminPassword.Where(u => u.Id > 0).FirstOrDefault();
            if (check == null)
            {
                var adminPassword = Salt.Encode("Pa$$w0rd", null);
                db.AdminPassword.Add(new AdminPassword
                {
                    AdminPassword1 = adminPassword
                });
                db.SaveChanges();
            }
            return View();
        }
        [HttpPost]
        public ActionResult AdminLogin(AdminPassword model)
        {
            if (ModelState.IsValid)
            {
                //Checks if the input password matches the one on file in the database. If it does, proceed. If not, display an error message.
                var db = new JamesEntities();
                var password = model.AdminPassword1;
                var check = db.AdminPassword.Where(u => u.Id > 0).FirstOrDefault();
                bool correct = Salt.Verify(password, check.AdminPassword1);
                if (correct == true)
                {
                    Session["Message"] = "Welcome.";
                    Session["AdminUsername"] = "Admin";
                    return RedirectToAction("Admin", "Admin");
                }
                else
                {
                    ViewData["Message"] = "Login failed.";
                }
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult Admin()
        {
            //If an admin login session is set, do nothing. If it is not, return to the admin login page.
            if (Session["AdminUsername"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
        }
        [HttpPost]
        public ActionResult Logout()
        {
            //Terminate the admin login session, and return to the admin login page.
            Session.Abandon();
            return RedirectToAction("AdminLogin", "Admin");
        }
        [HttpGet]
        public ActionResult AddStock()
        {
            //If an admin login session is set, do nothing. If it is not, return to the admin login page.
            if (Session["AdminUsername"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
        }
        [HttpPost]
        public JsonResult StockCodeCheck(string check)
        {
            var db = new JamesEntities();
            //Checks the database for any stock codes matching the one input. If no matches are returned, return true. Otherwise, return false.
            var duplicateCheck = db.Stock.Where(u => u.Code == check).FirstOrDefault();
            if (duplicateCheck == null)
            {
                return Json(new { free = true });
            }
            else
            {
                return Json(new { free = false });
            }
        }
        [HttpPost]
        public ActionResult AddStock(Stock model, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                //Add a new stock item record, save changes, upload the selected image, update the session message, and return to the Admin home page.
                var db = new JamesEntities();
                db.Stock.Add(new Stock
                {
                    Code = model.Code,
                    Description = model.Description,
                    PricePerUnit = model.PricePerUnit
                });
                db.SaveChanges();
                var location = Path.Combine(Server.MapPath("~/Images/"), model.Code + ".jpg");
                image.SaveAs(location);
                Session["Message"] = "Stock item successfully added.";
                return RedirectToAction("Admin", "Admin");
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult ChangeAdminPassword()
        {
            //If an admin login session is set, do nothing. If it is not, return to the admin login page.
            if (Session["AdminUsername"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
        }
        [HttpPost]
        public ActionResult ChangeAdminPassword(AdminPassword model, string OldPassword, string NewPassword, string ConfirmNewPassword)
        {
            if (ModelState.IsValid)
            {
                //If New Password and Confirm New Password do not match, generate an error message. Otherwise, proceed.
                if (NewPassword != ConfirmNewPassword)
                {
                    ViewData["Message"] = "New Password and Confirm New Password do not match.";
                }
                else
                {
                    var db = new JamesEntities();
                    //Check if the input Old Password is correct. If not, generate an error message. Otherwise, update the record in the database, save changes, update the session message, and return to the Admin home page.
                    var check = db.AdminPassword.Where(u => u.Id > 0).FirstOrDefault();
                    bool correct = Salt.Verify(OldPassword, check.AdminPassword1);
                    if (correct == false)
                    {
                        ViewData["Message"] = "Old Password is incorrect.";
                    }
                    else
                    {
                        check.AdminPassword1 = Salt.Encode(NewPassword, null);
                        db.SaveChanges();
                        Session["Message"] = "Admin password successfully changed. Please notify other admins.";
                        return RedirectToAction("Admin", "Admin");
                    }
                }
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult CheckOrders()
        {
            //If an admin login session is set, select all IDs in the order table in the database and return a list of them. If it is not, return to the admin login page.
            if (Session["AdminUsername"] != null)
            {
                var db = new JamesEntities();
                var list = db.Order.ToList();
                List<string> orderNumbers = new List<string>();
                orderNumbers.Add("");
                for (int i = 0; i < list.Count; i++)
                {
                    orderNumbers.Add(list[i].Id.ToString());
                }
                SelectList orderNumbers1 = new SelectList(orderNumbers);
                ViewData["OrderNumbers"] = orderNumbers1;
                return View();
            }
            else
            {
                return RedirectToAction("AdminLogin", "Admin");
            }
        }
        [HttpPost]
        public JsonResult GetOrderInfo(string orderNumber)
        {
            //Return all information for the order selected - the customer who placed it, the total value, and all items ordered.
            var orderNumber2 = int.Parse(orderNumber);
            var db = new JamesEntities();
            var details = db.OrderStock.Join(db.Stock, os => os.StockCode, s => s.Code, (os, s) => new { os, s }).Join(db.Order, oss => oss.os.OrderID, o => o.Id, (oss, o) => new { oss, o }).Join(db.Customer, osso => osso.o.Customer, c => c.Id, (osso, c) => new { osso.o.Id, c.FirstName, c.Surname, c.ContactNumber, c.Address1, c.Address2,  c.TownCity, c.County, c.Postcode, osso.oss.s.Code, osso.oss.s.Description, osso.oss.os.Quantity, Total = osso.o.OrderTotal }).Where(u => u.Id == orderNumber2).ToList();
            return Json(details);
        }
    }
    }
