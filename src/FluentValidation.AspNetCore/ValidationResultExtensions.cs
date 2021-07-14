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
	using FluentValidation.Internal;
	using FluentValidation.Results;
	using Microsoft.AspNetCore.Http;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.ModelBinding;
	using Microsoft.AspNetCore.Mvc.RazorPages;

	public static class ValidationResultExtension {

		private const string _rulesetKey = "_FV_ClientSideRuleSet";

		/// <summary>
		/// Stores the errors in a ValidationResult object to the specified modelstate dictionary.
		/// </summary>
		/// <param name="result">The validation result to store</param>
		/// <param name="modelState">The ModelStateDictionary to store the errors in.</param>
		/// <param name="prefix">An optional prefix. If ommitted, the property names will be the keys. If specified, the prefix will be concatenatd to the property name with a period. Eg "user.Name"</param>
		public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState, string prefix) {
			if (!result.IsValid) {
				foreach (var error in result.Errors) {
					string key = string.IsNullOrEmpty(prefix)
						? error.PropertyName
						: string.IsNullOrEmpty(error.PropertyName)
							? prefix
							: prefix + "." + error.PropertyName;
					modelState.AddModelError(key, error.ErrorMessage);
				}
			}
		}

		/// <summary>
		/// Sets the rulests used when generating clientside messages.
		/// </summary>
		/// <param name="context">Http context</param>
		/// <param name="ruleSets">Array of ruleset names</param>
		public static void SetRulesetForClientsideMessages(this HttpContext context, params string[] ruleSets) => context.Items[_rulesetKey] = ruleSets;

		/// <summary>
		/// Gets the rulesets used to generate clientside validation metadata.
		/// </summary>
		/// <param name="context">Http context</param>
		/// <returns>Array of ruleset names</returns>
		public static string[] GetRuleSetsForClientValidation(this HttpContext context) {
			// If the httpContext is null (for example, if IHttpContextProvider hasn't been registered) then just assume default ruleset.
			// This is OK because if we're actually using the attribute, the OnActionExecuting will have caught the fact that the provider is not registered.

			if (context?.Items != null && context.Items.ContainsKey(_rulesetKey) && context?.Items[_rulesetKey] is string[] ruleSets) {
				return ruleSets;
			}

			return new[] { RulesetValidatorSelector.DefaultRuleSetName };
		}

		/// <summary>
		/// Sets the rulests used when generating clientside messages.
		/// </summary>
		/// <param name="context">Controller context</param>
		/// <param name="ruleSets">Array of ruleset names</param>
		public static void SetRulesetForClientsideMessages(this ControllerContext context, params string[] ruleSets) => context.HttpContext.SetRulesetForClientsideMessages(ruleSets);

		/// <summary>
		/// Sets the rulests used when generating clientside messages.
		/// </summary>
		/// <param name="context">Page context</param>
		/// <param name="ruleSets">Array of ruleset names</param>
		public static void SetRulesetForClientsideMessages(this PageContext context, params string[] ruleSets) => context.HttpContext.SetRulesetForClientsideMessages(ruleSets);
	}
}
