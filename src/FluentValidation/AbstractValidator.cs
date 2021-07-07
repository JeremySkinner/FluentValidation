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
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Results;

	/// <summary>
	/// Base class for object validators.
	/// </summary>
	/// <typeparam name="T">The type of the object being validated</typeparam>
	public abstract class AbstractValidator<T> : IValidator<T>, IEnumerable<IValidationRule> {
		internal TrackingCollection<IValidationRuleInternal<T>> Rules { get; } = new();
		private Func<CascadeMode> _cascadeMode = () => ValidatorOptions.Global.CascadeMode;

		/// <summary>
		/// Sets the cascade mode for all rules within this validator.
		/// </summary>
		public CascadeMode CascadeMode {
			get => _cascadeMode();
			set => _cascadeMode = () => value;
		}

		ValidationResult IValidator.Validate(IValidationContext context) {
			context.Guard("Cannot pass null to Validate", nameof(context));
			return Validate(ValidationContext<T>.GetFromNonGenericContext(context));
		}

		Task<ValidationResult> IValidator.ValidateAsync(IValidationContext context, CancellationToken cancellation) {
			context.Guard("Cannot pass null to Validate", nameof(context));
			return ValidateAsync(ValidationContext<T>.GetFromNonGenericContext(context), cancellation);
		}

		/// <summary>
		/// Validates the specified instance
		/// </summary>
		/// <param name="instance">The object to validate</param>
		/// <returns>A ValidationResult object containing any validation failures</returns>
		public ValidationResult Validate(T instance) {
			return Validate(new ValidationContext<T>(instance, new PropertyChain(), ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory()));
		}

		/// <summary>
		/// Validates the specified instance asynchronously
		/// </summary>
		/// <param name="instance">The object to validate</param>
		/// <param name="cancellation">Cancellation token</param>
		/// <returns>A ValidationResult object containing any validation failures</returns>
		public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = new CancellationToken()) {
			return ValidateAsync(new ValidationContext<T>(instance, new PropertyChain(), ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory()), cancellation);
		}

		/// <summary>
		/// Validates the specified instance.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A ValidationResult object containing any validation failures.</returns>
		public virtual ValidationResult Validate(ValidationContext<T> context) {
			context.Guard("Cannot pass null to Validate.", nameof(context));

			var result = new ValidationResult(context.Failures);
			bool shouldContinue = PreValidate(context, result);

			if (!shouldContinue) {
				if (!result.IsValid && context.ThrowOnFailures) {
					RaiseValidationException(context, result);
				}

				return result;
			}

			EnsureInstanceNotNull(context.InstanceToValidate);

			try {
				foreach (var rule in Rules) {
					rule.Validate(context);

					if (CascadeMode == CascadeMode.Stop && result.Errors.Count > 0) {
						// Bail out if we're "failing-fast".
						// Check for > 0 rather than == 1 because a rule chain may have overridden the Stop behaviour to Continue
						// meaning that although the first rule failed, it actually generated 2 failures if there were 2 validators
						// in the chain.
						break;
					}
				}
			}
			catch (AsyncValidatorInvokedSynchronouslyException) {
				bool wasInvokedByMvc = context.RootContextData.ContainsKey("InvokedByMvc");
				throw new AsyncValidatorInvokedSynchronouslyException(GetType(), wasInvokedByMvc);
			}

			SetExecutedRulesets(result, context);

			if (!result.IsValid && context.ThrowOnFailures) {
				RaiseValidationException(context, result);
			}

			return result;
		}

		/// <summary>
		/// Validates the specified instance asynchronously.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <param name="cancellation">Cancellation token</param>
		/// <returns>A ValidationResult object containing any validation failures.</returns>
		public virtual async Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = new CancellationToken()) {
			context.Guard("Cannot pass null to Validate", nameof(context));
			context.IsAsync = true;

			var result = new ValidationResult(context.Failures);
			bool shouldContinue = PreValidate(context, result);

			if (!shouldContinue) {
				if (!result.IsValid && context.ThrowOnFailures) {
					RaiseValidationException(context, result);
				}

				return result;
			}

			EnsureInstanceNotNull(context.InstanceToValidate);

			foreach (var rule in Rules) {
				cancellation.ThrowIfCancellationRequested();
				await rule.ValidateAsync(context, cancellation);

				if (CascadeMode == CascadeMode.Stop && result.Errors.Count > 0) {
					// Bail out if we're "failing-fast".
					// Check for > 0 rather than == 1 because a rule chain may have overridden the Stop behaviour to Continue
					// meaning that although the first rule failed, it actually generated 2 failures if there were 2 validators
					// in the chain.
					break;
				}
			}

			SetExecutedRulesets(result, context);

			if (!result.IsValid && context.ThrowOnFailures) {
				RaiseValidationException(context, result);
			}

			return result;
		}

		private void SetExecutedRulesets(ValidationResult result, ValidationContext<T> context) {
			var executed = context.RootContextData.GetOrAdd("_FV_RuleSetsExecuted", () => new HashSet<string>{RulesetValidatorSelector.DefaultRuleSetName});
			result.RuleSetsExecuted = executed.ToArray();
		}

		/// <summary>
		/// Creates a <see cref="IValidatorDescriptor" /> that can be used to obtain metadata about the current validator.
		/// </summary>
		public virtual IValidatorDescriptor CreateDescriptor() {
			return new ValidatorDescriptor<T>(Rules);
		}

		bool IValidator.CanValidateInstancesOfType(Type type) {
			if (type == null) throw new ArgumentNullException(nameof(type));
			return typeof(T).IsAssignableFrom(type);
		}

		/// <summary>
		/// Defines a validation rule for a specify property.
		/// </summary>
		/// <example>
		/// RuleFor(x => x.Surname)...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <param name="expression">The expression representing the property to validate</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression) {
			expression.Guard("Cannot pass null to RuleFor", nameof(expression));
			var rule = PropertyRule<T, TProperty>.Create(expression, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TProperty>(rule, this);
		}

		/// <summary>
		/// Defines a validation rule for a specify property and transform it to a different type.
		/// </summary>
		/// <example>
		/// Transform(x => x.OrderNumber, to: orderNumber => orderNumber.ToString())...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="from">The expression representing the property to transform</param>
		/// <param name="to">Function to transform the property value into a different type</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitial<T, TTransformed> Transform<TProperty, TTransformed>(Expression<Func<T, TProperty>> from, Func<TProperty, TTransformed> to) {
			from.Guard("Cannot pass null to Transform", nameof(from));
			var rule = PropertyRule<T, TTransformed>.Create(from, to, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TTransformed>(rule, this);
		}

		/// <summary>
		/// Defines a validation rule for a specify property and transform it to a different type.
		/// </summary>
		/// <example>
		/// Transform(x => x.OrderNumber, to: orderNumber => orderNumber.ToString())...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="from">The expression representing the property to transform</param>
		/// <param name="to">Function to transform the property value into a different type</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitial<T, TTransformed> Transform<TProperty, TTransformed>(Expression<Func<T, TProperty>> from, Func<T, TProperty, TTransformed> to) {
			from.Guard("Cannot pass null to Transform", nameof(from));
			var rule = PropertyRule<T, TTransformed>.Create(from, to, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TTransformed>(rule, this);
		}


		/// <summary>
		/// Invokes a rule for each item in the collection.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitialCollection<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression) {
			expression.Guard("Cannot pass null to RuleForEach", nameof(expression));
			var rule = CollectionPropertyRule<T, TElement>.Create(expression, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TElement>(rule, this);
		}

		/// <summary>
		/// Invokes a rule for each item in the collection, transforming the element from one type to another.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <param name="to">Function to transform the collection element into a different type</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitialCollection<T, TTransformed> TransformForEach<TElement, TTransformed>(Expression<Func<T, IEnumerable<TElement>>> expression, Func<TElement, TTransformed> to) {
			expression.Guard("Cannot pass null to RuleForEach", nameof(expression));
			var rule = CollectionPropertyRule<T, TTransformed>.CreateTransformed<TElement>(expression, to, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TTransformed>(rule, this);
		}

		/// <summary>
		/// Invokes a rule for each item in the collection, transforming the element from one type to another.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <param name="to">Function to transform the collection element into a different type</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitialCollection<T, TTransformed> TransformForEach<TElement, TTransformed>(Expression<Func<T, IEnumerable<TElement>>> expression, Func<T, TElement, TTransformed> to) {
			expression.Guard("Cannot pass null to RuleForEach", nameof(expression));
			var rule = CollectionPropertyRule<T, TTransformed>.CreateTransformed<TElement>(expression, to, () => CascadeMode);
			Rules.Add(rule);
			return new RuleBuilder<T, TTransformed>(rule, this);
		}

		/// <summary>
		/// Defines a RuleSet that can be used to group together several validators.
		/// </summary>
		/// <param name="ruleSetName">The name of the ruleset.</param>
		/// <param name="action">Action that encapsulates the rules in the ruleset.</param>
		public void RuleSet(string ruleSetName, Action action) {
			ruleSetName.Guard("A name must be specified when calling RuleSet.", nameof(ruleSetName));
			action.Guard("A ruleset definition must be specified when calling RuleSet.", nameof(action));

			var ruleSetNames = ruleSetName.Split(',', ';')
				.Select(x => x.Trim())
				.ToArray();

			using (Rules.OnItemAdded(r => r.RuleSets = ruleSetNames)) {
				action();
			}
		}

		/// <summary>
		/// Defines a condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public IConditionBuilder When(Func<T, bool> predicate, Action action) {
			return When((x, ctx) => predicate(x), action);
		}

		/// <summary>
		/// Defines a condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public IConditionBuilder When(Func<T, ValidationContext<T>, bool> predicate, Action action) {
			return new ConditionBuilder<T>(Rules).When(predicate, action);
		}

		/// <summary>
		/// Defines an inverse condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public IConditionBuilder Unless(Func<T, bool> predicate, Action action) {
			return Unless((x, ctx) => predicate(x), action);
		}

		/// <summary>
		/// Defines an inverse condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public IConditionBuilder Unless(Func<T, ValidationContext<T>, bool> predicate, Action action) {
			return new ConditionBuilder<T>(Rules).Unless(predicate, action);
		}

		/// <summary>
		/// Defines an asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public IConditionBuilder WhenAsync(Func<T, CancellationToken, Task<bool>> predicate, Action action) {
			return WhenAsync((x, ctx, cancel) => predicate(x, cancel), action);
		}

		/// <summary>
		/// Defines an asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public IConditionBuilder WhenAsync(Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, Action action) {
			return new AsyncConditionBuilder<T>(Rules).WhenAsync(predicate, action);
		}

		/// <summary>
		/// Defines an inverse asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public IConditionBuilder UnlessAsync(Func<T, CancellationToken, Task<bool>> predicate, Action action) {
			return UnlessAsync((x, ctx, cancel) => predicate(x, cancel), action);
		}

		/// <summary>
		/// Defines an inverse asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public IConditionBuilder UnlessAsync(Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, Action action) {
			return new AsyncConditionBuilder<T>(Rules).UnlessAsync(predicate, action);
		}

		/// <summary>
		/// Includes the rules from the specified validator
		/// </summary>
		public void Include(IValidator<T> rulesToInclude) {
			rulesToInclude.Guard("Cannot pass null to Include", nameof(rulesToInclude));
			var rule = IncludeRule<T>.Create(rulesToInclude, () => CascadeMode);
			Rules.Add(rule);
		}

		/// <summary>
		/// Includes the rules from the specified validator
		/// </summary>
		public void Include<TValidator>(Func<T, TValidator> rulesToInclude) where TValidator : IValidator<T> {
			rulesToInclude.Guard("Cannot pass null to Include", nameof(rulesToInclude));
			var rule = IncludeRule<T>.Create(rulesToInclude, () => CascadeMode);
			Rules.Add(rule);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection of validation rules.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public IEnumerator<IValidationRule> GetEnumerator() {
			return Rules.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <summary>
		/// Throws an exception if the instance being validated is null.
		/// </summary>
		/// <param name="instanceToValidate"></param>
		protected virtual void EnsureInstanceNotNull(object instanceToValidate) {
			instanceToValidate.Guard("Cannot pass null model to Validate.", nameof(instanceToValidate));
		}

		/// <summary>
		/// Determines if validation should occur and provides a means to modify the context and ValidationResult prior to execution.
		/// If this method returns false, then the ValidationResult is immediately returned from Validate/ValidateAsync.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		protected virtual bool PreValidate(ValidationContext<T> context, ValidationResult result) {
			return true;
		}

		/// <summary>
		/// Throws a ValidationException. This method will only be called if the validator has been configured
		/// to throw exceptions if validation fails. The default behaviour is not to throw an exception.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="result"></param>
		/// <exception cref="ValidationException"></exception>
		protected virtual void RaiseValidationException(ValidationContext<T> context, ValidationResult result) {
			throw new ValidationException(result.Errors);
		}
	}
}
