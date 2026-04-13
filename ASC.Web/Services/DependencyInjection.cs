using ASC.DataAccess;
using ASC.Solution.Services;
using ASC.Web.Configuration;
using ASC.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ASC.DataAccess.ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddOptions();
            services.Configure<ApplicationSettings>(config.GetSection("AppSettings"));

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = config["Authentication:Google:ClientId"];
                    options.ClientSecret = config["Authentication:Google:ClientSecret"];
                });

            return services;
        }

        public static IServiceCollection AddMyDependencyGroup(this IServiceCollection services)
        {
            services.AddScoped<DbContext, ASC.DataAccess.ApplicationDbContext>();

            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ASC.DataAccess.ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, AuthMessageSender>();
            services.AddSingleton<IIdentitySeed, IdentitySeed>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddMemoryCache();
            services.AddScoped<INavigationCacheOperations, NavigationCacheOperations>();

            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddHttpContextAccessor();

            services.AddRazorPages();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddControllersWithViews();

            return services;
        }
    }
}