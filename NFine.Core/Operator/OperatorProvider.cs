using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace NFine.Code
{
    public class OperatorProvider
    {
        public static OperatorProvider Provider
        {
            get { return new OperatorProvider(); }
        }

        //private string LoginUserKey = "nfine_loginuserkey_2016";
        private string LoginProvider = Configs.GetValue("LoginProvider");

        public OperatorModel GetCurrent()
        {
            OperatorModel operatorModel = new OperatorModel();
            foreach (var claim in HttpContext.Current.User.Claims)
            {
                if (claim.Type == "OperatorModel")
                {
                    operatorModel = Newtonsoft.Json.JsonConvert.DeserializeObject<OperatorModel>(claim.Value);
                }
            }
            return operatorModel;
        }

        /// <summary>
        /// 设置当前登录用户的信息
        /// </summary>
        /// <param name="operatorModel"></param>
        /// <returns></returns>
        public async Task AddCurrent(OperatorModel operatorModel)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, operatorModel.UserId),
                new Claim("OperatorModel",Newtonsoft.Json.JsonConvert.SerializeObject(operatorModel))
            }, CookieAuthenticationDefaults.AuthenticationScheme));

            await HttpContext.Current.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
            {
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)//设置登录的Cookie的有效期时间
            });

        }

        public async Task RemoveCurrent()
        {
            await HttpContext.Current.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
