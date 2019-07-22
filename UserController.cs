using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View();
        }
        [HttpPost]
        public JsonResult EmailCheck(string check)
        {
            //Checks if the input email address matches any currently in use. If it does, return false. Otherwise, return true.
            var db = new JamesEntities();
            var duplicateCheck = db.Customer.Where(u => u.Email == check).FirstOrDefault();
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
        public ActionResult CreateAccount(Customer model)
        {
            if (ModelState.IsValid)
            {
                //Creates a new record in the database, saves changes, and redirects to the starting page.
                var db = new JamesEntities();
                db.Customer.Add(new Customer
                {
                    FirstName = model.FirstName,
                    Surname = model.Surname,
                    Password = Salt.Encode(model.Password, null),
                    ContactNumber = model.ContactNumber,
                    Email = model.Email,
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    TownCity = model.TownCity,
                    County = model.County,
                    Postcode = model.Postcode
                });
                db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(Customer model)
        {
            if (ModelState.IsValid)
            {
                //If the input email address does not match any in the database, generate an error message. Otherwise, proceed.
                var db = new JamesEntities();
                var password = model.Password;
                var check = db.Customer.Where(u => u.Email == model.Email).FirstOrDefault();
                if (check == null)
                {
                    ViewData["Message"] = "Email address not found.";
                }
                else
                {
                    //Check if the input password matches the one for the selected record. If it does, proceed. If not, generate an error message.
                    bool correct = Salt.Verify(password, check.Password);
                    if (correct == true)
                    {
                        Session["Name"] = check.FirstName;
                        Session["Email"] = check.Email;
                        Session["Message"] = "Welcome, " + check.FirstName + ".";
                        return RedirectToAction("Customer", "User");
                    }
                    else
                    {
                        ViewData["Message"] = "Login failed.";
                    }
                }
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult Customer()
        {
            //If a login session is set, do nothing. If it is not, return to the user login page.
            if (Session["Email"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }
        [HttpPost]
        public ActionResult Logout()
        {
            //Terminate the active session and return to the user login page.
            Session.Abandon();
            return RedirectToAction("Login", "User");
        }
        [HttpGet]
        public ActionResult EditDetails()
        {
            //If a login session is set, do nothing. If it is not, return to the user login page.
            if (Session["Email"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }
        [HttpPost]
        public ActionResult EditDetails(Customer model, bool Blank)
        {
            if (ModelState.IsValid)
            {
                //Update the logged-in user's record with all fields that have been updated, update the session message, and return to the user home page.
                var db = new JamesEntities();
                var search = Session["Email"];
                var toUpdate = db.Customer.Where(u => u.Email == search).FirstOrDefault();
                    if (model.FirstName != null)
                    {
                        toUpdate.FirstName = model.FirstName;
                    }
                    if (model.Surname != null)
                    {
                        toUpdate.Surname = model.Surname;
                    }
                    if (model.Password != null)
                    {
                        toUpdate.Password = Salt.Encode(model.Password, null);
                    }
                    if (model.ContactNumber != null)
                    {
                        toUpdate.ContactNumber = model.ContactNumber;
                    }
                    if (model.Email != null)
                    {
                        Session["Email"] = model.Email;
                        toUpdate.Email = model.Email;
                    }
                    if (model.Address1 != null)
                    {
                        toUpdate.Address1 = model.Address1;
                    }
                    if (model.Address2 != null || Blank == true)
                    {
                        toUpdate.Address2 = model.Address2;
                    }
                    if (model.TownCity != null)
                    {
                        toUpdate.TownCity = model.TownCity;
                    }
                    if (model.County != null)
                    {
                        toUpdate.County = model.County;
                    }
                    if (model.Postcode != null)
                    {
                        toUpdate.Postcode = model.Postcode;
                    }
                    db.SaveChanges();
                    Session["Message"] = "Your details have been successfully updated.";
                    return RedirectToAction("Customer", "User");      
            }
            return View(model);
        }
        [HttpGet]
        public ActionResult Order()
        {
            //If a login session is set, return a list of all stock codes from the database. If it is not, return to the user login page.
            if (Session["Email"] != null)
            {
                var db = new JamesEntities();
                var list = db.Stock.ToList();
                List<string> codes = new List<string>();
                codes.Add("");
                for (int i = 0; i < list.Count; i++)
                {
                    codes.Add(list[i].Code);
                }
                SelectList codes1 = new SelectList(codes);
                ViewData["StockCodes"] = codes1;
                return View();
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }
        [HttpPost]
        public JsonResult GetDetails(string stockCode)
        {
                //Return the description and price per unit for the selected stock item.
                var db = new JamesEntities();
                var item = db.Stock.Select(u => new { u.Code, u.Description, u.PricePerUnit }).Where(v => v.Code == stockCode).FirstOrDefault();
                return Json(item, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult Order(IList<Item> Items)
        {
            if (ModelState.IsValid)
            {
                //Update all relevant tables and the session message, and return to the User home page.
                var db = new JamesEntities();
                var numberOfItems = Items.Count;
                var orderNumber = db.Order.ToList().Count + 1;
                var combinedTotal = 0.0;
                var search = Session["Email"];
                var userID = db.Customer.Where(u => u.Email == search).FirstOrDefault().Id;
                for (int i = 0; i < numberOfItems; i++)
                {
                    combinedTotal += Items[i].Total;
                }
                combinedTotal = Math.Round(combinedTotal, 2);
                db.Order.Add(new Order
                {
                    Customer = userID,
                    OrderTotal = combinedTotal
                });
                db.SaveChanges();
                for (int j = 0; j < numberOfItems; j++)
                {
                    db.OrderStock.Add(new OrderStock
                    {
                        OrderID = orderNumber,
                        StockCode = Items[j].Code,
                        Quantity = Items[j].Quantity
                });
                }
                db.SaveChanges();
                Session["Message"] = "Order successfully placed.";
                return RedirectToAction("Customer", "User");
            }
            return View();
        }
        [HttpGet]
        public ActionResult CheckOrders()
        {
            //If a login session is set, return a list of all numbers for orders placed by the logged-in user. If it is not, return to the user login page.
            if (Session["Email"] != null)
            {
                var db = new JamesEntities();
                var search = Session["Email"];
                var check = db.Customer.Where(u => u.Email == search).FirstOrDefault();
                var userID = check.Id;
                var list = db.Order.Where(u => u.Customer == userID).ToList();
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
                return RedirectToAction("Login", "User");
            }
        }
        [HttpPost]
        public JsonResult GetOrderInfo (string orderNumber)
        {
            //Return the total value of the order selected, and all items ordered.
            var orderNumber2 = int.Parse(orderNumber);
            var db = new JamesEntities();
            var details = db.OrderStock.Join(db.Order, os => os.OrderID, o => o.Id, (os, o) => new { os, o }).Join(db.Stock, oso => oso.os.StockCode, s => s.Code, (oso, s) => new { Id = oso.os.OrderID, oso.os.Quantity, s.Code, s.Description, Total = oso.o.OrderTotal}).Where(u => u.Id == orderNumber2).ToList();
            return Json(details);
        }
    }
}
