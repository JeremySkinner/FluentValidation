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
	using Results;
	using Validators;

	internal abstract class RuleBase<T, TProperty, TValue> : IValidationRule<T, TValue> {
		private protected readonly List<PropertyValidator<T,TValue>> _validators = new();
		private Func<CascadeMode> _cascadeModeThunk;
		private string _propertyDisplayName;
		private string _propertyName;
		private Func<ValidationContext<T>, bool> _condition;
		private Func<ValidationContext<T>, CancellationToken, Task<bool>> _asyncCondition;
		private string _displayName;
		private Func<ValidationContext<T>, string> _displayNameFactory;

		/// <summary>
		/// Condition for all validators in this rule.
		/// </summary>
		internal Func<ValidationContext<T>, bool> Condition => _condition;

		/// <summary>
		/// Asynchronous condition for all validators in this rule.
		/// </summary>
		internal Func<ValidationContext<T>, CancellationToken, Task<bool>> AsyncCondition => _asyncCondition;

		/// <summary>
		/// Property associated with this rule.
		/// </summary>
		public MemberInfo Member { get; }

		/// <summary>
		/// Function that can be invoked to retrieve the value of the property.
		/// </summary>
		public Func<T, TProperty> PropertyFunc { get; }

		/// <summary>
		/// Expression that was used to create the rule.
		/// </summary>
		public LambdaExpression Expression { get; }

		/// <summary>
		/// Sets the display name for the property.
		/// </summary>
		/// <param name="name">The property's display name</param>
		public void SetDisplayName(string name) {
			_displayName = name;
			_displayNameFactory = null;
		}

		/// <summary>
		/// Sets the display name for the property using a function.
		/// </summary>
		/// <param name="factory">The function for building the display name</param>
		public void SetDisplayName(Func<ValidationContext<T>, string> factory) {
			if (factory == null) throw new ArgumentNullException(nameof(factory));
			_displayNameFactory = factory;
			_displayName = null;
		}

		/// <summary>
		/// Rule set that this rule belongs to (if specified)
		/// </summary>
		public string[] RuleSets { get; set; }

		/// <summary>
		/// Function that will be invoked if any of the validators associated with this rule fail.
		/// </summary>
		public Action<T, IEnumerable<ValidationFailure>> OnFailure { get; set; }

		/// <summary>
		/// The current validator being configured by this rule.
		/// </summary>
		public PropertyValidator<T,TValue> CurrentValidator => _validators.LastOrDefault();

		/// <summary>
		/// Type of the property being validated
		/// </summary>
		public Type TypeToValidate { get; }

		/// <inheritdoc />
		public bool HasCondition => Condition != null;

		/// <inheritdoc />
		public bool HasAsyncCondition => AsyncCondition != null;

		/// <summary>
		/// Cascade mode for this rule.
		/// </summary>
		public CascadeMode CascadeMode {
			get => _cascadeModeThunk();
			set => _cascadeModeThunk = () => value;
		}

		/// <summary>
		/// Validators associated with this rule.
		/// </summary>
		public IEnumerable<IPropertyValidator> Validators => _validators.Cast<IPropertyValidator>();

		/// <summary>
		/// Creates a new property rule.
		/// </summary>
		/// <param name="member">Property</param>
		/// <param name="propertyFunc">Function to get the property value</param>
		/// <param name="expression">Lambda expression used to create the rule</param>
		/// <param name="cascadeModeThunk">Function to get the cascade mode.</param>
		/// <param name="typeToValidate">Type to validate</param>
		public RuleBase(MemberInfo member, Func<T, TProperty> propertyFunc, LambdaExpression expression, Func<CascadeMode> cascadeModeThunk, Type typeToValidate) {
			Member = member;
			PropertyFunc = propertyFunc;
			Expression = expression;
			TypeToValidate = typeToValidate;
			_cascadeModeThunk = cascadeModeThunk;

			var containerType = typeof(T);
			PropertyName = ValidatorOptions.Global.PropertyNameResolver(containerType, member, expression);
			_displayNameFactory = context => ValidatorOptions.Global.DisplayNameResolver(containerType, member, expression);
		}

		/// <summary>
		/// Adds a validator to the rule.
		/// </summary>
		public void AddValidator(PropertyValidator<T,TValue> validator) {
			_validators.Add(validator);
		}

		/// <summary>
		/// Replaces a validator in this rule. Used to wrap validators.
		/// </summary>
		public void ReplaceValidator(PropertyValidator<T,TValue> original, PropertyValidator<T,TValue> newValidator) {
			var index = _validators.IndexOf(original);

			if (index > -1) {
				_validators[index] = newValidator;
			}
		}

		/// <summary>
		/// Remove a validator in this rule.
		/// </summary>
		public void RemoveValidator(PropertyValidator<T,TValue> original) {
			_validators.Remove(original);
		}

		/// <summary>
		/// Clear all validators from this rule.
		/// </summary>
		public void ClearValidators() {
			_validators.Clear();
		}

		/// <summary>
		/// Returns the property name for the property being validated.
		/// Returns null if it is not a property being validated (eg a method call)
		/// </summary>
		public string PropertyName {
			get { return _propertyName; }
			set {
				_propertyName = value;
				_propertyDisplayName = _propertyName.SplitPascalCase();
			}
		}

		/// <summary>
		/// Allows custom creation of an error message
		/// </summary>
		public Func<MessageBuilderContext<T,TValue>, string> MessageBuilder { get; set; }

		/// <summary>
		/// Dependent rules
		/// </summary>
		internal List<IExecutableValidationRule<T>> DependentRules { get; private protected set; }

		string IValidationRule.GetDisplayName(IValidationContext context) =>
			GetDisplayName(context != null ? ValidationContext<T>.GetFromNonGenericContext(context) : null);

		/// <summary>
		/// Display name for the property.
		/// </summary>
		public string GetDisplayName(ValidationContext<T> context)
			=> _displayNameFactory?.Invoke(context) ?? _displayName ?? _propertyDisplayName;

		/// <summary>
		/// Applies a condition to the rule
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="applyConditionTo"></param>
		public void ApplyCondition(Func<IValidationContext, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators) {
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			if (applyConditionTo == ApplyConditionTo.AllValidators) {
				foreach (var validator in _validators) {
					validator.ApplyCondition(predicate);
				}

				if (DependentRules != null) {
					foreach (var dependentRule in DependentRules) {
						dependentRule.ApplyCondition(predicate, applyConditionTo);
					}
				}
			}
			else {
				CurrentValidator.ApplyCondition(predicate);
			}
		}

		/// <summary>
		/// Applies the condition to the rule asynchronously
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="applyConditionTo"></param>
		public void ApplyAsyncCondition(Func<IValidationContext, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators) {
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			if (applyConditionTo == ApplyConditionTo.AllValidators) {
				foreach (var validator in _validators) {
					validator.ApplyAsyncCondition(predicate);
				}

				if (DependentRules != null) {
					foreach (var dependentRule in DependentRules) {
						dependentRule.ApplyAsyncCondition(predicate, applyConditionTo);
					}
				}
			}
			else {
				CurrentValidator.ApplyAsyncCondition(predicate);
			}
		}

		public void ApplySharedCondition(Func<ValidationContext<T>, bool> condition) {
			if (_condition == null) {
				_condition = condition;
			}
			else {
				var original = _condition;
				_condition = ctx => condition(ctx) && original(ctx);
			}
		}

		public void ApplySharedAsyncCondition(Func<ValidationContext<T>, CancellationToken, Task<bool>> condition) {
			if (_asyncCondition == null) {
				_asyncCondition = condition;
			}
			else {
				var original = _asyncCondition;
				_asyncCondition = async (ctx, ct) => await condition(ctx, ct) && await original(ctx, ct);
			}
		}
	}
}
