﻿using AccountGoWeb.Models;
using Dto.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace AccountGoWeb.Controllers
{
    public class SalesController : Controller
    {
        private readonly IConfiguration _config;

        public SalesController(IConfiguration config)
        {
            _config = config;

            Models.SelectListItemHelper._config = _config;
        }

        public IActionResult Index()
        {
            return RedirectToAction("SalesOrders");
        }

        public async System.Threading.Tasks.Task<IActionResult> SalesOrders()
        {
            ViewBag.PageContentHeader = "Sales Orders";
            using (var client = new HttpClient())
            {
                var baseUri = _config["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "sales/salesorders");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }
            return View();
        }

        public IActionResult AddSalesOrder()
        {
            ViewBag.PageContentHeader = "Add Sales Order";

            return View();
        }

        [HttpPost]
        public IActionResult AddSalesOrder(object Dto)
        {
            return Ok();
        }

        public async System.Threading.Tasks.Task<IActionResult> SalesInvoices()
        {
            ViewBag.PageContentHeader = "Sales Invoices";
            using (var client = new HttpClient())
            {
                var baseUri = _config["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "sales/salesinvoices");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }
            return View();
        }

        public IActionResult AddSalesInvoice()
        {
            ViewBag.PageContentHeader = "Add Sales Invoice";

            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> SalesReceipts()
        {
            ViewBag.PageContentHeader = "Sales Receipts";
            using (var client = new HttpClient())
            {
                var baseUri = _config["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "sales/salesreceipts");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }
            return View();
        }

        public IActionResult AddReceipt()
        {
            ViewBag.PageContentHeader = "Add Receipt";

            var model = new Models.Sales.AddReceipt();

            var customers = GetAsync<IEnumerable<Dto.Sales.Customer>>("sales/customers").Result;
            
            ViewBag.Customers = new HashSet<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            foreach (var customer in customers)
                ViewBag.Customers.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = customer.Id.ToString(), Text = customer.Name });

            var accounts = GetAsync<IEnumerable<Dto.Financial.Account>>("financials/accounts").Result;

            ViewBag.DebitAccounts = new HashSet<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            foreach (var account in accounts.Where(a => a.IsCash == true))
                ViewBag.DebitAccounts.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = account.Id.ToString(), Text = account.AccountName });

            ViewBag.CreditAccounts = new HashSet<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            foreach (var account in accounts)
                ViewBag.CreditAccounts.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = account.Id.ToString(), Text = account.AccountName });

            return View(model);
        }

        [HttpPost]
        public IActionResult AddReceipt(Models.Sales.AddReceipt model)
        {
            if (ModelState.IsValid)
            { }

            return RedirectToAction("salesreceipts");
        }


        public async System.Threading.Tasks.Task<IActionResult> Customers()
        {
            ViewBag.PageContentHeader = "Customers";
            using (var client = new HttpClient())
            {
                var baseUri = _config["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + "sales/customers");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return View(model: responseJson);
                }
            }
            return View();
        }
        
        public IActionResult Customer(int id = -1)
        {
            Customer customerModel = null;
            if (id == -1)
            {
                ViewBag.PageContentHeader = "New Customer";
                customerModel = new Customer();
                customerModel.No = new System.Random().Next(1, 99999).ToString(); // TODO: Replace with system generated numbering.
            }
            else
            {
                ViewBag.PageContentHeader = "Customer Card";
                customerModel = GetAsync<Customer>("sales/customer?id=" + id).Result;
            }

            ViewBag.Accounts = SelectListItemHelper.Accounts();
            ViewBag.TaxGroups = SelectListItemHelper.TaxGroups();
            ViewBag.PaymentTerms = SelectListItemHelper.PaymentTerms();

            return View(customerModel);
        }

        public IActionResult SaveCustomer(Customer model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Customers");
            }
            else {
                ViewBag.Accounts = SelectListItemHelper.Accounts();
                ViewBag.TaxGroups = SelectListItemHelper.TaxGroups();
                ViewBag.PaymentTerms = SelectListItemHelper.PaymentTerms();
            }

            if(model.Id == -1)
                ViewBag.PageContentHeader = "New Customer";
            else
                ViewBag.PageContentHeader = "Customer Card";

            return View("Customer", model);
        }

        public IActionResult CustomerAllocations(int id)
        {
            ViewBag.PageContentHeader = "Customer Allocations";

            return View();
        }

        public IActionResult Allocate(int id)
        {
            ViewBag.PageContentHeader = "Receipt Allocation";

            var model = new Models.Sales.Allocate();

            var receipt = GetAsync<Dto.Sales.SalesReceipt>("sales/salesreceipt?id=" + id).Result;

            model.CustomerId = receipt.CustomerId;
            model.ReceiptId = receipt.Id;
            model.Date = receipt.ReceiptDate;
            model.Amount = (double)receipt.Amount;
            model.RemainingAmountToAllocate = (double)receipt.RemainingAmountToAllocate;

             var invoices = GetAsync<IEnumerable<Dto.Sales.SalesInvoice>>("sales/customerinvoices?id=" + receipt.CustomerId).Result;

            foreach (var invoice in invoices) {
                if (invoice.TotalAllocatedAmount < (double)invoice.TotalAmount)
                {
                    model.AllocationLines.Add(new Models.Sales.AllocationLine()
                    {
                        InvoiceId = invoice.Id,
                        Amount = (double)invoice.TotalAmount,
                        AllocatedAmount = invoice.TotalAllocatedAmount
                    });
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Allocate(Models.Sales.Allocate model)
        {
            if (ModelState.IsValid)
            {
                if (!model.IsValid()) {
                    return View(model);
                }
            }

            return RedirectToAction("salesreceipts");
        }

        #region Private methods
        public async System.Threading.Tasks.Task<T> GetAsync<T>(string uri)
        {
            string responseJson = string.Empty;
            using (var client = new HttpClient())
            {
                var baseUri = _config["ApiUrl"];
                client.BaseAddress = new System.Uri(baseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync(baseUri + uri);
                if (response.IsSuccessStatusCode)
                {
                    responseJson = await response.Content.ReadAsStringAsync();
                }
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseJson);
        }
        #endregion
    }
}