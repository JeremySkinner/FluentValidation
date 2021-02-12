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
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Resources;
	using Results;
	using Validators;
	using Validators.Features;

	/// <summary>
	/// Default options that can be used to configure a validator.
	/// </summary>
	public static class DefaultValidatorOptions {
		/// <summary>
		/// Configures the rule.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		public static IRuleBuilderInitial<T, TProperty> Configure<T, TProperty>(this IRuleBuilderInitial<T, TProperty> ruleBuilder, Action<IValidationRule<T, TProperty>> configurator) {
			var rb = (RuleBuilder<T, TProperty>) ruleBuilder;
			configurator(rb.Rule);
			return ruleBuilder;
		}

		/// <summary>
		/// Configures the current object.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> Configure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> ruleBuilder, Action<IValidationRule<T, TProperty>> configurator) {
			var rb = (ComponentRuleBuilder<T, TProperty, TValidator>) ruleBuilder;
			configurator(rb.Rule);
			return rb;
		}

		/// <summary>
		/// Configures the rule object.
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="configurator">Action to configure the object.</param>
		/// <returns></returns>
		public static IRuleBuilderInitialCollection<T, TElement> Configure<T, TElement>(this IRuleBuilderInitialCollection<T, TElement> ruleBuilder, Action<ICollectionRule<T, TElement>> configurator) {
			var rb = (RuleBuilder<T, TElement>) ruleBuilder;
			configurator((ICollectionRule<T, TElement>) rb.Rule);
			return ruleBuilder;
		}

		/// <summary>
		/// Specifies the cascade mode for failures.
		/// If set to 'Stop' then execution of the rule will stop once the first validator in the chain fails.
		/// If set to 'Continue' then all validators in the chain will execute regardless of failures.
		/// </summary>
		public static IRuleBuilderInitial<T, TProperty> Cascade<T, TProperty>(this IRuleBuilderInitial<T, TProperty> ruleBuilder, CascadeMode cascadeMode) {
			return ruleBuilder.Configure(cfg => {
				cfg.CascadeMode = cascadeMode;
			});
		}

		/// <summary>
		/// Specifies the cascade mode for failures.
		/// If set to 'Stop' then execution of the rule will stop once the first validator in the chain fails.
		/// If set to 'Continue' then all validators in the chain will execute regardless of failures.
		/// </summary>
		public static IRuleBuilderInitialCollection<T, TProperty> Cascade<T, TProperty>(this IRuleBuilderInitialCollection<T, TProperty> ruleBuilder, CascadeMode cascadeMode) {
			return ruleBuilder.Configure(cfg => {
				cfg.CascadeMode = cascadeMode;
			});
		}

		/// <summary>
		/// Specifies a custom action to be invoked when the validator fails.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="onFailure"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OnAnyFailure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Action<T> onFailure)
			where TValidator : ISupportsOnAnyFailure {
			if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
			return rule.Configure(config => {
				config.OnFailure = (x, failures) => onFailure((T)x);
			});
		}

		/// <summary>
		/// Specifies a custom action to be invoked when the validator fails.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="onFailure"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OnAnyFailure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Action<T, IEnumerable<ValidationFailure>> onFailure)
			where TValidator : ISupportsOnAnyFailure {
			if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
			return rule.Configure(config => {
				config.OnFailure = (x, failures) => onFailure((T)x, failures);
			});
		}

		/// <summary>
		/// Specifies a custom error message to use when validation fails. Only applies to the rule that directly precedes it.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="errorMessage">The error message to use</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithMessage<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, string errorMessage)
			where TValidator : ISupportsCustomMessage {
			errorMessage.Guard("A message must be specified when calling WithMessage.", nameof(errorMessage));
			return rule.Configure(config => {
				config.Current.SetErrorMessage(errorMessage);
			});
		}

		/// <summary>
		/// Specifies a custom error message to use when validation fails. Only applies to the rule that directly precedes it.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="messageProvider">Delegate that will be invoked to retrieve the localized message. </param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithMessage<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, string> messageProvider)
			where TValidator : ISupportsCustomMessage {
			messageProvider.Guard("A messageProvider must be provided.", nameof(messageProvider));
			return rule.Configure(config => {
				config.Current.SetErrorMessage((ctx, val) => {
					return messageProvider(ctx == null ? default : ctx.InstanceToValidate);
				});
			});
		}

		/// <summary>
		/// Specifies a custom error message to use when validation fails. Only applies to the rule that directly precedes it.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="messageProvider">Delegate that will be invoked.Uses_localized_name to retrieve the localized message. </param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithMessage<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, TProperty, string> messageProvider)
			where TValidator : ISupportsCustomMessage {
			messageProvider.Guard("A messageProvider must be provided.", nameof(messageProvider));

			return rule.Configure(config => {
				config.Current.SetErrorMessage((context, value) => {
					return messageProvider(context == null ? default : context.InstanceToValidate, value);
				});
			});
		}

		/// <summary>
		/// Specifies a custom error code to use if validation fails.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="errorCode">The error code to use</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithErrorCode<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, string errorCode)
			where TValidator : ISupportsCustomErrorCode {
			errorCode.Guard("A error code must be specified when calling WithErrorCode.", nameof(errorCode));

			return rule.Configure(config => {
				config.Current.ErrorCode = errorCode;
			});
		}

		/// <summary>
		/// Specifies a condition limiting when the validator should run.
		/// The validator will only be executed if the result of the lambda returns true.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> When<T,TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsConditions {
			predicate.Guard("A predicate must be specified when calling When.", nameof(predicate));
			return rule.When((x, ctx) => predicate(x), applyConditionTo);
		}

		/// <summary>
		/// Specifies a condition limiting when the validator should run.
		/// The validator will only be executed if the result of the lambda returns true.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> When<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, ValidationContext<T>, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsConditions {
			predicate.Guard("A predicate must be specified when calling When.", nameof(predicate));
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			return rule.Configure(config => {
				config.ApplyCondition(ctx => predicate((T)ctx.InstanceToValidate, ValidationContext<T>.GetFromNonGenericContext(ctx)), applyConditionTo);
			});
		}

		/// <summary>
		/// Specifies a condition limiting when the validator should not run.
		/// The validator will only be executed if the result of the lambda returns false.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should not run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> Unless<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator  : ISupportsConditions {
			predicate.Guard("A predicate must be specified when calling Unless", nameof(predicate));
			return rule.Unless((x, ctx) => predicate(x), applyConditionTo);
		}

		/// <summary>
		/// Specifies a condition limiting when the validator should not run.
		/// The validator will only be executed if the result of the lambda returns false.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should not run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> Unless<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, ValidationContext<T>, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsConditions {
			predicate.Guard("A predicate must be specified when calling Unless", nameof(predicate));
			return rule.When((x, ctx) => !predicate(x, ctx), applyConditionTo);
		}

		/// <summary>
		/// Specifies an asynchronous condition limiting when the validator should run.
		/// The validator will only be executed if the result of the lambda returns true.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WhenAsync<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsAsyncConditions {
			predicate.Guard("A predicate must be specified when calling WhenAsync.", nameof(predicate));
			return rule.WhenAsync((x, ctx, ct) => predicate(x, ct), applyConditionTo);
		}

		/// <summary>
		/// Specifies an asynchronous condition limiting when the validator should run.
		/// The validator will only be executed if the result of the lambda returns true.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WhenAsync<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsAsyncConditions {
			predicate.Guard("A predicate must be specified when calling WhenAsync.", nameof(predicate));
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			return rule.Configure(config => {
				config.ApplyAsyncCondition((ctx, ct) => predicate((T)ctx.InstanceToValidate, ValidationContext<T>.GetFromNonGenericContext(ctx), ct), applyConditionTo);
			});
		}

		/// <summary>
		/// Specifies an asynchronous condition limiting when the validator should not run.
		/// The validator will only be executed if the result of the lambda returns false.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should not run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> UnlessAsync<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsAsyncConditions {
			predicate.Guard("A predicate must be specified when calling UnlessAsync", nameof(predicate));
			return rule.UnlessAsync((x, ctx, ct) => predicate(x, ct), applyConditionTo);
		}

		/// <summary>
		/// Specifies an asynchronous condition limiting when the validator should not run.
		/// The validator will only be executed if the result of the lambda returns false.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">A lambda expression that specifies a condition for when the validator should not run</param>
		/// <param name="applyConditionTo">Whether the condition should be applied to the current rule or all rules in the chain</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> UnlessAsync<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators)
			where TValidator : ISupportsAsyncConditions {
			predicate.Guard("A predicate must be specified when calling UnlessAsync", nameof(predicate));
			return rule.WhenAsync(async (x, ctx, ct) => !await predicate(x, ctx, ct), applyConditionTo);
		}

		/// <summary>
		/// Applies a filter to a collection property.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="predicate">The condition</param>
		/// <returns></returns>
		public static IRuleBuilderInitialCollection<T, TCollectionElement> Where<T, TCollectionElement>(this IRuleBuilderInitialCollection<T, TCollectionElement> rule, Func<TCollectionElement, bool> predicate) {
			// This overload supports RuleFor().SetCollectionValidator() (which returns IRuleBuilderOptions<T, IEnumerable<TElement>>)
			predicate.Guard("Cannot pass null to Where.", nameof(predicate));
			return rule.Configure(cfg => {
				cfg.Filter = predicate;
			});
		}

		/// <summary>
		/// Specifies a custom property name to use within the error message.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="overridePropertyName">The property name to use</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithName<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, string overridePropertyName)
			where TValidator:ISupportsDisplayNameOverride {
			overridePropertyName.Guard("A property name must be specified when calling WithName.", nameof(overridePropertyName));
			return rule.Configure(config => {
				config.SetDisplayName(overridePropertyName);
			});
		}

		/// <summary>
		/// Specifies a custom property name to use within the error message.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="nameProvider">Func used to retrieve the property's display name</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithName<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, string> nameProvider)
			where TValidator : ISupportsDisplayNameOverride {
			nameProvider.Guard("A nameProvider WithName.", nameof(nameProvider));
			return rule.Configure(config => {
				// Must use null propagation here.
				// The MVC clientside validation will try and retrieve the name, but won't
				// be able to to so if we've used this overload of WithName.
				config.SetDisplayName((context => {
					T instance = context == null ? default : context.InstanceToValidate;
					return nameProvider(instance);
				}));
			});
		}

		/// <summary>
		/// Overrides the name of the property associated with this rule.
		/// NOTE: This is a considered to be an advanced feature. Most of the time that you use this, you actually meant to use WithName.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="propertyName">The property name to use</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OverridePropertyName<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, string propertyName)
			where TValidator : ISupportsPropertyNameOverride {
			// Allow string.Empty as this could be a model-level rule.
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName), "A property name must be specified when calling OverridePropertyName.");
			return rule.Configure(config => config.PropertyName = propertyName);
		}

		/// <summary>
		/// Overrides the name of the property associated with this rule.
		/// NOTE: This is a considered to be an advanced feature. Most of the time that you use this, you actually meant to use WithName.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="expr">An expression referencing another property</param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OverridePropertyName<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Expression<Func<T, object>> expr)
			where TValidator : ISupportsPropertyNameOverride {
			if (expr == null) throw new ArgumentNullException(nameof(expr));
			var member = expr.GetMember();
			if (member == null) throw new NotSupportedException("Must supply a MemberExpression when calling OverridePropertyName");
			return rule.OverridePropertyName(member.Name);
		}

		/// <summary>
		/// Specifies custom state that should be stored alongside the validation message when validation fails for this rule.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="stateProvider"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithState<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, object> stateProvider)
			where TValidator : ISupportsCustomState {
			stateProvider.Guard("A lambda expression must be passed to WithState", nameof(stateProvider));
			var wrapper = new Func<ValidationContext<T>, TProperty, object>((ctx, _) => stateProvider(ctx.InstanceToValidate));
			return rule.Configure(config => config.Current.CustomStateProvider = wrapper);
		}

		/// <summary>
		/// Specifies custom state that should be stored alongside the validation message when validation fails for this rule.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="stateProvider"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithState<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, TProperty, object> stateProvider)
			where TValidator : ISupportsCustomState {
			stateProvider.Guard("A lambda expression must be passed to WithState", nameof(stateProvider));

			var wrapper = new Func<ValidationContext<T>, TProperty, object>((ctx, val) => {
				return stateProvider(ctx.InstanceToValidate, val);
			});

			return rule.Configure(config => config.Current.CustomStateProvider = wrapper);
		}

		/// <summary>
		///  Specifies custom severity that should be stored alongside the validation message when validation fails for this rule.
		///  </summary>
		///  <typeparam name="T"></typeparam>
		///  <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		///  <param name="severity"></param>
		///  <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithSeverity<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Severity severity)
			where TValidator : ISupportsSeverity {
			return rule.Configure(config => config.Current.SeverityProvider = (_, _) => severity);
		}

		/// <summary>
		/// Specifies custom severity that should be stored alongside the validation message when validation fails for this rule.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="severityProvider"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithSeverity<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, Severity> severityProvider)
			where TValidator : ISupportsSeverity {
			severityProvider.Guard("A lambda expression must be passed to WithSeverity", nameof(severityProvider));

			Severity SeverityProvider(ValidationContext<T> ctx, TProperty value) {
				return severityProvider(ctx.InstanceToValidate);
			}

			return rule.Configure(config => config.Current.SeverityProvider = SeverityProvider);
		}

		/// <summary>
		/// Specifies custom severity that should be stored alongside the validation message when validation fails for this rule.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="severityProvider"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> WithSeverity<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Func<T, TProperty, Severity> severityProvider)
			where TValidator : ISupportsSeverity {
			severityProvider.Guard("A lambda expression must be passed to WithSeverity", nameof(severityProvider));

			Severity SeverityProvider(ValidationContext<T> ctx, TProperty value) {
				return severityProvider(ctx.InstanceToValidate, value);
			}

			return rule.Configure(config => config.Current.SeverityProvider = SeverityProvider);
		}

		/// <summary>
		/// Specifies custom method that will be called when specific rule fails
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="onFailure"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OnFailure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Action<T> onFailure)
			where TValidator : ISupportsOnFailure {
			return rule.Configure(config => {
				config.Current.OnFailure = (instance, _, _, _) => onFailure((T)instance);
			});
		}

		/// <summary>
		/// Specifies custom method that will be called when specific rule fails
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="onFailure"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OnFailure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Action<T, ValidationContext<T>, TProperty> onFailure)
			where TValidator : ISupportsOnFailure {
			return rule.Configure(config => {
				config.Current.OnFailure = (instance, context, val, _) => onFailure(instance, context, val);
			});
		}

		/// <summary>
		/// Specifies custom method that will be called when specific rule fails
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <param name="rule"></param>
		/// <param name="onFailure"></param>
		/// <returns></returns>
		public static IRuleBuilderOptions<T, TProperty, TValidator> OnFailure<T, TProperty, TValidator>(this IRuleBuilderOptions<T, TProperty, TValidator> rule, Action<T, ValidationContext<T>, TProperty, string> onFailure)
			where TValidator : ISupportsOnFailure {
			return rule.Configure(config => {
				config.Current.OnFailure = onFailure;
			});
		}

		/// <summary>
		/// Allows the generated indexer to be overridden for collection rules.
		/// </summary>
		/// <param name="rule">The current rule</param>
		/// <param name="callback">The callback. Receives the model, the collection, the current element and the current index as parameters. Should return a string representation of the indexer. The default is "[" + index + "]"</param>
		/// <returns></returns>
		public static IRuleBuilderInitialCollection<T, TCollectionElement> OverrideIndexer<T, TCollectionElement>(this IRuleBuilderInitialCollection<T, TCollectionElement> rule, Func<T, IEnumerable<TCollectionElement>, TCollectionElement, int, string> callback) {
			// This overload supports RuleFor().SetCollectionValidator() (which returns IRuleBuilderOptions<T, IEnumerable<TElement>>)
			callback.Guard("Cannot pass null to OverrideIndexer.", nameof(callback));
			return rule.Configure(cfg => {
				cfg.IndexBuilder = (x, collection, element, index) => callback((T)x, (IEnumerable<TCollectionElement>)collection, (TCollectionElement)element, index);
			});
		}
	}
}
