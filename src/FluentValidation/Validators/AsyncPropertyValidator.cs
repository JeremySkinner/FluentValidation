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

namespace FluentValidation.Validators {
	using System.Threading;
	using System.Threading.Tasks;
	using Features;
	using Internal;

	public abstract class AsyncPropertyValidator<T, TProperty> : IAsyncPropertyValidator<T, TProperty>, IStandardAsyncFeatures {
		string IPropertyValidator.GetDefaultMessageTemplate(string errorCode)
			=> GetDefaultMessageTemplate(errorCode);

		/// <inheritdoc />
		public abstract string Name { get; }

		/// <summary>
		/// Returns the default error message template for this validator, when not overridden.
		/// </summary>
		/// <param name="errorCode">The currently configured error code for the validator.</param>
		/// <returns></returns>
		protected virtual string GetDefaultMessageTemplate(string errorCode) => "No default error message has been specified";

		/// <inheritdoc />
		public abstract Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation);

		/// <summary>
		/// Retrieves a localized string from the LanguageManager.
		/// If an ErrorCode is defined for this validator, the error code is used as the key.
		/// If no ErrorCode is defined (or the language manager doesn't have a translation for the error code)
		/// then the fallback key is used instead.
		/// </summary>
		/// <param name="errorCode">The currently configured error code for the validator.</param>
		/// <param name="fallbackKey">The fallback key to use for translation, if no ErrorCode is available.</param>
		/// <returns>The translated error message template.</returns>
		protected string Localized(string errorCode, string fallbackKey) {
			return ValidatorOptions.Global.LanguageManager.ResolveErrorMessageUsingErrorCode(errorCode, fallbackKey);
		}
	}
}
