﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wyam.Hosting.Middleware
{
    internal class DefaultExtensionsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFileProvider _fileProvider;
        private readonly string[] _extensions;
        private readonly ILogger _logger;

        public DefaultExtensionsMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, IOptions<DefaultExtensionsOptions> options, ILoggerFactory loggerFactory)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _next = next;
            _fileProvider = hostingEnv.WebRootFileProvider;
            _extensions = (options?.Value ?? new DefaultExtensionsOptions()).Extensions.Select(x => x.StartsWith(".") ? x : ("." + x)).ToArray();
            _logger = loggerFactory.CreateLogger<DefaultExtensionsMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsGetOrHeadMethod(context.Request.Method)
                && !PathEndsInSlash(context.Request.Path))
            {
                // Check if there's a file with a matched extension, and rewrite the request if found
                foreach (string extension in _extensions)
                {
                    string filePath = context.Request.Path.ToString() + extension;
                    IFileInfo fileInfo = _fileProvider.GetFileInfo(filePath);
                    if (fileInfo != null && fileInfo.Exists)
                    {
                        _logger.LogInformation($"Rewriting extensionless path to {filePath}");
                        context.Request.Path = new PathString(filePath);
                        break;
                    }
                }
            }
            await _next(context);
        }

        private static bool IsGetOrHeadMethod(string method) =>
            HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        private static bool PathEndsInSlash(PathString path) =>
            path.Value.EndsWith("/", StringComparison.Ordinal);
    }
}
