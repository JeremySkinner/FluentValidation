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
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
	using static MvcValidationHelper;

	internal class FluentValidationVisitor : ValidationVisitor {
		public FluentValidationVisitor(ActionContext actionContext, IModelValidatorProvider validatorProvider, ValidatorCache validatorCache, IModelMetadataProvider metadataProvider, ValidationStateDictionary validationState)
			: base(actionContext, validatorProvider, validatorCache, metadataProvider, validationState) {
			ValidateComplexTypesIfChildValidationFails = true;
		}

		// This overload needs to be in place for both .NET 5 and .NET Core 2/3
		public override bool Validate(ModelMetadata metadata, string key, object model, bool alwaysValidateAtTopLevel) {
			bool BaseValidate()
				=> base.Validate(metadata, key, model, alwaysValidateAtTopLevel);

			return ValidateInternal(metadata, key, model, BaseValidate);
		}

#if !NETCOREAPP3_1
		// .NET 5+ has this additional overload as an entry point.
		public override bool Validate(ModelMetadata metadata, string key, object model, bool alwaysValidateAtTopLevel, object container) {
			bool BaseValidate()
				=> base.Validate(metadata, key, model, alwaysValidateAtTopLevel, container);

			return ValidateInternal(metadata, key, model, BaseValidate);
		}
#endif

		private bool ValidateInternal(ModelMetadata metadata, string key, object model, Func<bool> continuation) {
			SetRootMetadata(Context, metadata);

			// Store and remove any implicit required messages.
			// Later we'll re-add those that are still relevant.
			var requiredErrorsNotHandledByFv = RemoveImplicitRequiredErrors(Context);

			// Apply any customizations made with the CustomizeValidatorAttribute
			if (model != null) {
				CacheCustomizations(Context, model, key);
			}

			var result = continuation();

			// Re-add errors that we took out if FV didn't add a key.
			ReApplyImplicitRequiredErrorsNotHandledByFV(requiredErrorsNotHandledByFv);

			// Remove duplicates. This can happen if someone has implicit child validation turned on and also adds an explicit child validator.
			RemoveDuplicateModelstateEntries(Context);

			return result;
		}

	}
}
