#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

using FluentValidation.Internal;

namespace FluentValidation {
	using System;
	using System.Collections.Generic;
	using Microsoft.Extensions.DependencyInjection;
	using Validators;

	/// <summary>
	/// Extension methods for working with a Service Provider.
	/// </summary>
	public static class DependencyInjectionExtensions {

		/// <summary>
		/// Gets the service provider associated with the validation context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static IServiceProvider GetServiceProvider(this IValidationContext context)
			=> Get(context.RootContextData);

		/// <summary>
		/// Gets the service provider associated with the validation context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static IServiceProvider GetServiceProvider<T,TProperty>(this MessageBuilderContext<T,TProperty> context)
			=> Get(context.ParentContext.RootContextData);

		private static IServiceProvider Get(IDictionary<string, object> rootContextData) {
			if (rootContextData.TryGetValue("_FV_ServiceProvider", out var sp)) {
				if (sp is IServiceProvider serviceProvider) {
					return serviceProvider;
				}
			}

			throw new InvalidOperationException("The service provider has not been configured to work with FluentValidation. Making use of InjectValidator or GetServiceProvider is only supported when using the automatic MVC integration.");
		}

		/// <summary>
		/// Sets the service provider associated with the validation context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="serviceProvider"></param>
		public static void SetServiceProvider(this IValidationContext context, IServiceProvider serviceProvider) {
			context.RootContextData["_FV_ServiceProvider"] = serviceProvider;
		}

		/// <summary>
		/// Uses the Service Provider to inject the default validator for the property type.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="ruleSets"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, IChildValidatorAdaptor> InjectValidator<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, params string[] ruleSets) {
			return ruleBuilder.InjectValidator((s, ctx) => s.GetService<IValidatorFactory>().GetValidator<TProperty>(), ruleSets);
		}

		/// <summary>
		/// Uses the Service Provider to inject the default validator for the property type.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="callback"></param>
		/// <param name="ruleSets"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, IChildValidatorAdaptor> InjectValidator<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Func<IServiceProvider, ValidationContext<T>, IValidator<TProperty>> callback, params string[] ruleSets) {
			var adaptor = new ChildValidatorAdaptor<T,TProperty>((context, _) => {
				var serviceProvider = context.GetServiceProvider();
				var validator = callback(serviceProvider, context);
				return validator;
			}, typeof(IValidator<TProperty>));

			var rb = (RuleBuilder<T, TProperty>) ruleBuilder;
			rb.Rule.AddAsyncValidator(adaptor, adaptor);
			return new ComponentRuleBuilder<T, TProperty, IChildValidatorAdaptor>(rb.Rule, rb.ParentValidator);
		}
	}
}
