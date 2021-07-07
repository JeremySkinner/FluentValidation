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

	/// <summary>
	/// Rule definition for collection properties
	/// </summary>
	/// <typeparam name="TElement"></typeparam>
	/// <typeparam name="T"></typeparam>
	internal class CollectionPropertyRule<T, TElement> : RuleBase<T, IEnumerable<TElement>, TElement>, ICollectionRule<T, TElement>, IValidationRuleInternal<T, TElement> {
		/// <summary>
		/// Initializes new instance of the CollectionPropertyRule class
		/// </summary>
		public CollectionPropertyRule(MemberInfo member, Func<T, IEnumerable<TElement>> propertyFunc, LambdaExpression expression, Func<CascadeMode> cascadeModeThunk, Type typeToValidate)
			: base(member, propertyFunc, expression, cascadeModeThunk, typeToValidate) {
		}

		/// <summary>
		/// Filter that should include/exclude items in the collection.
		/// </summary>
		public Func<TElement, bool> Filter { get; set; }

		/// <summary>
		/// Constructs the indexer in the property name associated with the error message.
		/// By default this is "[" + index + "]"
		/// </summary>
		public Func<T, IEnumerable<TElement>, TElement, int, string> IndexBuilder { get; set; }

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		public static CollectionPropertyRule<T, TElement> Create(Expression<Func<T, IEnumerable<TElement>>> expression, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache, "FV_RuleForEach");
			return new CollectionPropertyRule<T, TElement>(member, x => compiled(x), expression, cascadeModeThunk, typeof(TElement));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		internal static CollectionPropertyRule<T, TElement> CreateTransformed<TOriginal>(Expression<Func<T, IEnumerable<TOriginal>>> expression, Func<TOriginal, TElement> transformer, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache, "FV_RuleForEach");

			IEnumerable<TElement> PropertyFunc(T instance) =>
				compiled(instance).Select(transformer);

			return new CollectionPropertyRule<T, TElement>(member, PropertyFunc, expression, cascadeModeThunk, typeof(TElement));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		internal static CollectionPropertyRule<T, TElement> CreateTransformed<TOriginal>(Expression<Func<T, IEnumerable<TOriginal>>> expression, Func<T, TOriginal, TElement> transformer, Func<CascadeMode> cascadeModeThunk, bool bypassCache = false) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression, bypassCache, "FV_RuleForEach");

			IEnumerable<TElement> PropertyFunc(T instance) {
				return compiled(instance).Select(element => transformer(instance, element));
			}

			return new CollectionPropertyRule<T, TElement>(member, PropertyFunc, expression, cascadeModeThunk, typeof(TOriginal));
		}

		void IValidationRuleInternal<T>.Validate(ValidationContext<T> context) {
			string displayName = GetDisplayName(context);

			if (PropertyName == null && displayName == null) {
				//No name has been specified. Assume this is a model-level rule, so we should use empty string instead.
				displayName = string.Empty;
			}

			// Construct the full name of the property, taking into account overriden property names and the chain (if we're in a nested validator)
			string propertyName = context.PropertyChain.BuildPropertyName(PropertyName ?? displayName);

			if (string.IsNullOrEmpty(propertyName)) {
				propertyName = InferPropertyName(Expression);
			}

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

			var filteredValidators = GetValidatorsToExecute(context);

			if (filteredValidators.Count == 0) {
				// If there are no property validators to execute after running the conditions, bail out.
				return;
			}

			var cascade = CascadeMode;
			var collection = PropertyFunc(context.InstanceToValidate) as IEnumerable<TElement>;

			int count = 0;
			int totalFailures = context.Failures.Count;

			if (collection != null) {
				if (string.IsNullOrEmpty(propertyName)) {
					throw new InvalidOperationException("Could not automatically determine the property name ");
				}

				foreach (var element in collection) {
					int index = count++;

					if (Filter != null && !Filter(element)) {
						continue;
					}

					string indexer = index.ToString();
					bool useDefaultIndexFormat = true;

					if (IndexBuilder != null) {
						indexer = IndexBuilder(context.InstanceToValidate, collection, element, index);
						useDefaultIndexFormat = false;
					}

					context.PrepareForChildCollectionValidator();
					context.PropertyChain.Add(propertyName);
					context.PropertyChain.AddIndexer(indexer, useDefaultIndexFormat);

					var valueToValidate = element;
					var propertyNameToValidate = context.PropertyChain.ToString();
					var totalFailuresInner = context.Failures.Count;
					context.InitializeForPropertyValidator(propertyNameToValidate, GetDisplayName, PropertyName);

					foreach (var validator in filteredValidators) {
						context.MessageFormatter.Reset();
						if (validator.ShouldValidateAsynchronously(context)) {
							throw new AsyncValidatorInvokedSynchronouslyException();
						}
						else {
							InvokePropertyValidator(context, valueToValidate, propertyNameToValidate, validator, index);
						}

						// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
						// then don't continue to the next rule
#pragma warning disable 618
						if (context.Failures.Count > totalFailuresInner && (cascade == CascadeMode.StopOnFirstFailure || cascade == CascadeMode.Stop)) {
							context.RestoreState();
							goto AfterValidate; // 🙃
						}
#pragma warning restore 618
					}
					context.RestoreState();
				}
			}

			AfterValidate:

			if (context.Failures.Count <= totalFailures && DependentRules != null) {
				foreach (var dependentRule in DependentRules) {
					dependentRule.Validate(context);
				}
			}
		}

		async Task IValidationRuleInternal<T>.ValidateAsync(ValidationContext<T> context, CancellationToken cancellation) {
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

			if (string.IsNullOrEmpty(propertyName)) {
				propertyName = InferPropertyName(Expression);
			}

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

			var filteredValidators = await GetValidatorsToExecuteAsync(context, cancellation);

			if (filteredValidators.Count == 0) {
				// If there are no property validators to execute after running the conditions, bail out.
				return;
			}

			var cascade = CascadeMode;
			var collection = PropertyFunc(context.InstanceToValidate) as IEnumerable<TElement>;

			int count = 0;
			int totalFailures = context.Failures.Count;

			if (collection != null) {
				if (string.IsNullOrEmpty(propertyName)) {
					throw new InvalidOperationException("Could not automatically determine the property name ");
				}

				foreach (var element in collection) {
					int index = count++;

					if (Filter != null && !Filter(element)) {
						continue;
					}

					string indexer = index.ToString();
					bool useDefaultIndexFormat = true;

					if (IndexBuilder != null) {
						indexer = IndexBuilder(context.InstanceToValidate, collection, element, index);
						useDefaultIndexFormat = false;
					}

					context.PrepareForChildCollectionValidator();
					context.PropertyChain.Add(propertyName);
					context.PropertyChain.AddIndexer(indexer, useDefaultIndexFormat);

					var valueToValidate = element;
					var propertyNameToValidate = context.PropertyChain.ToString();
					var totalFailuresInner = context.Failures.Count;
					context.InitializeForPropertyValidator(propertyNameToValidate, GetDisplayName, PropertyName);

					foreach (var validator in filteredValidators) {
						context.MessageFormatter.Reset();
						if (validator.ShouldValidateAsynchronously(context)) {
							await InvokePropertyValidatorAsync(context, valueToValidate, propertyNameToValidate, validator, index, cancellation);
						}
						else {
							InvokePropertyValidator(context, valueToValidate, propertyNameToValidate, validator, index);
						}

						// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
						// then don't continue to the next rule
#pragma warning disable 618
						if (context.Failures.Count > totalFailuresInner && (cascade == CascadeMode.StopOnFirstFailure || cascade == CascadeMode.Stop)) {
							context.RestoreState();
							goto AfterValidate; // 🙃
						}
#pragma warning restore 618
					}

					context.RestoreState();
				}
			}

			AfterValidate:

			if (context.Failures.Count <= totalFailures && DependentRules != null) {
				foreach (var dependentRule in DependentRules) {
					cancellation.ThrowIfCancellationRequested();
					await dependentRule.ValidateAsync(context, cancellation);
				}
			}
		}

		void IValidationRuleInternal<T>.AddDependentRules(IEnumerable<IValidationRuleInternal<T>> rules) {
			if (DependentRules == null) DependentRules = new();
			DependentRules.AddRange(rules);
		}

		private List<RuleComponent<T,TElement>> GetValidatorsToExecute(ValidationContext<T> context) {
			// Loop over each validator and check if its condition allows it to run.
			// This needs to be done prior to the main loop as within a collection rule
			// validators' conditions still act upon the root object, not upon the collection property.
			// This allows the property validators to cancel their execution prior to the collection
			// being retrieved (thereby possibly avoiding NullReferenceExceptions).
			// Must call ToList so we don't modify the original collection mid-loop.
			var validators = Components.ToList();

			foreach (var component in Components) {
				if (component.HasCondition) {
					if (!component.InvokeCondition(context)) {
						validators.Remove(component);
					}
				}

				if (component.HasAsyncCondition) {
					throw new AsyncValidatorInvokedSynchronouslyException();
				}
			}

			return validators;
		}

		private async Task<List<RuleComponent<T,TElement>>> GetValidatorsToExecuteAsync(ValidationContext<T> context, CancellationToken cancellation) {
			// Loop over each validator and check if its condition allows it to run.
			// This needs to be done prior to the main loop as within a collection rule
			// validators' conditions still act upon the root object, not upon the collection property.
			// This allows the property validators to cancel their execution prior to the collection
			// being retrieved (thereby possibly avoiding NullReferenceExceptions).
			// Must call ToList so we don't modify the original collection mid-loop.
			var validators = Components.ToList();

			foreach (var component in Components) {
				if (component.HasCondition) {
					if (!component.InvokeCondition(context)) {
						validators.Remove(component);
					}
				}

				if (component.HasAsyncCondition) {
					if (!await component.InvokeAsyncCondition(context, cancellation)) {
						validators.Remove(component);
					}
				}
			}

			return validators;
		}

		private async Task InvokePropertyValidatorAsync(ValidationContext<T> context, TElement value, string propertyName, RuleComponent<T,TElement> component, int index, CancellationToken cancellation) {
			context.MessageFormatter.AppendArgument("CollectionIndex", index);
			bool valid = await component.ValidateAsync(context, value, cancellation);

			if (!valid) {
				PrepareMessageFormatterForValidationError(context, value);
				var failure = CreateValidationError(context, value, component);
				context.Failures.Add(failure);
			}
		}

		private void InvokePropertyValidator(ValidationContext<T> context, TElement value, string propertyName, RuleComponent<T, TElement> component, int index) {
			context.MessageFormatter.AppendArgument("CollectionIndex", index);
			bool valid = component.Validate(context, value);

			if (!valid) {
				PrepareMessageFormatterForValidationError(context, value);
				var failure = CreateValidationError(context, value, component);
				context.Failures.Add(failure);
			}
		}

		private static string InferPropertyName(LambdaExpression expression) {
			var paramExp = expression.Body as ParameterExpression;

			if (paramExp == null) {
				throw new InvalidOperationException("Could not infer property name for expression: " + expression + ". Please explicitly specify a property name by calling OverridePropertyName as part of the rule chain. Eg: RuleForEach(x => x).NotNull().OverridePropertyName(\"MyProperty\")");
			}

			return paramExp.Name;
		}
	}
}
