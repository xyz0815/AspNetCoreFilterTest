using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;


using DTO = System.Collections.Generic.Dictionary<string, object>;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
// using mfsd.DtoLib;

namespace mfsd.Core.Mvc.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PayloadResourceFilterTestAttribute : Attribute, IAsyncResourceFilter, IOrderedFilter
    {
        public static Action<string> DebugWriter { get; set; }

        public int Order { get; set; } = 0;

        private DTO _requestPayload = new DTO();

        private bool _hasJsonPayload = false;
        private bool _hasQueryStringPayload = false;
        private bool _hasFormPayload = false;

        /// <summary>
        /// No need for a "big" buffer, only for Json-Probing, 
        /// set to expected average JSON post size 
        /// </summary>
        public int JsonBufferSize { get; set; } = 4096;

        //private ResourceExecutingContext _filterContext;
        //private HttpRequest _request;
        //private string _url;


        public PayloadResourceFilterTestAttribute()
        {
        }


        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var filterContext = context;
            filterContext.HttpContext.Items["RequestPayload"] = _requestPayload;

            var proceed = await getJsonRequestData(filterContext);
            if (!proceed) return;

            getOtherRequestData(filterContext);
            await next();
        }

        // rx to test for json-start: { followed by a "
        private static Regex _rxJsonStart = new Regex(@"^\s*\{\s*\x22");
        // rx to test for json-end: } <EOF>
        private static Regex _rxJsonEnd = new Regex(@"}\s*$");
        // rx to test for "double" JSON
        private static Regex _rxJsonTwice = new Regex(@"}\s*{");

        public async Task<bool> getJsonRequestData(ResourceExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            if (request.ContentType == null ||
                !request.ContentType.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }


            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }
            request.Body.Position = 0;

            using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

            var body = (await reader.ReadToEndAsync()) ?? "";
            request.Body.Position = 0;

            filterContext.HttpContext.Items["BodyPayload"] = body;
            filterContext.HttpContext.Items["Message"] = "OK";

            if (body == "")
            {
                filterContext.HttpContext.Items["Message"] = "Empty JSON request";
            }
            else
            {
                if(!_rxJsonStart.IsMatch(body) || !_rxJsonEnd.IsMatch(body))
                {
                    filterContext.HttpContext.Items["Message"] = "JSON syntax error";
                }
                else if(_rxJsonTwice.IsMatch(body))
                {
                    filterContext.HttpContext.Items["Message"] = "Double JSON";
                }
            }

            return true;
        }

        public void getOtherRequestData(ResourceExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;

            foreach (var kv in request.Query)
            {
                _requestPayload[kv.Key] = kv.Value.Last();
                _hasQueryStringPayload = true;
            }

            if (request.HasFormContentType && request.Form != null && request.Form.Count() > 0)
            {
                foreach (var kv in request.Form)
                {
                    _requestPayload[kv.Key] = kv.Value.Last();
                    _hasFormPayload = true;
                }
            }
        }

    }
}