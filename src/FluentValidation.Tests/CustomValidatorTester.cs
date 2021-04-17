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

namespace FluentValidation.Tests {
	using System.Linq;
	using System.Threading.Tasks;
	using Xunit;

	public class CustomValidatorTester {
		private TestValidator validator;
		public CustomValidatorTester() {
			validator = new TestValidator();
		}

		[Fact]
		public void New_Custom_Returns_single_failure() {
			validator
				.RuleFor(x => x)
				.Custom((x, context) => {
					context.AddFailure("Surname", "Fail");
				});


			var result = validator.Validate(new Person());

			result.IsValid.ShouldBeFalse();
			result.Errors[0].ErrorMessage.ShouldEqual("Fail");
			result.Errors[0].PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public async Task New_Custom_Returns_single_failure_async() {
			validator
				.RuleFor(x => x)
				.CustomAsync((x, context, cancel) => {
					context.AddFailure("Surname", "Fail");
					return Task.CompletedTask;
				});

			var result = await validator.ValidateAsync(new Person());

			result.IsValid.ShouldBeFalse();
			result.Errors[0].ErrorMessage.ShouldEqual("Fail");
			result.Errors[0].PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public void Perserves_property_chain_using_custom() {
			validator.RuleForEach(x => x.Orders).SetValidator(new NestedOrderValidator());
			var person = new Person();
			person.Orders.Add(new Order());
			var result = validator.Validate(person);

			result.Errors.Single().PropertyName.ShouldEqual("Orders[0].Amount");
		}

		[Fact]
		public void New_Custom_within_ruleset() {
			var validator = new InlineValidator<Person>();

			validator.RuleSet("foo", () => {
				validator.RuleFor(x => x).Custom((x, ctx) => {
					ctx.AddFailure("x", "y");
				});
			});

			validator.RuleSet("bar", () => {
				validator.RuleFor(x=>x).Custom((x,ctx) => {
					ctx.AddFailure("x", "y");
				});
			});

			var result = validator.Validate(new Person(), v => v.IncludeRuleSets("foo"));
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public async Task New_CustomAsync_within_ruleset() {
			var validator = new InlineValidator<Person>();

			validator.RuleSet("foo", () => {
				validator.RuleFor(x => x).CustomAsync((x, ctx,cancel) => {
					ctx.AddFailure("x", "y");
					return Task.CompletedTask;
				});
			});

			validator.RuleSet("bar", () => {
				validator.RuleFor(x => x).CustomAsync((x, ctx,cancel) => {
					ctx.AddFailure("x", "y");
					return Task.CompletedTask;
				});
			});

			var result = await validator.ValidateAsync(new Person(), v => v.IncludeRuleSets("foo"));
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void New_Custom_When_property_name_omitted_infers_property_name() {
			validator.RuleFor(x => x.Surname)
				.Custom((x, context) => {
					context.AddFailure("Error");
				});

			var result = validator.Validate(new Person());
			result.Errors.Single().PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public void New_Custom_When_property_name_omitted_infers_property_name_nested() {
			var addressValidator = new InlineValidator<Address>();
			addressValidator.RuleFor(x => x.Line1).Custom((x, ctx) => {
				ctx.AddFailure("Error");
			});

			validator.RuleFor(x => x.Address)
				.SetValidator(addressValidator);

			var result = validator.Validate(new Person { Address = new Address() });
			result.Errors.Single().PropertyName.ShouldEqual("Address.Line1");
		}

		[Fact]
		public void New_custom_uses_empty_property_name_for_model_level_rule() {
			validator.RuleFor(x => x).Custom((x, ctx) => ctx.AddFailure("Foo"));
			var result = validator.Validate(new Person());
			result.Errors.Single().PropertyName.ShouldEqual(string.Empty);
		}

		[Fact]
		public void Throws_when_async_rule_invoked_synchronously() {
			validator.RuleFor(x => x.Forename).CustomAsync((x, context, cancel) => {
				context.AddFailure("foo");
				return Task.CompletedTask;
			});
			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() =>
				validator.Validate(new Person()));
		}

		[Fact]
		public async void Runs_sync_rule_asynchronously_when_validator_invoked_asynchronously() {
			validator.RuleFor(x => x.Forename).Custom((x, context) => context.AddFailure("foo"));
			var result = await validator.ValidateAsync(new Person());
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void Allows_placeholders() {
			validator.RuleFor(x => x.Forename).Custom((name, context) => {
				context.MessageFormatter.AppendArgument("Foo", "1");
				context.AddFailure("{Foo}");
			});
			var result = validator.Validate(new Person());
			result.Errors.Single().ErrorMessage.ShouldEqual("1");
		}

		[Fact]
		public void Allows_conditions() {
			validator.RuleFor(x => x.Forename).Custom((name, ctx) => {
				ctx.AddFailure("foo");
			}).When(x => x.Age < 18);

			var result = validator.Validate(new Person() {Age = 17});
			result.IsValid.ShouldBeFalse();

			result = validator.Validate(new Person() {Age = 18});
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Allows_conditions_async() {
			validator.RuleFor(x => x.Forename).CustomAsync((name, ctx, ct) => {
				ctx.AddFailure("foo");
				return Task.CompletedTask;
			}).WhenAsync((x, ct) => Task.FromResult(x.Age < 18));

			var result = await validator.ValidateAsync(new Person() {Age = 17});
			result.IsValid.ShouldBeFalse();

			result = await validator.ValidateAsync(new Person() {Age = 18});
			result.IsValid.ShouldBeTrue();
		}

		private class NestedOrderValidator : AbstractValidator<Order> {
			public NestedOrderValidator() {
				RuleFor(x=>x).Custom((x, ctx) => {
					ctx.AddFailure("Amount", "bar");
				});
			}
		}
	}
}
