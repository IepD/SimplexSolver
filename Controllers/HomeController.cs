using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimplexSolver.Models;

namespace SimplexSolver.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(Simplex simplex)
        {
            TempData["Variaveis"] = simplex.Variaveis;
            TempData["Restricoes"] = simplex.Restricoes;
            TempData["Minimizar"] = simplex.Minimizar;
            TempData["ExibirPassoAPasso"] = simplex.ExibirPassoAPasso;
            return View(simplex);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
