﻿namespace G2Libsys
{
    #region Namespaces

    using System;
    using System.Collections.Generic;
    using System.Text;
    using G2Libsys.Services;
    using Microsoft.Extensions.DependencyInjection;

    #endregion

    /// <summary>
    /// Inversion of Control
    /// </summary>
    public static class IoC
    {
        /// <summary>
        /// Access service collection
        /// </summary>
        public static ServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Setup service provider
        /// NOTE: Must be initiated at the start of the application
        /// </summary>
        public static void SetUp()
        {
            // Build the serviceprovider with the provided service collection: Services()
            ServiceProvider = Services().BuildServiceProvider();
        }

        /// <summary>
        /// Create service collection
        /// </summary>
        private static IServiceCollection Services()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddScoped<IDialogService, DialogService>();
            services.AddTransient<IFileService, FileService>();
            services.AddScoped<ILoansService, LoansServices>();

            return services;
        }
    }
}
