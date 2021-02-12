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
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Validator implementation that allows rules to be defined without inheriting from AbstractValidator.
	/// </summary>
	/// <example>
	/// <code>
	/// public class Customer {
	///   public int Id { get; set; }
	///   public string Name { get; set; }
	///
	///   public static readonly InlineValidator&lt;Customer&gt; Validator = new InlineValidator&lt;Customer&gt; {
	///     v =&gt; v.RuleFor(x =&gt; x.Name).NotNull(),
	///     v =&gt; v.RuleFor(x =&gt; x.Id).NotEqual(0),
	///   }
	/// }
	/// </code>
	/// </example>
	/// <typeparam name="T"></typeparam>
	public class InlineValidator<T> : AbstractValidator<T> {
		/// <summary>
		/// Allows configuration of the validator.
		/// </summary>
		public void Add<TProperty, TValidator>(Func<InlineValidator<T>, IRuleBuilderOptions<T, TProperty, TValidator>> ruleCreator) {
			ruleCreator(this);
		}

		/// <summary>
		/// Defines a validation rule for a specify property.
		/// </summary>
		/// <example>
		/// RuleFor(x => x.Surname)...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <param name="expression">The expression representing the property to validate</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression) {
			return base.RuleFor(expression);
		}

		/// <summary>
		/// Defines a validation rule for a specify property and transform it to a different type.
		/// </summary>
		/// <example>
		/// Transform(x => x.OrderNumber, to: orderNumber => orderNumber.ToString())...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="from">The expression representing the property to transform</param>
		/// <param name="to">Function to transform the property value into a different type</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitial<T, TTransformed> Transform<TProperty, TTransformed>(Expression<Func<T, TProperty>> from, Func<TProperty, TTransformed> to) {
			return base.Transform(from, to);
		}

		/// <summary>
		/// Defines a validation rule for a specify property and transform it to a different type.
		/// </summary>
		/// <example>
		/// Transform(x => x.OrderNumber, to: orderNumber => orderNumber.ToString())...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="from">The expression representing the property to transform</param>
		/// <param name="to">Function to transform the property value into a different type</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitial<T, TTransformed> Transform<TProperty, TTransformed>(Expression<Func<T, TProperty>> from, Func<T, TProperty, TTransformed> to) {
			return base.Transform(from, to);
		}

		/// <summary>
		/// Invokes a rule for each item in the collection.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitialCollection<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression) {
			return base.RuleForEach(expression);
		}

		/// <summary>
		/// Invokes a rule for each item in the collection, transforming the element from one type to another.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <param name="to">Function to transform the collection element into a different type</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitialCollection<T, TTransformed> TransformForEach<TElement, TTransformed>(Expression<Func<T, IEnumerable<TElement>>> expression, Func<TElement, TTransformed> to) {
			return base.TransformForEach(expression, to);
		}

		/// <summary>
		/// Invokes a rule for each item in the collection, transforming the element from one type to another.
		/// </summary>
		/// <typeparam name="TElement">Type of property</typeparam>
		/// <typeparam name="TTransformed">The type after the transformer has been applied</typeparam>
		/// <param name="expression">Expression representing the collection to validate</param>
		/// <param name="to">Function to transform the collection element into a different type</param>
		/// <returns>An IRuleBuilder instance on which validators can be defined</returns>
		public new IRuleBuilderInitialCollection<T, TTransformed> TransformForEach<TElement, TTransformed>(Expression<Func<T, IEnumerable<TElement>>> expression, Func<T, TElement, TTransformed> to) {
			return base.TransformForEach(expression, to);
		}

		/// <summary>
		/// Defines a RuleSet that can be used to group together several validators.
		/// </summary>
		/// <param name="ruleSetName">The name of the ruleset.</param>
		/// <param name="action">Action that encapsulates the rules in the ruleset.</param>
		public new void RuleSet(string ruleSetName, Action action) {
			base.RuleSet(ruleSetName, action);
		}

		/// <summary>
		/// Defines a condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public new IConditionBuilder When(Func<T, bool> predicate, Action action) {
			return base.When(predicate, action);
		}

		/// <summary>
		/// Defines a condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public new IConditionBuilder When(Func<T, ValidationContext<T>, bool> predicate, Action action) {
			return base.When(predicate, action);
		}

		/// <summary>
		/// Defines an inverse condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public new IConditionBuilder Unless(Func<T, bool> predicate, Action action) {
			return base.Unless(predicate, action);
		}

		/// <summary>
		/// Defines an inverse condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public new IConditionBuilder Unless(Func<T, ValidationContext<T>, bool> predicate, Action action) {
			return base.Unless(predicate, action);
		}

		/// <summary>
		/// Defines an asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public new IConditionBuilder WhenAsync(Func<T, CancellationToken, Task<bool>> predicate, Action action) {
			return base.WhenAsync(predicate, action);
		}

		/// <summary>
		/// Defines an asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should apply to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules.</param>
		/// <returns></returns>
		public new IConditionBuilder WhenAsync(Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, Action action) {
			return base.WhenAsync(predicate, action);
		}

		/// <summary>
		/// Defines an inverse asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public new IConditionBuilder UnlessAsync(Func<T, CancellationToken, Task<bool>> predicate, Action action) {
			return base.UnlessAsync(predicate, action);
		}

		/// <summary>
		/// Defines an inverse asynchronous condition that applies to several rules
		/// </summary>
		/// <param name="predicate">The asynchronous condition that should be applied to multiple rules</param>
		/// <param name="action">Action that encapsulates the rules</param>
		public new IConditionBuilder UnlessAsync(Func<T, ValidationContext<T>, CancellationToken, Task<bool>> predicate, Action action) {
			return base.UnlessAsync(predicate, action);
		}

		/// <summary>
		/// Includes the rules from the specified validator
		/// </summary>
		public new void Include(IValidator<T> rulesToInclude) {
			base.Include(rulesToInclude);
		}

		/// <summary>
		/// Includes the rules from the specified validator
		/// </summary>
		public new void Include<TValidator>(Func<T, TValidator> rulesToInclude) where TValidator : IValidator<T> {
			base.Include(rulesToInclude);
		}
	}
}
