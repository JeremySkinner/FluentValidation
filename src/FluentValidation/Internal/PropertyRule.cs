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
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using Validators;

	/// <summary>
	/// Defines a rule associated with a property.
	/// </summary>
	internal class PropertyRule<T, TProperty> : RuleBase<T, TProperty, TProperty>, IValidationRuleInternal<T, TProperty> {

		public PropertyRule(MemberInfo member, Func<T, TProperty> propertyFunc, LambdaExpression expression, Func<CascadeMode> cascadeModeThunk, Type typeToValidate)
			: base(member, propertyFunc, expression, cascadeModeThunk, typeToValidate) {
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		public static PropertyRule<T, TProperty> Create(Expression<Func<T, TProperty>> expression, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache);
			return new PropertyRule<T, TProperty>(member, x => compiled(x), expression, cascadeModeThunk, typeof(TProperty));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		internal static PropertyRule<T, TProperty> Create<TOld>(Expression<Func<T, TOld>> expression, Func<TOld, TProperty> transformer, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache);

			TProperty PropertyFunc(T instance)
				=> transformer(compiled(instance));

			return new PropertyRule<T, TProperty>(member, PropertyFunc, expression, cascadeModeThunk, typeof(TOld));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		internal static PropertyRule<T, TProperty> Create<TOld>(Expression<Func<T, TOld>> expression, Func<T, TOld, TProperty> transformer, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache);

			TProperty PropertyFunc(T instance)
				=> transformer(instance, compiled(instance));

			return new PropertyRule<T, TProperty>(member, PropertyFunc, expression, cascadeModeThunk, typeof(TOld));
		}

		/// <summary>
		/// Performs validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public virtual void Validate(ValidationContext<T> context) {
			string displayName = GetDisplayName(context);

			if (PropertyName == null && displayName == null) {
				//No name has been specified. Assume this is a model-level rule, so we should use empty string instead.
				displayName = string.Empty;
			}

			// Construct the full name of the property, taking into account overriden property names and the chain (if we're in a nested validator)
			string propertyName = context.PropertyChain.BuildPropertyName(PropertyName ?? displayName);

			// Ensure that this rule is allowed to run.
			// The validatselector has the opportunity to veto this before any of the validators execute.
			if (!context.Selector.CanExecute(this, propertyName, context)) {
				return;
			}

			if (Condition != null) {
				if (!Condition(context)) {
					return;
				}
			}

			if (AsyncCondition != null) {
				throw new AsyncValidatorInvokedSynchronouslyException();
			}

			var cascade = CascadeMode;
			var accessor = new Lazy<TProperty>(() => PropertyFunc(context.InstanceToValidate), LazyThreadSafetyMode.None);
			var totalFailures = context.Failures.Count;
			context.InitializeForPropertyValidator(propertyName, GetDisplayName, PropertyName);

			// Invoke each validator and collect its results.
			foreach (var step in Components) {
				context.MessageFormatter.Reset();

				if (!step.InvokeCondition(context)) {
					continue;
				}

				if (step.HasAsyncCondition) {
					throw new AsyncValidatorInvokedSynchronouslyException();
				}

				if (step.ShouldValidateAsynchronously(context)) {
					throw new AsyncValidatorInvokedSynchronouslyException();
				}
				else {
					InvokePropertyValidator(context, accessor, propertyName, step);
				}

				// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
				// then don't continue to the next rule
#pragma warning disable 618
				if (context.Failures.Count > totalFailures && (cascade == CascadeMode.StopOnFirstFailure || cascade == CascadeMode.Stop)) {
#pragma warning restore 618
					break;
				}
			}

			if (context.Failures.Count > totalFailures) {
				// Callback if there has been at least one property validator failed.
				var failuresThisRound = context.Failures.Skip(totalFailures).ToList();
#pragma warning disable 618
				OnFailure?.Invoke(context.InstanceToValidate, failuresThisRound);
#pragma warning restore 618
			}
			else if(DependentRules != null) {
				foreach (var dependentRule in DependentRules) {
					dependentRule.Validate(context);
				}
			}
		}

		/// <summary>
		/// Performs asynchronous validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <param name="cancellation"></param>
		/// <returns>A collection of validation failures</returns>
		public virtual async Task ValidateAsync(ValidationContext<T> context, CancellationToken cancellation) {
			if (!context.IsAsync) {
				context.IsAsync = true;
			}

			string displayName = GetDisplayName(context);

			if (PropertyName == null && displayName == null) {
				//No name has been specified. Assume this is a model-level rule, so we should use empty string instead.
				displayName = string.Empty;
			}

			// Construct the full name of the property, taking into account overriden property names and the chain (if we're in a nested validator)
			string propertyName = context.PropertyChain.BuildPropertyName(PropertyName ?? displayName);

			// Ensure that this rule is allowed to run.
			// The validatselector has the opportunity to veto this before any of the validators execute.
			if (!context.Selector.CanExecute(this, propertyName, context)) {
				return;
			}

			if (Condition != null) {
				if (!Condition(context)) {
					return;
				}
			}

			if (AsyncCondition != null) {
				if (! await AsyncCondition(context, cancellation)) {
					return;
				}
			}

			var cascade = CascadeMode;
			var accessor = new Lazy<TProperty>(() => PropertyFunc(context.InstanceToValidate), LazyThreadSafetyMode.None);
			var totalFailures = context.Failures.Count;
			context.InitializeForPropertyValidator(propertyName, GetDisplayName, PropertyName);

			// Invoke each validator and collect its results.
			foreach (var validator in Components) {
				cancellation.ThrowIfCancellationRequested();
				context.MessageFormatter.Reset();

				if (!validator.InvokeCondition(context)) {
					continue;
				}

				if (!await validator.InvokeAsyncCondition(context, cancellation)) {
					continue;
				};

				if (validator.ShouldValidateAsynchronously(context)) {
					await InvokePropertyValidatorAsync(context, accessor, propertyName, validator, cancellation);
				}
				else {
					InvokePropertyValidator(context, accessor, propertyName, validator);
				}

				// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
				// then don't continue to the next rule
#pragma warning disable 618
				if (context.Failures.Count > totalFailures && (cascade == CascadeMode.StopOnFirstFailure || cascade == CascadeMode.Stop)) {
#pragma warning restore 618
					break;
				}
			}

			if (context.Failures.Count > totalFailures) {
				var failuresThisRound = context.Failures.Skip(totalFailures).ToList();
#pragma warning disable 618
				OnFailure?.Invoke(context.InstanceToValidate, failuresThisRound);
#pragma warning restore 618
			}
			else if (DependentRules != null) {
				foreach (var dependentRule in DependentRules) {
					cancellation.ThrowIfCancellationRequested();
					await dependentRule.ValidateAsync(context, cancellation);
				}
			}
		}

		private async Task InvokePropertyValidatorAsync(ValidationContext<T> context, Lazy<TProperty> accessor, string propertyName, RuleComponent<T,TProperty> component, CancellationToken cancellation) {
			bool valid = await component.ValidateAsync(context, accessor.Value, cancellation);

			if (!valid) {
				PrepareMessageFormatterForValidationError(context, accessor.Value);
				var failure = CreateValidationError(context, accessor.Value, component);
#pragma warning disable 618
				component.OnFailure?.Invoke(context.InstanceToValidate, context, accessor.Value, failure.ErrorMessage);
#pragma warning restore 618
				context.Failures.Add(failure);
			}
		}

		private protected void InvokePropertyValidator(ValidationContext<T> context, Lazy<TProperty> accessor, string propertyName, RuleComponent<T, TProperty> component) {
			bool valid = component.Validate(context, accessor.Value);

			if (!valid) {
				PrepareMessageFormatterForValidationError(context, accessor.Value);
				var failure = CreateValidationError(context, accessor.Value, component);
#pragma warning disable 618
				component.OnFailure?.Invoke(context.InstanceToValidate, context, accessor.Value, failure.ErrorMessage);
#pragma warning restore 618
				context.Failures.Add(failure);
			}
		}

		void IValidationRuleInternal<T>.AddDependentRules(IEnumerable<IValidationRuleInternal<T>> rules) {
			if (DependentRules == null) DependentRules = new();
			DependentRules.AddRange(rules);
		}
	}
}
