﻿using almondCove.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace almondCove.Controllers
{
    public class StudioRouteController : Controller
    {
        [Route("/studio")]
        public IActionResult Index()
        {
            return View("Views/Gallery/Index.cshtml");
        }

        [Route("/studio/browse")]
        public IActionResult Browse()
        {
            return View("Views/Gallery/Index.cshtml");
        }

        [Route("/studio/view/{slug}")]
        public IActionResult Viewer()
        {
            return View("Views/Gallery/Index.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
