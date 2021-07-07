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
	using Validators;

	/// <summary>
	/// An individual component within a rule with a validator attached.
	/// </summary>
	public interface IRuleComponent<T, out TProperty> : IRuleComponent {
		/// <summary>
		/// The error code associated with this rule component.
		/// </summary>
		new string ErrorCode { get; set; }

		/// <summary>
		/// Function used to retrieve custom state for the validator
		/// </summary>
		Func<ValidationContext<T>, TProperty, object> CustomStateProvider { set; }

		/// <summary>
		/// Function used to retrieve the severity for the validator
		/// </summary>
		Func<ValidationContext<T>, TProperty, Severity> SeverityProvider { set; }

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		void ApplyCondition(Func<ValidationContext<T>, bool> condition);

		/// <summary>
		/// Adds a condition for this validator. If there's already a condition, they're combined together with an AND.
		/// </summary>
		/// <param name="condition"></param>
		void ApplyAsyncCondition(Func<ValidationContext<T>, CancellationToken, Task<bool>> condition);

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorFactory">A function for retrieving the error message template.</param>
		void SetErrorMessage(Func<ValidationContext<T>, TProperty, string> errorFactory);

		/// <summary>
		/// Sets the overridden error message template for this validator.
		/// </summary>
		/// <param name="errorMessage">The error message to set</param>
		void SetErrorMessage(string errorMessage);
	}

	/// <summary>
	/// An individual component within a rule with a validator attached.
	/// </summary>
	public interface IRuleComponent {
		/// <summary>
		/// Whether or not this validator has a condition associated with it.
		/// </summary>
		bool HasCondition { get; }

		/// <summary>
		/// Whether or not this validator has an async condition associated with it.
		/// </summary>
		bool HasAsyncCondition { get; }

		/// <summary>
		/// The validator associated with this component.
		/// </summary>
		IPropertyValidator Validator { get; }

		/// <summary>
		/// Gets the raw unformatted error message. Placeholders will not have been rewritten.
		/// </summary>
		/// <returns></returns>
		string GetUnformattedErrorMessage();

		/// <summary>
		/// The error code associated with this rule component.
		/// </summary>
		string ErrorCode { get; }
	}
}
