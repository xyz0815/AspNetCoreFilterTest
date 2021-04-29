using mfsd.Core.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FilterBodyTest.Controllers
{
    public class ApiController : ProcessController
    {
        [HttpPost]
        public IActionResult bodyTest()
        {
            var answer = new Dictionary<string, object>();

            if (Payload == null)
            {
                answer["message"] = "Filter did not run";
            }
            else
            {
                answer["message"] = Message;
            }

            answer["jsonBody"] = Payload;


            return Json(answer);

        }
    }
}
