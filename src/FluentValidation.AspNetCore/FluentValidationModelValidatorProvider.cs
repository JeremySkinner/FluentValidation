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

namespace FluentValidation.AspNetCore {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Internal;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
	using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
	using Microsoft.Extensions.DependencyInjection;
	using Results;

	/// <summary>
	/// ModelValidatorProvider implementation only used for child properties.
	/// </summary>
	public class FluentValidationModelValidatorProvider : IModelValidatorProvider {
		private readonly bool _implicitValidationEnabled;
		private readonly bool _implicitRootCollectionElementValidationEnabled;

		public FluentValidationModelValidatorProvider(bool implicitValidationEnabled)
			: this(implicitValidationEnabled, false) {
		}

		public FluentValidationModelValidatorProvider(
			bool implicitValidationEnabled,
			bool implicitRootCollectionElementValidationEnabled) {
			_implicitValidationEnabled = implicitValidationEnabled;
			_implicitRootCollectionElementValidationEnabled = implicitRootCollectionElementValidationEnabled;
		}

		public virtual void CreateValidators(ModelValidatorProviderContext context) {
			context.Results.Add(new ValidatorItem {
				IsReusable = false,
				Validator = new FluentValidationModelValidator(_implicitValidationEnabled, _implicitRootCollectionElementValidationEnabled),
			});
		}
	}

	/// <summary>
	/// FluentValidation's implementation of an ASP.NET Core model validator.
	/// </summary>
	public class FluentValidationModelValidator : IModelValidator {
		private readonly bool _implicitValidationEnabled;
		private readonly bool _implicitRootCollectionElementValidationEnabled;

		public FluentValidationModelValidator(bool implicitValidationEnabled)
			: this(implicitValidationEnabled, false) {
		}

		public FluentValidationModelValidator(
			bool implicitValidationEnabled,
			bool implicitRootCollectionElementValidationEnabled) {
			_implicitValidationEnabled = implicitValidationEnabled;
			_implicitRootCollectionElementValidationEnabled = implicitRootCollectionElementValidationEnabled;
		}

		public virtual IEnumerable<ModelValidationResult> Validate(ModelValidationContext mvContext) {
			if (ShouldSkip(mvContext)) {
				return Enumerable.Empty<ModelValidationResult>();
			}

			var factory = mvContext.ActionContext.HttpContext.RequestServices.GetService(typeof(IValidatorFactory)) as IValidatorFactory;
			var validator = factory?.GetValidator(mvContext.ModelMetadata.ModelType);

			if (validator != null) {
				var customizations = GetCustomizations(mvContext.ActionContext, mvContext.Model);

				if (customizations.Skip) {
					return Enumerable.Empty<ModelValidationResult>();
				}

				if (mvContext.Container != null) {
					var containerCustomizations = GetCustomizations(mvContext.ActionContext, mvContext.Container);
					if (containerCustomizations.Skip) {
						return Enumerable.Empty<ModelValidationResult>();
					}
				}

				var selector = customizations.ToValidatorSelector(mvContext);
				var interceptor = customizations.GetInterceptor()
				                  ?? validator as IValidatorInterceptor
				                  ?? mvContext.ActionContext.HttpContext.RequestServices.GetService<IValidatorInterceptor>();

				IValidationContext context = new ValidationContext<object>(mvContext.Model, new PropertyChain(), selector);
				context.RootContextData["InvokedByMvc"] = true;
				context.SetServiceProvider(mvContext.ActionContext.HttpContext.RequestServices);

				if (interceptor != null) {
					// Allow the user to provide a customized context
					// However, if they return null then just use the original context.
					context = interceptor.BeforeAspNetValidation(mvContext.ActionContext, context) ?? context;
				}


				ValidationResult result;

				try {
					result = validator.Validate(context);
				}
				catch (AsyncValidatorInvokedSynchronouslyException ex) {
					throw new AsyncValidatorInvokedSynchronouslyException($"The validator \"{ex.ValidatorType.Name}\" can't be used with ASP.NET automatic validation as it contains asynchronous rules.");
				}

				if (interceptor != null) {
					// allow the user to provide a custom collection of failures, which could be empty.
					// However, if they return null then use the original collection of failures.
					result = interceptor.AfterAspNetValidation(mvContext.ActionContext, context, result) ?? result;
				}

				return result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage));
			}

			return Enumerable.Empty<ModelValidationResult>();
		}

		protected bool ShouldSkip(ModelValidationContext mvContext) {
			// Skip if there's nothing to process.
			if (mvContext.Model == null) {
				return true;
			}

			// If implicit validation is disabled, then we want to only validate the root object.
			if (!_implicitValidationEnabled) {
				var rootMetadata = GetRootMetadata(mvContext);

				// We should always have root metadata, so this should never happen...
				if (rootMetadata == null) return true;

				var modelMetadata = mvContext.ModelMetadata;

				// Careful when handling properties.
				// If we're processing a property of our root object,
				// then we always skip if implicit validation is disabled
				// However if our root object *is* a property (because of [BindProperty])
				// then this is OK to proceed.
				if (modelMetadata.MetadataKind == ModelMetadataKind.Property) {
					if (!ReferenceEquals(rootMetadata, modelMetadata)) {
						// The metadata for the current property is not the same as the root metadata
						// This means we're validating a property on a model, so we want to skip.
						return true;
					}
				}

				// If we're handling a type, we need to make sure we're handling the root type.
				// When MVC encounters child properties, it will set the MetadataKind to Type,
				// so we can't use the MetadataKind to differentiate the root from the child property.
				// Instead check if our cached root metadata is the same.
				// If they're not, then it means we're handling a child property, so we should skip
				// validation if implicit validation is disabled
				else if (modelMetadata.MetadataKind == ModelMetadataKind.Type) {
					// If implicit validation of root collection elements is enabled then we
					// do want to validate the type if it matches the element type of the root collection
					if (_implicitRootCollectionElementValidationEnabled && IsRootCollectionElementType(rootMetadata, modelMetadata.ModelType)) {
						return false;
					}

					if (!ReferenceEquals(rootMetadata, modelMetadata)) {
						// The metadata for the current type is not the same as the root metadata
						// This means we're validating a child element of a collection or sub property.
						// Skip it as implicit validation is disabled.
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the metadata object for the root object being validated.
		/// </summary>
		/// <param name="mvContext">MVC Validation context</param>
		/// <returns>Metadata instance.</returns>
		protected static ModelMetadata GetRootMetadata(ModelValidationContext mvContext) {
			return MvcValidationHelper.GetRootMetadata(mvContext);
		}

		/// <summary>
		/// Gets customizations associated with this validation request.
		/// </summary>
		/// <param name="context">Current action context</param>
		/// <param name="model">The object being validated</param>
		/// <returns>Customizations</returns>
		protected static CustomizeValidatorAttribute GetCustomizations(ActionContext context, object model) {
			return MvcValidationHelper.GetCustomizations(context, model);
		}

		private static bool IsRootCollectionElementType(ModelMetadata rootMetadata, Type modelType) {
			if (!rootMetadata.IsEnumerableType)
				return false;

			return modelType == rootMetadata.ElementType;
		}
	}
}
