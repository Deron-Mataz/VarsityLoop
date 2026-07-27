using Microsoft.AspNetCore.Mvc;

namespace VarsityLoop.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/NotFound")]
        public IActionResult NotFound404()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        [Route("Error/Unauthorized")]
        public IActionResult Unauthorized401()
        {
            Response.StatusCode = 401;
            return View("Unauthorized");
        }

        [Route("Error/Forbidden")]
        public IActionResult Forbidden403()
        {
            Response.StatusCode = 403;
            return View("Forbidden");
        }

        [Route("Error/ServerError")]
        public IActionResult ServerError500()
        {
            Response.StatusCode = 500;
            return View("ServerError");
        }

        /// <summary>
        /// Catch-all invoked by app.UseStatusCodePagesWithReExecute for any status
        /// code not explicitly handled above.
        /// </summary>
        [Route("Error/{code:int}")]
        public IActionResult HandleStatusCode(int code)
        {
            return code switch
            {
                404 => NotFound404(),
                401 => Unauthorized401(),
                403 => Forbidden403(),
                _ => ServerError500()
            };
        }
    }
}
