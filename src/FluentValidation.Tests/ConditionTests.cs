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
#pragma warning disable 1998

namespace FluentValidation.Tests {
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Xunit;


	public class ConditionTests {
		[Fact]
		public void Validation_should_succeed_when_condition_does_not_match() {
			var validator = new TestConditionValidator();
			var result = validator.Validate(new Person {Id = 1});
			Assert.True(result.IsValid);
		}

		[Fact]
		public async Task Validation_should_succeed_when_async_condition_does_not_match() {
			var validator = new TestConditionAsyncValidator();
			var result = await validator.ValidateAsync(new Person {Id = 1});
            result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Validation_should_fail_when_condition_matches() {
			var validator = new TestConditionValidator();
			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Validation_should_fail_when_async_condition_matches() {
			var validator = new TestConditionAsyncValidator();
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Validation_should_succeed_when_condition_matches() {
			var validator = new InverseConditionValidator();
			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Validation_should_succeed_when_async_condition_matches() {
			var validator = new InverseConditionAsyncValidator();
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Validation_should_fail_when_condition_does_not_match() {
			var validator = new InverseConditionValidator();
			var result = validator.Validate(new Person {Id = 1});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Validation_should_fail_when_async_condition_does_not_match() {
			var validator = new InverseConditionAsyncValidator();
			var result = await validator.ValidateAsync(new Person {Id = 1});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Condition_is_applied_to_all_validators_in_the_chain() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull().NotEqual("foo").When(x => x.Id > 0)
			};

			var result = validator.Validate(new Person());
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public async Task Async_condition_is_applied_to_all_validators_in_the_chain() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull().NotEqual("foo").WhenAsync(async (x,c) => x.Id > 0)
			};

			var result = await validator.ValidateAsync(new Person());
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Async_condition_is_applied_to_all_validators_in_the_chain_when_executed_synchronously() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull().NotEqual("foo").WhenAsync(async (x,c) => x.Id > 0)
			};

			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() =>
				validator.Validate(new Person()));
		}

		[Fact]
		public async Task Sync_condition_is_applied_to_async_validators() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname)
					.MustAsync(async (val, token) => val != null)
					.MustAsync(async (val, token) => val != "foo")
					.When(x => x.Id > 0)
			};

			var result = await validator.ValidateAsync(new Person());
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Condition_is_applied_to_single_validator_in_the_chain_when_ApplyConditionTo_set_to_CurrentValidator() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull().NotEqual("foo").When(x => x.Id > 0, ApplyConditionTo.CurrentValidator)
			};

			var result = validator.Validate(new Person());
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public async Task Async_condition_is_applied_to_single_validator_in_the_chain_when_ApplyConditionTo_set_to_CurrentValidator() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull().NotEqual("foo").WhenAsync(async (x,c) => x.Id > 0, ApplyConditionTo.CurrentValidator)
			};

			var result = await validator.ValidateAsync(new Person());
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void Async_condition_throws_when_executed_synchronosuly_with_synchronous_role() {
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname).NotNull()
				.WhenAsync((x, token) => Task.FromResult(false));

			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() =>
				validator.Validate(new Person()));
		}

		[Fact]
		public void Async_condition_throws_when_executed_synchronosuly_with_asynchronous_rule() {
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.MustAsync((surname, c) => Task.FromResult(surname != null))
				.WhenAsync((x, token) => Task.FromResult(false));

			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() => validator.Validate(new Person()));
		}

		[Fact]
		public void Async_condition_throws_when_executed_synchronosuly_with_synchronous_collection_role() {
			var validator = new TestValidator();
			validator.RuleForEach(x => x.NickNames).NotNull()
				.WhenAsync((x, token) => Task.FromResult(false));
			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() =>
				validator.Validate(new Person {NickNames = new string[0]}));
		}

		[Fact]
		public void Async_condition_throws_when_invoked_synchronosuly_with_asynchronous_collection_rule() {
			var validator = new TestValidator();
			validator.RuleForEach(x => x.NickNames)
				.MustAsync((n, c) => Task.FromResult(n != null))
				.WhenAsync((x, token) => Task.FromResult(false));

			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() => validator.Validate(new Person {NickNames = new string[0]}));
		}

		[Fact]
		public void Can_access_property_value_in_custom_condition() {
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname).Must(v => false).Configure(cfg => {
				cfg.ApplyCondition(context => cfg.GetPropertyValue(context.InstanceToValidate) != null);
			});

			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();

			result = validator.Validate(new Person {Surname = "foo"});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Can_access_property_value_in_custom_condition_foreach() {
			var validator = new TestValidator();
			validator.RuleForEach(x => x.Orders).Must(v => false).Configure(cfg => {
				cfg.ApplyCondition(context => cfg.GetPropertyValue(context.InstanceToValidate) != null);
			});

			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();

			result = validator.Validate(new Person { Orders = new List<Order> { new Order() }});
			result.IsValid.ShouldBeFalse();
		}

		private class TestConditionValidator : AbstractValidator<Person> {
			public TestConditionValidator() {
				RuleFor(x => x.Forename).NotNull().When(x => x.Id == 0);
			}
		}

		class TestConditionAsyncValidator : AbstractValidator<Person> {
			public TestConditionAsyncValidator() {
				RuleFor(x => x.Forename).NotNull().WhenAsync(async (x,c) => x.Id == 0);
			}
		}

		private class InverseConditionValidator : AbstractValidator<Person> {
			public InverseConditionValidator() {
				RuleFor(x => x.Forename).NotNull().Unless(x => x.Id == 0);
			}
		}

		class InverseConditionAsyncValidator : AbstractValidator<Person> {
			public InverseConditionAsyncValidator() {
				RuleFor(x => x.Forename).NotNull().UnlessAsync(async (x,c) => x.Id == 0);
			}
		}
	}

}
