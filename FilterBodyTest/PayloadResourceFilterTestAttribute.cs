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

        private ResourceExecutingContext _filterContext;
        private HttpRequest _request;
        private string _url;


        public PayloadResourceFilterTestAttribute()
        {
        }


        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            _filterContext = context;
            _filterContext.HttpContext.Items["RequestPayload"] = _requestPayload;

            _request = _filterContext.HttpContext.Request;
            _url = UriHelper.GetDisplayUrl(_request) + (_request.QueryString.HasValue ? _request.QueryString.Value : "");

            var proceed = await getJsonRequestData();
            if (!proceed) return;

            getOtherRequestData();
            await next();
        }

        // rx to test for json-start: { followed by a "
        private static Regex _rxJsonStart = new Regex(@"^\s*\{\s*\x22");
        // rx to test for json-end: } <EOF>
        private static Regex _rxJsonEnd = new Regex(@"}\s*$");
        // rx to test for "double" JSON
        private static Regex _rxJsonTwice = new Regex(@"}\s*{");

        public async Task<bool> getJsonRequestData()
        {
            if (_request.ContentType == null ||
                !_request.ContentType.StartsWith("application/json", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }


            if (!_request.Body.CanSeek)
            {
                _request.EnableBuffering();
            }
            _request.Body.Position = 0;

            using var reader = new StreamReader(
                    _request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

            var body = (await reader.ReadToEndAsync()) ?? "";
            _request.Body.Position = 0;

            _filterContext.HttpContext.Items["BodyPayload"] = body;
            _filterContext.HttpContext.Items["Message"] = "OK";

            if (body == "")
            {
                _filterContext.HttpContext.Items["Message"] = "Empty JSON request";
            }
            else
            {
                if(!_rxJsonStart.IsMatch(body) || !_rxJsonEnd.IsMatch(body))
                {
                    _filterContext.HttpContext.Items["Message"] = "JSON syntax error";
                }
                else if(_rxJsonTwice.IsMatch(body))
                {
                    _filterContext.HttpContext.Items["Message"] = "Double JSON";
                }
            }

            return true;
        }

        public void getOtherRequestData()
        {

            foreach (var kv in _request.Query)
            {
                _requestPayload[kv.Key] = kv.Value.Last();
                _hasQueryStringPayload = true;
            }

            if (_request.HasFormContentType && _request.Form != null && _request.Form.Count() > 0)
            {
                foreach (var kv in _request.Form)
                {
                    _requestPayload[kv.Key] = kv.Value.Last();
                    _hasFormPayload = true;
                }
            }
        }

    }
}