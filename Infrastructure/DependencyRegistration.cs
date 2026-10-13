using Core.BaseRepository;

namespace ImportandExports.Infrastructure
{
    public static class DependencyRegistration
    {
        public static void RegisterDependencies(IServiceCollection services)
        {
            RegisterRepositories(services);
            RegisterServices(services);
            RegisterFactories(services);
            RegisterUnitOfWork(services);
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            //services.AddScoped<IRepositoryBase<Customer>, RepositoryBase<Customer>>();
           
        }

        private static void RegisterServices(IServiceCollection services)
        {
          //  services.AddScoped<ICustomerService, CustomerService>();
          
        }

        private static void RegisterFactories(IServiceCollection services)
        {
          // services.AddScoped<IProductFactory, ProductFactory>();
          
        }

        private static void RegisterUnitOfWork(IServiceCollection services)
        {
           // services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
