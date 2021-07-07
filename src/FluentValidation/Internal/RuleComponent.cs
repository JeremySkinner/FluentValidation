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
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Validators;

	/// <summary>
	/// An individual component within a rule.
	/// In a rule definition such as RuleFor(x => x.Name).NotNull().NotEqual("Foo")
	/// the NotNull and the NotEqual are both rule components.
	/// </summary>
	public class RuleComponent<T,TProperty> : IRuleComponent<T,TProperty> {
		private string _errorMessage;
		private Func<ValidationContext<T>, TProperty, string> _errorMessageFactory;
		private Func<ValidationContext<T>, bool> _condition;
		private Func<ValidationContext<T>, CancellationToken, Task<bool>> _asyncCondition;
		private readonly IPropertyValidator<T, TProperty> _propertyValidator;
		private readonly IAsyncPropertyValidator<T, TProperty> _asyncPropertyValidator;

		internal RuleComponent(IPropertyValidator<T, TProperty> propertyValidator) {
			_propertyValidator = propertyValidator;
		}

		internal RuleComponent(IAsyncPropertyValidator<T, TProperty> asyncPropertyValidator, IPropertyValidator<T, TProperty> propertyValidator) {
			_asyncPropertyValidator = asyncPropertyValidator;
			_propertyValidator = propertyValidator;
		}

		/// <inheritdoc />
		public bool HasCondition => _condition != null;

		/// <inheritdoc />
		public bool HasAsyncCondition => _asyncCondition != null;

		/// <inheritdoc />
		public virtual IPropertyValidator Validator
			=> (IPropertyValidator) _propertyValidator ?? _asyncPropertyValidator;

		private protected virtual bool SupportsAsynchronousValidation
			=> _asyncPropertyValidator != null;

		private protected virtual bool SupportsSynchronousValidation
			=> _propertyValidator != null;

		internal bool ShouldValidateAsynchronously(IValidationContext context) {
			// If ValidateAsync has been invoked on the root validator, then always prefer
			// the asynchronous property validator (if available).
			if (context.IsAsync) {
				if (SupportsAsynchronousValidation) {
					return true;
				}

				// Fallback to sync if no async validator available.
				return false;
			}

			// If Validate has been invoked on the root validator, then always prefer
			// the synchronous validator.
			if (SupportsSynchronousValidation) {
				return false;
			}

			// Fall back to sync-over-async if only an async validator is available.
			return true;
		}

		internal virtual bool Validate(ValidationContext<T> context, TProperty value)
			=> _propertyValidator.IsValid(context, value);

		internal virtual Task<bool> ValidateAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation)
			=> _asyncPropertyValidator.IsValidAsync(context, value, cancellation);

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		public void ApplyCondition(Func<ValidationContext<T>, bool> condition) {
			if (_condition == null) {
				_condition = condition;
			}
			else {
				var original = _condition;
				_condition = ctx => condition(ctx) && original(ctx);
			}
		}

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		public void ApplyAsyncCondition(Func<ValidationContext<T>, CancellationToken, Task<bool>> condition) {
			if (_asyncCondition == null) {
				_asyncCondition = condition;
			}
			else {
				var original = _asyncCondition;
				_asyncCondition = async (ctx, ct) => await condition(ctx, ct) && await original(ctx, ct);
			}
		}

		internal bool InvokeCondition(ValidationContext<T> context) {
			if (_condition != null) {
				return _condition(context);
			}

			return true;
		}

		internal async Task<bool> InvokeAsyncCondition(ValidationContext<T> context, CancellationToken token) {
			if (_asyncCondition != null) {
				return await _asyncCondition(context, token);
			}

			return true;
		}

		/// <summary>
		/// Function used to retrieve custom state for the validator
		/// </summary>
		public Func<ValidationContext<T>, TProperty, object> CustomStateProvider { get; set; }

		/// <summary>
		/// Function used to retrieve the severity for the validator
		/// </summary>
		public Func<ValidationContext<T>, TProperty, Severity> SeverityProvider { get; set; }

		/// <summary>
		/// Retrieves the error code.
		/// </summary>
		public string ErrorCode { get; set; }

		/// <summary>
		/// Gets the error message. If a context is supplied, it will be used to format the message if it has placeholders.
		/// If no context is supplied, the raw unformatted message will be returned, containing placeholders.
		/// </summary>
		/// <param name="context">The validation context.</param>
		/// <param name="value">The current property value.</param>
		/// <returns>Either the formatted or unformatted error message.</returns>
		public string GetErrorMessage(ValidationContext<T> context, TProperty value) {
			// Use a custom message if one has been specified.
			string rawTemplate = _errorMessageFactory?.Invoke(context, value) ?? _errorMessage;


			// If no custom message has been supplied, use the default.
			if (rawTemplate == null) {
				rawTemplate = Validator.GetDefaultMessageTemplate(ErrorCode);
			}

			if (context == null) {
				return rawTemplate;
			}

			return context.MessageFormatter.BuildMessage(rawTemplate);
		}

		/// <summary>
		/// Gets the raw unformatted error message. Placeholders will not have been rewritten.
		/// </summary>
		/// <returns></returns>
		public string GetUnformattedErrorMessage() {
			string message = _errorMessageFactory?.Invoke(null, default) ?? _errorMessage;

			// If no custom message has been supplied, use the default.
			if (message == null) {
				message = Validator.GetDefaultMessageTemplate(ErrorCode);
			}

			return message;
		}

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorFactory">A function for retrieving the error message template.</param>
		public void SetErrorMessage(Func<ValidationContext<T>, TProperty, string> errorFactory) {
			_errorMessageFactory = errorFactory;
			_errorMessage = null;
		}

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorMessage">The error message to set</param>
		public void SetErrorMessage(string errorMessage) {
			_errorMessage = errorMessage;
			_errorMessageFactory = null;
		}
	}

}
