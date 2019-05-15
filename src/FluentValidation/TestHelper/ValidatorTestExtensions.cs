#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
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
// The latest version of this file can be found at https://github.com/jeremyskinner/FluentValidation
#endregion

#pragma warning disable 1591
namespace FluentValidation.TestHelper {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using Internal;
	using Results;
	using Validators;

	public static class ValidationTestExtension {
		public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, string ruleSet = null) where T : class, new() {
			var instanceToValidate = new T();
			var testValidationResult = validator.TestValidate(expression, instanceToValidate, value, ruleSet);
			return testValidationResult.ShouldHaveError();
		}

		public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, string ruleSet = null) where T : class {
			var value = expression.Compile()(objectToTest);
			var testValidationResult = validator.TestValidate(expression, objectToTest, value, ruleSet, setProperty:false);
			return testValidationResult.ShouldHaveError();
		}

		public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, T>> expression, string ruleSet)
            where T : class, new()
        {
            T instance = Activator.CreateInstance<T>();
            return validator.TestValidate<T>(instance, ruleSet).ShouldHaveError<T, T>();
        }

        public static IEnumerable<ValidationFailure> ShouldHaveValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, T>> expression, T value, string ruleSet)
            where T : class, new()
        {
            return validator.TestValidate<T>(value, ruleSet).ShouldHaveError<T, T>();
        }

		public static void ShouldNotHaveValidationErrorFor<T, TValue>(this IValidator<T> validator,
			Expression<Func<T, TValue>> expression, TValue value, string ruleSet = null) where T : class, new() {
				var instanceToValidate = new T();
			var testValidationResult = validator.TestValidate(expression, instanceToValidate, value, ruleSet);
			testValidationResult.ShouldNotHaveError();
		}

		public static void ShouldNotHaveValidationErrorFor<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T objectToTest, string ruleSet = null) where T : class {
			var value = expression.Compile()(objectToTest);
			var testValidationResult = validator.TestValidate(expression, objectToTest, value, ruleSet, setProperty:false);
			testValidationResult.ShouldNotHaveError();
		}

		public static void ShouldNotHaveValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, T>> expression, string ruleSet)
            where T : class, new()
        {
            T instance = Activator.CreateInstance<T>();
            validator.TestValidate<T>(instance, ruleSet).ShouldNotHaveError<T, T>();
        }

        public static void ShouldNotHaveValidationErrorFor<T>(this IValidator<T> validator, Expression<Func<T, T>> expression, T value, string ruleSet)
            where T : class, new()
        {
            validator.TestValidate<T>(value, ruleSet).ShouldNotHaveError<T, T>();
        }

		public static void ShouldHaveChildValidator<T, TProperty>(this IValidator<T> validator, Expression<Func<T, TProperty>> expression, Type childValidatorType) {
			var descriptor = validator.CreateDescriptor();
			var expressionMemberName = expression.GetMember()?.Name;

			if (expressionMemberName == null && !expression.IsParameterExpression()) {
				throw new NotSupportedException("ShouldHaveChildValidator can only be used for simple property expressions. It cannot be used for model-level rules or rules that contain anything other than a property reference.");
			}

			var matchingValidators = 
				expression.IsParameterExpression()	 ? GetModelLevelValidators(descriptor) :
				descriptor.GetValidatorsForMember(expressionMemberName).ToArray();


			matchingValidators = matchingValidators.Concat(GetDependentRules(expressionMemberName, expression, descriptor)).ToArray();
			
			var childValidatorTypes = matchingValidators.OfType<IChildValidatorAdaptor>().Select(x => x.ValidatorType);

			if (childValidatorTypes.All(x => !childValidatorType.GetTypeInfo().IsAssignableFrom(x.GetTypeInfo()))) {
				var childValidatorNames = childValidatorTypes.Any() ? string.Join(", ", childValidatorTypes.Select(x => x.Name)) : "none";
				throw new ValidationTestException(string.Format("Expected property '{0}' to have a child validator of type '{1}.'. Instead found '{2}'", expressionMemberName, childValidatorType.Name, childValidatorNames));
			}
		}

		private static IEnumerable<IPropertyValidator> GetDependentRules<T, TProperty>(string expressionMemberName, Expression<Func<T, TProperty>> expression, IValidatorDescriptor descriptor) {
			var member = expression.IsParameterExpression() ? null : expressionMemberName;
			var rules = descriptor.GetRulesForMember(member).OfType<PropertyRule>().SelectMany(x => x.DependentRules)
				.SelectMany(x => x.Validators);

			return rules;
		}

		private static IPropertyValidator[] GetModelLevelValidators(IValidatorDescriptor descriptor) {
			var rules = descriptor.GetRulesForMember(null).OfType<PropertyRule>();
			return rules.Where(x => x.Expression.IsParameterExpression()).SelectMany(x => x.Validators)
				.ToArray();
		}

		private static TestValidationResult<T, TValue> TestValidate<T, TValue>(this IValidator<T> validator, Expression<Func<T, TValue>> expression, T instanceToValidate, TValue value, string ruleSet = null, bool setProperty=true) where T : class {
			var memberAccessor = new MemberAccessor<T, TValue>(expression, setProperty);

			if (setProperty) {
				memberAccessor.Set(instanceToValidate, value);
			}

			var validationResult = validator.Validate(instanceToValidate, null, ruleSet: ruleSet);

			return new TestValidationResult<T, TValue>(validationResult, memberAccessor);
		}

		public static TestValidationResult<T, T> TestValidate<T>(this IValidator<T> validator, T objectToTest, string ruleSet = null) where T : class {
			var validationResult = validator.Validate(objectToTest, null, ruleSet: ruleSet);

			return new TestValidationResult<T, T>(validationResult, (Expression<Func<T, T>>) (o => o));
		}

		public static IEnumerable<ValidationFailure> ShouldHaveError<T, TValue>(this TestValidationResult<T, TValue> testValidationResult) where T : class {
			return testValidationResult.Which.ShouldHaveValidationError();
		}

		public static void ShouldNotHaveError<T, TValue>(this TestValidationResult<T, TValue> testValidationResult) where T : class {
			testValidationResult.Which.ShouldNotHaveValidationError();
		}

		private static string BuildErrorMessage(ValidationFailure failure, string exceptionMessage, string defaultMessage) {
			if (exceptionMessage != null && failure != null) {
				return exceptionMessage.Replace("{Code}", failure.ErrorCode)
					.Replace("{Message}", failure.ErrorMessage)
					.Replace("{State}", failure.CustomState?.ToString() ?? "")
					.Replace("{Severity}", failure.Severity.ToString());
			}
			return defaultMessage;
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

		public static IEnumerable<ValidationFailure> WithCustomState(this IEnumerable<ValidationFailure> failures, object expectedCustomState) {
			return failures.When(failure => failure.CustomState == expectedCustomState, string.Format("Expected custom state of '{0}'. Actual state was '{{State}}'", expectedCustomState));
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
	}
}