using mfsd.Core.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FilterBodyTest.Controllers
{
    [PayloadResourceFilterTest]
    public class ProcessController : Controller
    {
        public string Message { get; set; }
        public string Payload { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            Payload = Request.HttpContext.Items["BodyPayload"] as string;
            Message = Request.HttpContext.Items["Message"] as string;
        }

    }
}
