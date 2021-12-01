using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    public static class HttpContext
    {
        private static Microsoft.AspNetCore.Http.IHttpContextAccessor m_httpContextAccessor;

        private static Microsoft.AspNetCore.Hosting.IWebHostEnvironment m_webHostEnvironment;

        public static void Configure(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_webHostEnvironment = webHostEnvironment;
        }

        public static Microsoft.AspNetCore.Http.HttpContext Current
        {
            get
            {
                return m_httpContextAccessor.HttpContext;
            }
        }

        public static Microsoft.AspNetCore.Hosting.IWebHostEnvironment WebHostEnvironment
        {
            get
            {
                return m_webHostEnvironment;
            }
        }
    }
}
