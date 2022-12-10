using System.IdentityModel.Tokens.Jwt;
using AppRoles.WebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppRoles.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure App Roles options.
            var appRolesOptions = new AppRolesOptions();
            Configuration.GetSection("AppRoles").Bind(appRolesOptions);
            services.AddSingleton<AppRolesOptions>(appRolesOptions);

            // Inject a service to work with App Roles in the Azure AD B2C directory itself which is accessed through the Graph API.
            services.Configure<AzureADAppRolesProviderOptions>(Configuration.GetSection("AzureAdB2C"));
            services.AddSingleton<IAppRolesProvider, AzureADAppRolesProvider>();

            // Configure support for the SameSite cookies breaking change.
            services.ConfigureSameSiteCookiePolicy();

            // Don't map any standard OpenID Connect claims to Microsoft-specific claims.
            // See https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // Add Azure AD B2C authentication using OpenID Connect.
#pragma warning disable 0618 // AzureADB2CDefaults is obsolete in favor of "Microsoft.Identity.Web"
            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));

            services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, options =>
            {
                // Don't remove any incoming claims.
                options.ClaimActions.Clear();

                // Define the role claim type to match the configured user attribute name in Azure AD B2C.
                options.TokenValidationParameters.RoleClaimType = appRolesOptions.UserAttributeName;
            });
#pragma warning restore 0618

            // Add a claims transformation to split the space-separated app roles into multiple individual claims,
            // so that we can more easily check if a user has a role with User.IsInRole(roleName) and other built-in
            // roles functionality within ASP.NET.
            services.AddSingleton<IClaimsTransformation>(new StringSplitClaimsTransformation(appRolesOptions.UserAttributeName));

            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddControllers();
            services.AddRouting(options => { options.LowercaseUrls = true; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
