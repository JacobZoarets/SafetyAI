using System;
using System.Collections.Concurrent;
using System.Web;
using SafetyAI.Data.Context;
using SafetyAI.Data.Interfaces;
using SafetyAI.Data.Repositories;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Web.App_Start
{
    public static class DependencyConfig
    {
        private static readonly object _lock = new object();
        private static readonly ConcurrentDictionary<Type, object> _services = new ConcurrentDictionary<Type, object>();

        public static void RegisterDependencies()
        {
            // Register services - this is a simple service locator pattern for .NET Framework 4.8
            // In a production environment, consider using a proper DI container like Unity or Autofac
            
            // Register DbContext as singleton for application lifetime
            _services.TryAdd(typeof(SafetyAIDbContext), new SafetyAIDbContext());
            
            // Register Unit of Work
            _services.TryAdd(typeof(IUnitOfWork), new Lazy<IUnitOfWork>(() => new UnitOfWork(GetService<SafetyAIDbContext>())));
            
            // Register individual repositories
            _services.TryAdd(typeof(ISafetyReportRepository), new Lazy<ISafetyReportRepository>(() => new SafetyReportRepository(GetService<SafetyAIDbContext>())));
            _services.TryAdd(typeof(IAnalysisResultRepository), new Lazy<IAnalysisResultRepository>(() => new AnalysisResultRepository(GetService<SafetyAIDbContext>())));
            _services.TryAdd(typeof(IRecommendationRepository), new Lazy<IRecommendationRepository>(() => new RecommendationRepository(GetService<SafetyAIDbContext>())));
            
            // Register services
            _services.TryAdd(typeof(IGeminiAPIClient), new Lazy<IGeminiAPIClient>(() => new SafetyAI.Services.Implementation.GeminiAPIClient()));
            _services.TryAdd(typeof(IFileValidator), new Lazy<IFileValidator>(() => new SafetyAI.Services.Implementation.FileValidator()));

            _services.TryAdd(typeof(IDocumentProcessor), new Lazy<IDocumentProcessor>(() => new SafetyAI.Services.Implementation.DocumentProcessor(
                GetService<IGeminiAPIClient>(), 
                GetService<IFileValidator>())));
            _services.TryAdd(typeof(IChatService), new Lazy<IChatService>(() => new SafetyAI.Services.Implementation.ChatService(
                GetService<IGeminiAPIClient>())));
        }

        public static T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            
            if (_services.TryGetValue(serviceType, out var service))
            {
                if (service is Lazy<T> lazyService)
                {
                    return lazyService.Value;
                }
                return service as T;
            }

            // Fallback for types not explicitly registered
            if (serviceType == typeof(SafetyAIDbContext))
            {
                return GetDbContext() as T;
            }

            return null;
        }

        public static SafetyAIDbContext GetDbContext()
        {
            return GetService<SafetyAIDbContext>() ?? new SafetyAIDbContext();
        }

        public static IUnitOfWork GetUnitOfWork()
        {
            return GetService<IUnitOfWork>() ?? new UnitOfWork(GetDbContext());
        }

        public static void DisposeServices()
        {
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (service is Lazy<IDisposable> lazyDisposable && lazyDisposable.IsValueCreated)
                {
                    lazyDisposable.Value.Dispose();
                }
            }
            _services.Clear();
        }
    }
}