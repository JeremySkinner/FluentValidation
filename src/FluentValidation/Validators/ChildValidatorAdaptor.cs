namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Features;
	using Internal;
	using Results;

	/// <summary>
	/// Indicates that this validator wraps another validator.
	/// </summary>
	public interface IChildValidatorAdaptor : ISupportsConditions, ISupportsAsyncConditions, ISupportsPropertyNameOverride {
		/// <summary>
		/// The type of the underlying validator
		/// </summary>
		Type ValidatorType { get; }
	}

	public class ChildValidatorAdaptor<T,TProperty> : IPropertyValidator<T,TProperty>, IAsyncPropertyValidator<T, TProperty>, IChildValidatorAdaptor {
		private readonly Func<ValidationContext<T>, TProperty, IValidator<TProperty>> _validatorProvider;
		private readonly IValidator<TProperty> _validator;

		public string Name => "ChildValidatorAdaptor";

		public Type ValidatorType { get; }

		public string[] RuleSets { get; set; }

		internal bool PassThroughParentContext { get; set; }

		string IPropertyValidator.GetDefaultMessageTemplate(string errorCode) => null;

		public ChildValidatorAdaptor(IValidator<TProperty> validator, Type validatorType) {
			_validator = validator;
			ValidatorType = validatorType;
		}

		public ChildValidatorAdaptor(Func<ValidationContext<T>, TProperty, IValidator<TProperty>> validatorProvider, Type validatorType) {
			_validatorProvider = validatorProvider;
			ValidatorType = validatorType;
		}

		public virtual bool IsValid(ValidationContext<T> context, TProperty value) {
			if (value == null) {
				return true;
			}

			var validator = GetValidator(context, value);

			if (validator == null) {
				return true;
			}

			var newContext = CreateNewValidationContextForChildValidator(context, value);
			var totalFailures = context.Failures.Count;

			// If we're inside a collection with RuleForEach, then preserve the CollectionIndex placeholder
			// and pass it down to child validator by caching it in the RootContextData which flows through to
			// the child validator. PropertyValidator.PrepareMessageFormatterForValidationError handles extracting this.
			HandleCollectionIndex(context, out object originalIndex, out object currentIndex);

			validator.Validate(newContext);

			// Reset the collection index
			ResetCollectionIndex(context, originalIndex, currentIndex);
			return true;
		}

		public virtual async Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation) {
			if (value == null) {
				return true;
			}

			var validator = GetValidator(context, value);

			if (validator == null) {
				return true;
			}

			var newContext = CreateNewValidationContextForChildValidator(context, value);
			var totalFailures = context.Failures.Count;

			// If we're inside a collection with RuleForEach, then preserve the CollectionIndex placeholder
			// and pass it down to child validator by caching it in the RootContextData which flows through to
			// the child validator. PropertyValidator.PrepareMessageFormatterForValidationError handles extracting this.
			HandleCollectionIndex(context, out object originalIndex, out object currentIndex);

			await validator.ValidateAsync(newContext, cancellation);

			ResetCollectionIndex(context, originalIndex, currentIndex);

			return true;
		}

		public virtual IValidator GetValidator(ValidationContext<T> context, TProperty value) {
			context.Guard("Cannot pass a null context to GetValidator", nameof(context));

			return _validatorProvider != null ? _validatorProvider(context, value) : _validator;
		}

		protected virtual IValidationContext CreateNewValidationContextForChildValidator(ValidationContext<T> context, TProperty value) {
			var selector = GetSelector(context, value);
			var newContext = context.CloneForChildValidator(value, PassThroughParentContext, selector);

			if(!context.IsChildCollectionContext)
				newContext.PropertyChain.Add(context.RawPropertyName);

			return newContext;
		}

		private protected virtual IValidatorSelector GetSelector(ValidationContext<T> context, TProperty value) {
			return RuleSets?.Length > 0 ? new RulesetValidatorSelector(RuleSets) : null;
		}

		private void HandleCollectionIndex(ValidationContext<T> context, out object originalIndex, out object index) {
			originalIndex = null;
			if (context.MessageFormatter.PlaceholderValues.TryGetValue("CollectionIndex", out index)) {
				context.RootContextData.TryGetValue("__FV_CollectionIndex", out originalIndex);
				context.RootContextData["__FV_CollectionIndex"] = index;
			}
		}

		private void ResetCollectionIndex(ValidationContext<T> context, object originalIndex, object index) {
			// Reset the collection index
			if (index != null) {
				if (originalIndex != null) {
					context.RootContextData["__FV_CollectionIndex"] = originalIndex;
				}
				else {
					context.RootContextData.Remove("__FV_CollectionIndex");
				}
			}
		}
	}
}
