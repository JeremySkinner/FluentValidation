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
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Features;
	using Internal;

#pragma warning disable 618
	internal class LegacyValidatorAdaptor<T, TProperty> : IPropertyValidator<T, TProperty>, IAsyncPropertyValidator<T, TProperty> {

		private PropertyValidator _inner;

		public LegacyValidatorAdaptor(PropertyValidator inner) {
			_inner = inner;
		}

		public bool IsValid(ValidationContext<T> context, TProperty value) {
			var pvc = new PropertyValidatorContext(context, context.PropertyName, value, () => context.DisplayName);
			return _inner.IsValidInternal(pvc);
		}

		public Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation) {
			var pvc = new PropertyValidatorContext(context, context.PropertyName, value, () => context.DisplayName);
			return _inner.IsValidInternalAsync(pvc, cancellation);
		}

		public string Name => ((IPropertyValidator) _inner).Name;

		public string GetDefaultMessageTemplate(string errorCode)
			=> ((IPropertyValidator) _inner).GetDefaultMessageTemplate(errorCode);
	}

	[Obsolete("The PropertyValidator class is deprecated and will be removed in FluentValidation 11. Please migrate to the generic PropertyValidator<T,TProperty> class.")]
	public abstract class PropertyValidator : IPropertyValidator, IStandardFeatures {

		internal bool IsValidInternal(PropertyValidatorContext context)
			=> IsValid(context);

		internal Task<bool> IsValidInternalAsync(PropertyValidatorContext context, CancellationToken cancellation)
			=> IsValidAsync(context, cancellation);

		protected abstract bool IsValid(PropertyValidatorContext context);

		protected virtual Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return Task.FromResult(IsValid(context));
		}

		string IPropertyValidator.Name => GetType().Name;

		string IPropertyValidator.GetDefaultMessageTemplate(string errorCode)
			=> GetDefaultMessageTemplate();

		protected virtual string GetDefaultMessageTemplate()
			=> "No default error message has been specified";

	}

	[Obsolete("The PropertyValidatorContext class is deprecated and will be removed in FluentValidation 11. Please switch to using PropertyValidator<T,TProperty> for custom property validators.")]
	public class PropertyValidatorContext {
		private readonly Func<string> _displayNameFunc;
		private MessageFormatter _messageFormatter;
		public IValidationContext ParentContext { get; }
		public string PropertyName { get; }
		public string DisplayName => _displayNameFunc();
		public object InstanceToValidate => ParentContext.InstanceToValidate;
		public MessageFormatter MessageFormatter => _messageFormatter ??= ValidatorOptions.Global.MessageFormatterFactory();
		public object PropertyValue { get; }

		internal PropertyValidatorContext(IValidationContext parentContext, string propertyName, object propertyValue, Func<string> displayNameFunc) {
			_displayNameFunc = displayNameFunc;
			ParentContext = parentContext;
			PropertyName = propertyName;
			PropertyValue = propertyValue;
		}
	}
}
