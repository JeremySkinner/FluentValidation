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

#pragma warning disable 1591
namespace FluentValidation.TestHelper {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Results;
	using Validators;

	public static class ValidationTestExtension {
		internal const string MatchAnyFailure = "__FV__ANY";
#pragma warning disable 618


		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidate method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, string ruleSet = null) where T : class, new() {
			var instanceToValidate = new T();

			var memberAccessor = new MemberAccessor<T, TValue>(expression, true);
			memberAccessor.Set(instanceToValidate, value);

			var testValidationResult = validator.TestValidate(instanceToValidate, ruleSet);
			return testValidationResult.ShouldHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidate method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, string ruleSet = null) where T : class {
			var value = expression.Compile()(objectToTest);
			var testValidationResult = validator.TestValidate(objectToTest, ruleSet);
			return testValidationResult.ShouldHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidate method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static void ShouldNotHaveValidationErrorFor<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, string ruleSet = null) where T : class, new() {

			var instanceToValidate = new T();

			var memberAccessor = new MemberAccessor<T, TValue>(expression, true);
			memberAccessor.Set(instanceToValidate, value);

			var testValidationResult = validator.TestValidate(instanceToValidate, ruleSet);
			testValidationResult.ShouldNotHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidate method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static void ShouldNotHaveValidationErrorFor<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, string ruleSet = null) where T : class {
			var value = expression.Compile()(objectToTest);
			var testValidationResult = validator.TestValidate(objectToTest, ruleSet);
			testValidationResult.ShouldNotHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidateAsync method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static async Task<IEnumerable<ValidationFailure>> ShouldHaveValidationErrorForAsync<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, CancellationToken cancellationToken = default, string ruleSet = null) where T : class, new() {
			var instanceToValidate = new T();

			var memberAccessor = new MemberAccessor<T, TValue>(expression, true);
			memberAccessor.Set(instanceToValidate, value);

			var testValidationResult = await validator.TestValidateAsync(instanceToValidate, cancellationToken, ruleSet);
			return testValidationResult.ShouldHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidateAsync method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static async Task<IEnumerable<ValidationFailure>> ShouldHaveValidationErrorForAsync<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, CancellationToken cancellationToken = default, string ruleSet = null) where T : class {
			var value = expression.Compile()(objectToTest);
			var testValidationResult = await validator.TestValidateAsync(objectToTest, cancellationToken, ruleSet);
			return testValidationResult.ShouldHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidateAsync method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static async Task ShouldNotHaveValidationErrorForAsync<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, CancellationToken cancellationToken = default, string ruleSet = null) where T : class, new() {

			var instanceToValidate = new T();

			var memberAccessor = new MemberAccessor<T, TValue>(expression, true);
			memberAccessor.Set(instanceToValidate, value);

			var testValidationResult = await validator.TestValidateAsync(instanceToValidate, cancellationToken, ruleSet);
			testValidationResult.ShouldNotHaveValidationErrorFor(expression);
		}

		[Obsolete("This method is deprecated and will be removed in version 11. Use the TestValidateAsync method instead. See https://docs.fluentvalidation.net/en/latest/testing.html")]
		public static async Task ShouldNotHaveValidationErrorForAsync<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, CancellationToken cancellationToken = default, string ruleSet = null) where T : class {
			var testValidationResult = await validator.TestValidateAsync(objectToTest, cancellationToken, ruleSet);
			testValidationResult.ShouldNotHaveValidationErrorFor(expression);
		}
#pragma warning restore 618

		//TODO: Should ShouldHaveChildValidator be deprecated? It isn't recommended and leads to brittle tests.
		public static void ShouldHaveChildValidator<T, TProperty>(this IValidator<T> validator, Expression<Func<T, TProperty>> expression, Type childValidatorType) {
			var descriptor = validator.CreateDescriptor();
			var expressionMemberName = expression.GetMember()?.Name;

			if (expressionMemberName == null && !expression.IsParameterExpression()) {
				throw new NotSupportedException("ShouldHaveChildValidator can only be used for simple property expressions. It cannot be used for model-level rules or rules that contain anything other than a property reference.");
			}

			var matchingValidators =
				expression.IsParameterExpression() ? GetModelLevelValidators<T>(descriptor) :
				descriptor.GetValidatorsForMember(expressionMemberName)
					.Select(x => x.Validator)
					.ToArray();


			matchingValidators = matchingValidators.Concat(GetDependentRules(expressionMemberName, expression, descriptor)).ToArray();

			var childValidatorTypes = matchingValidators.OfType<IChildValidatorAdaptor>().Select(x => x.ValidatorType);

			if (childValidatorTypes.All(x => !childValidatorType.IsAssignableFrom(x))) {
				var childValidatorNames = childValidatorTypes.Any() ? string.Join(", ", childValidatorTypes.Select(x => x.Name)) : "none";
				throw new ValidationTestException(string.Format("Expected property '{0}' to have a child validator of type '{1}.'. Instead found '{2}'", expressionMemberName, childValidatorType.Name, childValidatorNames));
			}
		}

		private static IEnumerable<IPropertyValidator> GetDependentRules<T, TProperty>(string expressionMemberName, Expression<Func<T, TProperty>> expression, IValidatorDescriptor descriptor) {
			var member = expression.IsParameterExpression() ? null : expressionMemberName;

			var rules = descriptor.GetRulesForMember(member)
				.OfType<IValidationRuleInternal<T>>()
				.SelectMany(x => x.DependentRules ?? Enumerable.Empty<IValidationRuleInternal<T>>())
				.SelectMany(x => x.Components)
				.Select(x => x.Validator);

			return rules;
		}

		private static IPropertyValidator[] GetModelLevelValidators<T>(IValidatorDescriptor descriptor) {
			var rules = descriptor.GetRulesForMember(null).OfType<IValidationRule<T>>();
			return rules.Where(x => x.Expression == null || x.Expression.IsParameterExpression())
				.SelectMany(x => x.Components)
				.Select(x => x.Validator)
				.ToArray();
		}

		[Obsolete("Use the overload that takes an Action<ValidationStrategy> instead, which allows the ruleset to be specified inside the delegate.")]
		public static TestValidationResult<T> TestValidate<T>(this IValidator<T> validator, T objectToTest, string ruleSet) where T : class {
			return validator.TestValidate(objectToTest, options => {
				if (ruleSet != null) {
					options.IncludeRuleSets(RulesetValidatorSelector.LegacyRulesetSplit(ruleSet));
				}
			});
		}

		[Obsolete("Use the overload that takes an Action<ValidationStrategy> instead, which allows the ruleset to be specified inside the delegate.")]
		public static async Task<TestValidationResult<T>> TestValidateAsync<T>(this IValidator<T> validator, T objectToTest, CancellationToken cancellationToken, string ruleSet) where T : class {
			return await validator.TestValidateAsync(objectToTest, options => {
				if (ruleSet != null) {
					options.IncludeRuleSets(RulesetValidatorSelector.LegacyRulesetSplit(ruleSet));
				}
			}, cancellationToken);
		}

		/// <summary>
		/// Performs validation, returning a TestValidationResult which allows assertions to be performed.
		/// </summary>
		public static TestValidationResult<T> TestValidate<T>(this IValidator<T> validator, T objectToTest, Action<ValidationStrategy<T>> options = null) where T : class {
			options ??= _ => { };
			ValidationResult validationResult;
			try {
				validationResult = validator.Validate(objectToTest, options);
			}
			catch (AsyncValidatorInvokedSynchronouslyException ex) {
				throw new AsyncValidatorInvokedSynchronouslyException(ex.ValidatorType.Name + " contains asynchronous rules - please use the asynchronous test methods instead.");
			}

			return new TestValidationResult<T>(validationResult);
		}

		/// <summary>
		/// Performs async validation, returning a TestValidationResult which allows assertions to be performed.
		/// </summary>
		public static async Task<TestValidationResult<T>> TestValidateAsync<T>(this IValidator<T> validator, T objectToTest, Action<ValidationStrategy<T>> options = null, CancellationToken cancellationToken = default) where T : class {
			options ??= _ => { };
			var validationResult = await validator.ValidateAsync(objectToTest, options, cancellationToken);
			return new TestValidationResult<T>(validationResult);
		}

		public static IEnumerable<ValidationFailure> ShouldHaveAnyValidationError<T>(this TestValidationResult<T> testValidationResult) where T : class {
			if (!testValidationResult.Errors.Any())
				throw new ValidationTestException($"Expected at least one validation error, but none were found.");

			return testValidationResult.Errors;
		}

		public static void ShouldNotHaveAnyValidationErrors<T>(this TestValidationResult<T> testValidationResult) where T : class {
			ShouldNotHaveValidationError(testValidationResult.Errors, MatchAnyFailure, true);
		}

		private static string BuildErrorMessage(ValidationFailure failure, string exceptionMessage, string defaultMessage) {
      if (exceptionMessage != null && failure != null) {
        var formattedExceptionMessage = exceptionMessage.Replace("{Code}", failure.ErrorCode)
          .Replace("{Message}", failure.ErrorMessage)
          .Replace("{State}", failure.CustomState?.ToString() ?? "")
          .Replace("{Severity}", failure.Severity.ToString());

        var messageArgumentMatches = Regex.Matches(formattedExceptionMessage, "{MessageArgument:(.*)}");
        for (var i = 0; i < messageArgumentMatches.Count; i++) {
          if (failure.FormattedMessagePlaceholderValues.ContainsKey(messageArgumentMatches[i].Groups[1].Value)) {
            formattedExceptionMessage = formattedExceptionMessage.Replace(messageArgumentMatches[i].Value, failure.FormattedMessagePlaceholderValues[messageArgumentMatches[i].Groups[1].Value].ToString());
          }
        }
        return formattedExceptionMessage;
      }
			return defaultMessage;
		}

    internal static IEnumerable<ValidationFailure> ShouldHaveValidationError(IList<ValidationFailure> errors, string propertyName, bool shouldNormalizePropertyName) {

      var failures = errors.Where(x => (shouldNormalizePropertyName ?  NormalizePropertyName(x.PropertyName) == propertyName : x.PropertyName == propertyName)
                                       || (string.IsNullOrEmpty(x.PropertyName) && string.IsNullOrEmpty(propertyName))
                                       || propertyName == MatchAnyFailure
                                       ).ToArray();

      if (failures.Any()) {
        return failures;
      }

      // We expected an error but failed to match it.
      var errorMessageBanner = $"Expected a validation error for property {propertyName}";

      string errorMessage = "";

      if (errors?.Any() == true) {
        string errorMessageDetails = "";
        for (int i = 0; i < errors.Count; i++) {
          errorMessageDetails += $"[{i}]: {errors[i].PropertyName}\n";
        }
        errorMessage = $"{errorMessageBanner}\n----\nProperties with Validation Errors:\n{errorMessageDetails}";
      }
      else {
        errorMessage = $"{errorMessageBanner}";
      }

      throw new ValidationTestException(errorMessage);
    }

		internal static void ShouldNotHaveValidationError(IEnumerable<ValidationFailure> errors, string propertyName, bool shouldNormalizePropertyName) {
			var failures = errors.Where(x => (shouldNormalizePropertyName ? NormalizePropertyName(x.PropertyName) == propertyName : x.PropertyName == propertyName)
			                                 || (string.IsNullOrEmpty(x.PropertyName) && string.IsNullOrEmpty(propertyName))
			                                 || propertyName == MatchAnyFailure
			                                 ).ToList();

			if (failures.Any()) {
				var errorMessageBanner = $"Expected no validation errors for property {propertyName}";
				if (propertyName == MatchAnyFailure) {
					errorMessageBanner = "Expected no validation errors";
				}
				string errorMessageDetails = "";
				for (int i = 0; i < failures.Count; i++) {
					errorMessageDetails += $"[{i}]: {failures[i].ErrorMessage}\n";
				}
				var errorMessage = $"{errorMessageBanner}\n----\nValidation Errors:\n{errorMessageDetails}";
				throw new ValidationTestException(errorMessage, failures);
			}
		}

		public static IEnumerable<ValidationFailure> When(this IEnumerable<ValidationFailure> failures, Func<ValidationFailure, bool> failurePredicate, string exceptionMessage = null){
			bool anyMatched = failures.Any(failurePredicate);

			if (!anyMatched) {
				var failure = failures.FirstOrDefault();
				string message = BuildErrorMessage(failure, exceptionMessage, "Expected validation error was not found");
				throw new ValidationTestException(message);
			}

			return failures;
		}

		public static IEnumerable<ValidationFailure> WhenAll(this IEnumerable<ValidationFailure> failures, Func<ValidationFailure, bool> failurePredicate, string exceptionMessage = null) {
			bool allMatched = failures.All(failurePredicate);

			if (!allMatched) {
				var failure = failures.First(fail => !(failurePredicate(fail)));
				string message = BuildErrorMessage(failure, exceptionMessage, "Found an unexpected validation error");
				throw new ValidationTestException(message);
			}

			return failures;
		}

		public static IEnumerable<ValidationFailure> WithSeverity(this IEnumerable<ValidationFailure> failures, Severity expectedSeverity) {
			return failures.When(failure => failure.Severity == expectedSeverity, string.Format("Expected a severity of '{0}'. Actual severity was '{{Severity}}'", expectedSeverity));
		}

		public static IEnumerable<ValidationFailure> WithCustomState(this IEnumerable<ValidationFailure> failures, object expectedCustomState, IEqualityComparer comparer = null) {
			return failures.When(failure => comparer?.Equals(failure.CustomState, expectedCustomState) ?? Equals(failure.CustomState, expectedCustomState), string.Format("Expected custom state of '{0}'. Actual state was '{{State}}'", expectedCustomState));
		}

    public static IEnumerable<ValidationFailure> WithMessageArgument<T>(this IEnumerable<ValidationFailure> failures, string argumentKey, T argumentValue) {
      return failures.When(failure => failure.FormattedMessagePlaceholderValues.ContainsKey(argumentKey) && ((T)failure.FormattedMessagePlaceholderValues[argumentKey]).Equals(argumentValue),
        string.Format("Expected message argument '{0}' with value '{1}'. Actual value was '{{MessageArgument:{0}}}'", argumentKey, argumentValue.ToString()));
    }

		public static IEnumerable<ValidationFailure> WithErrorMessage(this IEnumerable<ValidationFailure> failures, string expectedErrorMessage) {
			return failures.When(failure => failure.ErrorMessage == expectedErrorMessage, string.Format("Expected an error message of '{0}'. Actual message was '{{Message}}'", expectedErrorMessage));
		}

		public static IEnumerable<ValidationFailure> WithErrorCode(this IEnumerable<ValidationFailure> failures, string expectedErrorCode) {
			return failures.When(failure => failure.ErrorCode == expectedErrorCode, string.Format("Expected an error code of '{0}'. Actual error code was '{{Code}}'", expectedErrorCode));
		}

		public static IEnumerable<ValidationFailure> WithoutSeverity(this IEnumerable<ValidationFailure> failures, Severity unexpectedSeverity) {
			return failures.WhenAll(failure => failure.Severity != unexpectedSeverity, string.Format("Found an unexpected severity of '{0}'", unexpectedSeverity));
		}

		public static IEnumerable<ValidationFailure> WithoutCustomState(this IEnumerable<ValidationFailure> failures, object unexpectedCustomState) {
			return failures.WhenAll(failure => failure.CustomState != unexpectedCustomState, string.Format("Found an unexpected custom state of '{0}'", unexpectedCustomState));
		}

		public static IEnumerable<ValidationFailure> WithoutErrorMessage(this IEnumerable<ValidationFailure> failures, string unexpectedErrorMessage) {
			return failures.WhenAll(failure => failure.ErrorMessage != unexpectedErrorMessage, string.Format("Found an unexpected error message of '{0}'", unexpectedErrorMessage));
		}

		public static IEnumerable<ValidationFailure> WithoutErrorCode(this IEnumerable<ValidationFailure> failures, string unexpectedErrorCode) {
			return failures.WhenAll(failure => failure.ErrorCode != unexpectedErrorCode, string.Format("Found an unexpected error code of '{0}'", unexpectedErrorCode));
		}

		private static string NormalizePropertyName(string propertyName) {
			return Regex.Replace(propertyName, @"\[.*\]", string.Empty);
		}
	}
}
