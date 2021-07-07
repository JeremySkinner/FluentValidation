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

	/// <summary>
	/// This exception is thrown when an asynchronous validator is executed synchronously.
	/// </summary>
	public class AsyncValidatorInvokedSynchronouslyException : InvalidOperationException {
		public Type ValidatorType { get; }

		internal AsyncValidatorInvokedSynchronouslyException() {
		}

		internal AsyncValidatorInvokedSynchronouslyException(Type validatorType, bool wasInvokedByAspNet)
			: base(BuildMessage(validatorType, wasInvokedByAspNet)) {
			ValidatorType = validatorType;
		}

		internal AsyncValidatorInvokedSynchronouslyException(string message) : base(message) {
		}

		private static string BuildMessage(Type validatorType, bool wasInvokedByMvc) {
			if (wasInvokedByMvc) {
				return $"Validator \"{validatorType.Name}\" can't be used with ASP.NET automatic validation as it contains asynchronous rules. ASP.NET's validation pipeline is not asynchronous and can't invoke asynchronous rules. Remove the asynchronous rules in order for this validator to run.";
			}

			return $"Validator \"{validatorType.Name}\" contains asynchronous rules but was invoked synchronously. Please call ValidateAsync rather than Validate.";
		}
	}
}
