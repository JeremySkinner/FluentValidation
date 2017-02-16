﻿using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentValidation.AspNetCore {
	using System;
	using System.Reflection;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Options;
	using FluentValidation;
	using System.Linq;
	using System.Collections.Generic;
	public static class FluentValidationMvcExtensions {
		/// <summary>
		///     Adds Fluent Validation services to the specified
		///     <see cref="T:Microsoft.Extensions.DependencyInjection.IMvcBuilder" />.
		/// </summary>
		/// <returns>
		///     An <see cref="T:Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder" /> that can be used to further configure the
		///     MVC services.
		/// </returns>
		public static IMvcCoreBuilder AddFluentValidation(this IMvcCoreBuilder mvcBuilder, Action<FluentValidationMvcConfiguration> configurationExpression=null) {
			var expr = configurationExpression ?? delegate { };
			var config = new FluentValidationMvcConfiguration();

			expr(config);

			if (config.AssembliesToRegister.Count > 0)
			{
				RegisterTypes(config.AssembliesToRegister, mvcBuilder.Services);
			}

			RegisterServices(mvcBuilder.Services, config);
			// clear all model validation providers since fluent validation will be handling everything

			mvcBuilder.AddMvcOptions(
				options => {
					options.ModelValidatorProviders.Clear();
				});

			return mvcBuilder;
		}

		private static void RegisterServices(IServiceCollection services, FluentValidationMvcConfiguration config) {
			if (config.ValidatorFactory != null) {
				// Allow user to register their own IValidatorFactory instance, before falling back to try resolving by Type. 
				var factory = config.ValidatorFactory;
				services.Add(ServiceDescriptor.Singleton(s => factory));
			}
			else {
				services.Add(ServiceDescriptor.Singleton(typeof(IValidatorFactory), config.ValidatorFactoryType ?? typeof(ServiceProviderValidatorFactory)));
			}

			services.Add(ServiceDescriptor.Singleton<IObjectModelValidator, FluentValidationObjectModelValidator>(s => {
				var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
				var metadataProvider = s.GetRequiredService<IModelMetadataProvider>();
				return new FluentValidationObjectModelValidator(metadataProvider, options.ModelValidatorProviders, s.GetRequiredService<IValidatorFactory>());
			}));

			if (config.ClientsideEnabled)
			{
				services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcViewOptions>, FluentValidationViewOptionsSetup>(s =>
					{
						return new FluentValidationViewOptionsSetup(s.GetService<IValidatorFactory>(), config.ClientsideConfig);
					}));
			}


		}

		/// <summary>
		///     Adds Fluent Validation services to the specified
		///     <see cref="T:Microsoft.Extensions.DependencyInjection.IMvcBuilder" />.
		/// </summary>
		/// <returns>
		///     An <see cref="T:Microsoft.Extensions.DependencyInjection.IMvcBuilder" /> that can be used to further configure the
		///     MVC services.
		/// </returns>
   		public static IMvcBuilder AddFluentValidation(this IMvcBuilder mvcBuilder, Action<FluentValidationMvcConfiguration> configurationExpression = null) {
			// add all IValidator to MVC's service provider

		    var expr = configurationExpression ?? delegate { };
            var config = new FluentValidationMvcConfiguration();

		    expr(config);

			if (config.AssembliesToRegister.Count > 0) {
				RegisterTypes(config.AssembliesToRegister, mvcBuilder.Services);
			}

			RegisterServices(mvcBuilder.Services, config);

			// clear all model validation providers since fluent validation will be handling everything
			mvcBuilder.AddMvcOptions(
			    options => {
			        options.ModelValidatorProviders.Clear();
                });

            return mvcBuilder;
		}
				
		/// <summary>
		///     Adds Fluent Validation services to the specified
		///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />. Ensure you first add MVC via a call to <c>AddMvc()</c>.
		/// </summary>
		/// <returns>
		///     An <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> that can be used to further configure the
		///     ASP.NET Core services.
		/// </returns>
		public static IServiceCollection AddFluentValidation(this IServiceCollection services, Action<FluentValidationMvcConfiguration> configurationExpression = null)
		{
    			var mvcBuilder = services.BuildServiceProvider().GetRequiredService<IMvcBuilder>();
    			AddFluentValidation(mvcBuilder, configurationExpression);
    			return services;
		}		

		private static void RegisterTypes(IEnumerable<Assembly> assembliesToRegister, IServiceCollection services) {
			var openGenericType = typeof(IValidator<>);

			var query = from a in assembliesToRegister.Distinct()
                        from type in a.GetTypes().Where(c => !(c.GetTypeInfo().IsAbstract || c.GetTypeInfo().IsGenericTypeDefinition))
						let interfaces = type.GetInterfaces()
						let genericInterfaces = interfaces.Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == openGenericType)
						let matchingInterface = genericInterfaces.FirstOrDefault()
						where matchingInterface != null
						select new { matchingInterface, type };

			foreach (var pair in query) {
				services.Add(ServiceDescriptor.Transient(pair.matchingInterface, pair.type));
			}
		}
	}

	internal class FluentValidationViewOptionsSetup : IConfigureOptions<MvcViewOptions>
	{
		private readonly IValidatorFactory _factory;
		private readonly Action<FluentValidationClientModelValidatorProvider> _action;

		public FluentValidationViewOptionsSetup(IValidatorFactory factory, Action<FluentValidationClientModelValidatorProvider> action)
		{
			_factory = factory;
			_action = action;
		}

		public void Configure(MvcViewOptions options)
		{
			var provider = new FluentValidationClientModelValidatorProvider(_factory);
			_action(provider);
			options.ClientModelValidatorProviders.Add(provider);
		}
	}

	public class FluentValidationMvcConfiguration {
	    public Type ValidatorFactoryType { get; set; }
		public IValidatorFactory ValidatorFactory { get; set; }
	    internal List<Assembly> AssembliesToRegister { get; } = new List<Assembly>();
	    internal bool ClientsideEnabled = true;
	    internal Action<FluentValidationClientModelValidatorProvider> ClientsideConfig = x => {};

	    public FluentValidationMvcConfiguration RegisterValidatorsFromAssemblyContaining<T>() {
		    return RegisterValidatorsFromAssemblyContaining(typeof(T));
	    }

	    public FluentValidationMvcConfiguration RegisterValidatorsFromAssemblyContaining(Type type) {
		    return RegisterValidatorsFromAssembly(type.GetTypeInfo().Assembly);
	    }

	    public FluentValidationMvcConfiguration RegisterValidatorsFromAssembly(Assembly assembly) {
		    ValidatorFactoryType = typeof(ServiceProviderValidatorFactory);
		    AssembliesToRegister.Add(assembly);
		    return this;
	    }

	    public FluentValidationMvcConfiguration ConfigureClientsideValidation(Action<FluentValidationClientModelValidatorProvider> clientsideConfig=null, bool enabled=true) {
		    if (clientsideConfig != null) {
			    ClientsideConfig = clientsideConfig;
		    }
		    ClientsideEnabled = enabled;
		    return this;
	    }
		
	}
}
