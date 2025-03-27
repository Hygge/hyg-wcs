using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using infrastructure.Exceptions;
using domain.Result;

namespace infrastructure.Filters
{
    public class GlobalExceptionFilter : IAsyncExceptionFilter, IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }



        public Task OnExceptionAsync(ExceptionContext context)
        {

            switch (context.Exception)
            {
                case BusinessException:
                    var h = context.Exception as BusinessException;
                    context.Result = new ObjectResult(ApiResult.failed(h.code, h.message));
                    _logger.LogError(context.Exception, h.message);
                    break;
                default:
                    context.Result = new ObjectResult(ApiResult.failed(500, context.Exception.Message));
                    _logger.LogError(context.Exception, context.Exception.Message);
                    break;
            }

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }

        public void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case BusinessException:
                    var h = context.Exception as BusinessException;
                    context.Result = new ObjectResult(ApiResult.failed(h.code, h.message));
                    _logger.LogError(context.Exception, h.message);
                    break;
                default:
                    context.Result = new ObjectResult(ApiResult.failed(500, context.Exception.Message));
                    _logger.LogError(context.Exception, context.Exception.Message);
                    break;
            }
        }
    }
}
