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
	using System.Threading;
	using System.Threading.Tasks;
	using Validators;

	internal class RuleComponentForNullableStruct<T, TProperty> : RuleComponent<T, TProperty?> where TProperty : struct {
		private IPropertyValidator<T, TProperty> _propertyValidator;
		private IAsyncPropertyValidator<T, TProperty> _asyncPropertyValidator;

		internal RuleComponentForNullableStruct(IPropertyValidator<T, TProperty> propertyValidator)
			: base(null) {
			_propertyValidator = propertyValidator;
		}

		internal RuleComponentForNullableStruct(IAsyncPropertyValidator<T, TProperty> asyncPropertyValidator, IPropertyValidator<T, TProperty> propertyValidator)
			: base(null, null) {
			_asyncPropertyValidator = asyncPropertyValidator;
		}

		public override IPropertyValidator Validator
			=> (IPropertyValidator) _propertyValidator ?? _asyncPropertyValidator;

		private protected override bool SupportsAsynchronousValidation
			=> _asyncPropertyValidator != null;

		private protected override bool SupportsSynchronousValidation
			=> _propertyValidator != null;

		internal override bool Validate(ValidationContext<T> context, TProperty? value) {
			if (!value.HasValue) return true;
			return _propertyValidator.IsValid(context, value.Value);
		}

		internal override async Task<bool> ValidateAsync(ValidationContext<T> context, TProperty? value, CancellationToken cancellation) {
			if (!value.HasValue) return true;
			return await _asyncPropertyValidator.IsValidAsync(context, value.Value, cancellation);
		}
	}
}
