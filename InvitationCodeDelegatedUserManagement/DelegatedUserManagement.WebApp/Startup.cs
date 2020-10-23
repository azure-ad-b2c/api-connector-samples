using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DelegatedUserManagement.WebApp
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
            // Inject a service to store user invitations.
            var userInvitationsBasePath = Configuration.GetValue<string>("App:UserInvitationsBasePath");
            if (string.IsNullOrWhiteSpace(userInvitationsBasePath))
            {
                userInvitationsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserInvitations");
            }
            services.AddSingleton<IUserInvitationRepository>(new FileStorageUserInvitationRepository(userInvitationsBasePath));

            // Inject a service to work with Azure AD B2C through the Graph API.
            var b2cConfigurationSection = Configuration.GetSection("AzureAdB2C");
            var b2cGraphService = new B2cGraphService(
                clientId: b2cConfigurationSection.GetValue<string>(nameof(AzureADB2COptions.ClientId)),
                domain: b2cConfigurationSection.GetValue<string>(nameof(AzureADB2COptions.Domain)),
                clientSecret: b2cConfigurationSection.GetValue<string>(nameof(AzureADB2COptions.ClientSecret)),
                b2cExtensionsAppClientId: b2cConfigurationSection.GetValue<string>("B2cExtensionsAppClientId"));
            services.AddSingleton<B2cGraphService>(b2cGraphService);

            // Configure support for the SameSite cookies breaking change.
            services.ConfigureSameSiteCookiePolicy();

            // Don't map any standard OpenID Connect claims to Microsoft-specific claims.
            // See https://leastprivilege.com/2017/11/15/missing-claims-in-the-asp-net-core-2-openid-connect-handler/.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // Add Azure AD B2C authentication using OpenID Connect.
            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureAdB2C", options));

            services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, options =>
            {
                // Don't remove any incoming claims.
                options.ClaimActions.Clear();

                // Set the "role" claim type to be the "extension_DelegatedUserManagementRole" user attribute.
                options.TokenValidationParameters.RoleClaimType = b2cGraphService.GetUserAttributeClaimName(Constants.UserAttributes.DelegatedUserManagementRole);
            });

            services.AddRazorPages();
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
