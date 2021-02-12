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

namespace FluentValidation.Internal {
	using System;
	using System.Collections.Generic;
	using Validators;

	/// <summary>
	/// Builds a validation rule and constructs a validator.
	/// </summary>
	/// <typeparam name="T">Type of object being validated</typeparam>
	/// <typeparam name="TProperty">Type of property being validated</typeparam>
	internal class RuleBuilder<T, TProperty> : IRuleBuilderInitial<T, TProperty>, IRuleBuilderInitialCollection<T,TProperty> {

		/// <summary>
		/// The rule being created by this RuleBuilder.
		/// </summary>
		public IValidationRuleInternal<T, TProperty> Rule { get; }

		/// <summary>
		/// Parent validator
		/// </summary>
		public AbstractValidator<T> ParentValidator { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="RuleBuilder{T,TProperty}">RuleBuilder</see> class.
		/// </summary>
		public RuleBuilder(IValidationRuleInternal<T, TProperty> rule, AbstractValidator<T> parent) {
			Rule = rule;
			ParentValidator = parent;
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> SetValidator<TValidator>(TValidator validator) where TValidator : IPropertyValidator<T,TProperty> {
			if (validator == null) throw new ArgumentNullException(nameof(validator));
			Rule.AddValidator(validator);
			return new ComponentRuleBuilder<T, TProperty, TValidator>(Rule, ParentValidator);
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> SetAsyncValidator<TValidator>(TValidator validator) where TValidator : IAsyncPropertyValidator<T, TProperty> {
			if (validator == null) throw new ArgumentNullException(nameof(validator));
			// See if the async validator supports synchronous execution too.
			IPropertyValidator<T, TProperty> fallback = validator as IPropertyValidator<T, TProperty>;
			Rule.AddAsyncValidator(validator, fallback);
			return new ComponentRuleBuilder<T, TProperty, TValidator>(Rule, ParentValidator);
		}

		public IRuleBuilderOptions<T, TProperty, IChildValidatorAdaptor> SetValidator(IValidator<TProperty> validator, params string[] ruleSets) {
			validator.Guard("Cannot pass a null validator to SetValidator", nameof(validator));
			var adaptor = new ChildValidatorAdaptor<T,TProperty>(validator, validator.GetType()) {
				RuleSets = ruleSets
			};
			// ChildValidatorAdaptor supports both sync and async execution.
			Rule.AddAsyncValidator(adaptor, adaptor);
			return new ComponentRuleBuilder<T, TProperty, IChildValidatorAdaptor>(Rule, ParentValidator);
		}

		public IRuleBuilderOptions<T, TProperty, IChildValidatorAdaptor> SetValidator<TValidator>(Func<T, TValidator> validatorProvider, params string[] ruleSets) where TValidator : IValidator<TProperty> {
			validatorProvider.Guard("Cannot pass a null validatorProvider to SetValidator", nameof(validatorProvider));
			var adaptor = new ChildValidatorAdaptor<T,TProperty>((context, _) => validatorProvider(context.InstanceToValidate), typeof (TValidator)) {
				RuleSets = ruleSets
			};
			// ChildValidatorAdaptor supports both sync and async execution.
			Rule.AddAsyncValidator(adaptor, adaptor);
			return new ComponentRuleBuilder<T, TProperty, IChildValidatorAdaptor>(Rule, ParentValidator);
		}

		public IRuleBuilderOptions<T, TProperty, IChildValidatorAdaptor> SetValidator<TValidator>(Func<T, TProperty, TValidator> validatorProvider, params string[] ruleSets) where TValidator : IValidator<TProperty> {
			validatorProvider.Guard("Cannot pass a null validatorProvider to SetValidator", nameof(validatorProvider));
			var adaptor = new ChildValidatorAdaptor<T,TProperty>((context, val) => validatorProvider(context.InstanceToValidate, val), typeof (TValidator)) {
				RuleSets = ruleSets
			};
			// ChildValidatorAdaptor supports both sync and async execution.
			Rule.AddAsyncValidator(adaptor, adaptor);
			return new ComponentRuleBuilder<T, TProperty, IChildValidatorAdaptor>(Rule, ParentValidator);
		}

		public void AddComponent(RuleComponent<T,TProperty> component) {
			Rule.Components.Add(component);
		}
	}

	internal class ComponentRuleBuilder<T, TProperty, TValidator> : RuleBuilder<T, TProperty>, IRuleBuilderOptions<T, TProperty, TValidator> {
		public ComponentRuleBuilder(IValidationRuleInternal<T, TProperty> rule, AbstractValidator<T> parent)
			: base(rule, parent) {
		}

		public IRuleBuilderOptions<T, TProperty, TValidator> DependentRules(Action action) {
			var dependencyContainer = new List<IValidationRuleInternal<T>>();
			// Capture any rules added to the parent validator inside this delegate.
			using (ParentValidator.Rules.Capture(dependencyContainer.Add)) {
				action();
			}

			if (Rule.RuleSets != null && Rule.RuleSets.Length > 0) {
				foreach (var dependentRule in dependencyContainer) {
					if (dependentRule is IValidationRule propRule && propRule.RuleSets == null) {
						propRule.RuleSets = Rule.RuleSets;
					}
				}
			}

			Rule.AddDependentRules(dependencyContainer);
			return this;
		}
	}
}
