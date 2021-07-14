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
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.Abstractions;
	using Microsoft.AspNetCore.Mvc.Controllers;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
	using Microsoft.AspNetCore.Mvc.RazorPages;
	using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
	using Microsoft.Extensions.DependencyInjection;

	/// <summary>
	/// Utilities for working around limitations of the MVC validation api.
	/// Used by <see cref="FluentValidationVisitor"/>
	/// </summary>
	internal static class MvcValidationHelper {
		internal static void SetRootMetadata(ActionContext context, ModelMetadata metadata) {
			context.HttpContext.Items["_FV_ROOT_METADATA"] = metadata;
		}

		internal static ModelMetadata GetRootMetadata(ModelValidationContext context) {
			if (context.ActionContext.HttpContext.Items
				.TryGetValue("_FV_ROOT_METADATA", out var rootMetadata)) {
				return rootMetadata as ModelMetadata;
			}

			return null;
		}

		internal static List<KeyValuePair<ModelStateEntry, ModelError>> RemoveImplicitRequiredErrors(ActionContext actionContext) {
			// This is all to work around the default "Required" messages.
			var requiredErrorsNotHandledByFv = new List<KeyValuePair<ModelStateEntry, ModelError>>();

			foreach (KeyValuePair<string, ModelStateEntry> entry in actionContext.ModelState) {
				List<ModelError> errorsToModify = new List<ModelError>();

				if (entry.Value.ValidationState == ModelValidationState.Invalid) {
					foreach (var err in entry.Value.Errors) {
						if (err.ErrorMessage.StartsWith(FluentValidationBindingMetadataProvider.Prefix)) {
							errorsToModify.Add(err);
						}
					}

					foreach (ModelError err in errorsToModify) {
						entry.Value.Errors.Clear();
						entry.Value.ValidationState = ModelValidationState.Unvalidated;
						requiredErrorsNotHandledByFv.Add(new KeyValuePair<ModelStateEntry, ModelError>(entry.Value, new ModelError(err.ErrorMessage.Replace(FluentValidationBindingMetadataProvider.Prefix, string.Empty))));
					}
				}
			}

			return requiredErrorsNotHandledByFv;
		}

		internal static CustomizeValidatorAttribute GetCustomizations(ActionContext actionContext, Type type, string prefix) {

			IList<TDescriptor> FilterParameterDescriptors<TDescriptor>(IList<ParameterDescriptor> parameters) {
				return parameters
					.Where(x => x.ParameterType == type)
					.Where(x => (x.BindingInfo != null && x.BindingInfo.BinderModelName != null && x.BindingInfo.BinderModelName == prefix) || x.Name == prefix || (prefix == string.Empty && x.BindingInfo?.BinderModelName == null))
					.OfType<TDescriptor>()
					.ToList();
			}

			CustomizeValidatorAttribute attribute = null;

			if (actionContext is ControllerContext controllerContext && controllerContext.ActionDescriptor?.Parameters != null) {

				var descriptors = FilterParameterDescriptors<ControllerParameterDescriptor>(actionContext.ActionDescriptor.Parameters);

				if (descriptors.Count == 1) {
					attribute = descriptors[0].ParameterInfo.GetCustomAttributes(typeof(CustomizeValidatorAttribute), true).FirstOrDefault() as CustomizeValidatorAttribute;
				}
			}
			else if (actionContext is PageContext pageContext && pageContext.ActionDescriptor?.BoundProperties != null) {

				var descriptors = FilterParameterDescriptors<PageBoundPropertyDescriptor>(pageContext.ActionDescriptor.BoundProperties);

				if (descriptors.Count == 1) {
					attribute = descriptors[0].Property.GetCustomAttributes(typeof(CustomizeValidatorAttribute), true).FirstOrDefault() as CustomizeValidatorAttribute;
				}
			}

			return attribute ?? new CustomizeValidatorAttribute();
		}

		internal static void CacheCustomizations(ActionContext context, object model, string key) {
			var customizations = GetCustomizations(context, model.GetType(), key);
			context.HttpContext.Items["_FV_Customizations"] = (model, customizations);
		}

		internal static void ReApplyImplicitRequiredErrorsNotHandledByFV(List<KeyValuePair<ModelStateEntry, ModelError>> requiredErrorsNotHandledByFv) {
			foreach (var pair in requiredErrorsNotHandledByFv) {
				if (pair.Key.ValidationState != ModelValidationState.Invalid) {
					pair.Key.Errors.Add(pair.Value);
					pair.Key.ValidationState = ModelValidationState.Invalid;
				}
			}
		}

		internal static void RemoveDuplicateModelstateEntries(ActionContext actionContext) {
			foreach (var entry in actionContext.ModelState) {
				if (entry.Value.ValidationState == ModelValidationState.Invalid) {
					var existing = new HashSet<string>();

					foreach (var err in entry.Value.Errors.ToList()) {
						//ToList to create a copy so we can remove from the original
						if (existing.Contains(err.ErrorMessage)) {
							entry.Value.Errors.Remove(err);
						}
						else {
							existing.Add(err.ErrorMessage);
						}
					}
				}
			}
		}

		internal static CustomizeValidatorAttribute GetCustomizations(ActionContext ctx, object model) {
			if (ctx.HttpContext.Items["_FV_Customizations"] is ValueTuple<object, CustomizeValidatorAttribute> customizations
			    && ReferenceEquals(model, customizations.Item1)) {
				return customizations.Item2; // the attribute
			}
			return new CustomizeValidatorAttribute();
		}

		internal static ValidatorConfiguration GetValidatorConfiguration(this IServiceProvider serviceProvider) {
			return serviceProvider.GetRequiredService<ValidatorConfiguration>();
		}

	}
}
