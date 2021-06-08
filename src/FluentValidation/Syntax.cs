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

namespace FluentValidation {
	using System;
	using Internal;
	using Validators;

	/// <summary>
	/// Rule builder that starts the chain
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilderInitial<T, out TProperty> : IRuleBuilder<T, TProperty> {
	}

	/// <summary>
	/// Rule builder
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilder<T, out TProperty> {

		/// <summary>
		/// Associates a validator with this the property for this rule builder.
		/// </summary>
		/// <param name="validator">The validator to set</param>
		/// <returns></returns>
		IRuleBuilderOptions<T, TProperty> SetValidator(IPropertyValidator<T, TProperty> validator);

		/// <summary>
		/// Associates an async validator with this the property for this rule builder.
		/// </summary>
		/// <param name="validator">The validator to set</param>
		/// <returns></returns>
		IRuleBuilderOptions<T, TProperty> SetAsyncValidator(IAsyncPropertyValidator<T, TProperty> validator);

		/// <summary>
		/// Associates an instance of IValidator with the current property rule.
		/// </summary>
		/// <param name="validator">The validator to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator(IValidator<TProperty> validator, params string[] ruleSets);

		/// <summary>
		/// Associates a validator provider with the current property rule.
		/// </summary>
		/// <param name="validatorProvider">The validator provider to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator<TValidator>(Func<T, TValidator> validatorProvider, params string[] ruleSets)
			where TValidator : IValidator<TProperty>;

		/// <summary>
		/// Associates a validator provider with the current property rule.
		/// </summary>
		/// <param name="validatorProvider">The validator provider to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator<TValidator>(Func<T, TProperty, TValidator> validatorProvider, params string[] ruleSets)
			where TValidator : IValidator<TProperty>;
	}


	/// <summary>
	/// Rule builder
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilderOptions<T, out TProperty> : IRuleBuilder<T, TProperty> {
		/// <summary>
		/// Creates a scope for declaring dependent rules.
		/// </summary>
		IRuleBuilderOptions<T, TProperty> DependentRules(Action action);
	}

	/// <summary>
	/// Rule builder (for validators that only support conditions, but no other options)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	public interface IRuleBuilderOptionsConditions<T, out TProperty> : IRuleBuilder<T, TProperty> {
	}

	/// <summary>
	/// Rule builder that starts the chain for a child collection
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	public interface IRuleBuilderInitialCollection<T, TElement> : IRuleBuilder<T, TElement> {
	}

	/// <summary>
	/// Fluent interface for conditions (When/Unless/WhenAsync/UnlessAsync)
	/// </summary>
	public interface IConditionBuilder {
		/// <summary>
		/// Rules to be invoked if the condition fails.
		/// </summary>
		/// <param name="action"></param>
		void Otherwise(Action action);
	}

	internal interface IRuleBuilderInternal<T, out TProperty> {
		IValidationRule<T, TProperty> Rule { get; }
	}

}
