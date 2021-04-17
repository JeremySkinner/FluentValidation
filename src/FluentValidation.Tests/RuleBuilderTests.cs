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
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Xunit;
	using Validators;

	public class RuleBuilderTests {
		IRuleBuilderInitial<Person, string> builder;
		private InlineValidator<Person> _validator;
		private IValidationRule<Person, string> _rule;


		public  RuleBuilderTests() {
			_validator = new InlineValidator<Person>();
			builder = _validator.RuleFor(x => x.Surname);
			builder.Configure(rule => _rule = rule);
		}

		[Fact]
		public void Should_build_property_name() {
			_rule.PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public void Adding_a_validator_should_store_validator() {
			var validator = new TestPropertyValidator<Person, string>();
			builder.SetValidator(validator);
			_rule.Current.Validator.ShouldBeTheSameAs(validator);
		}

		[Fact]
		public void Should_set_custom_property_name() {
			builder.SetValidator(new TestPropertyValidator<Person, string>()).WithName("Foo");
			Assert.Equal(_rule.GetDisplayName(null), "Foo");
		}

		[Fact]
		public void Should_set_custom_error() {
			builder.SetValidator(new TestPropertyValidator<Person, string>()).WithMessage("Bar");
			var component = (RuleComponent<Person, string>) _rule.Current;
			component.GetErrorMessage(null, default).ShouldEqual("Bar");
		}

		[Fact]
		public void Should_throw_if_validator_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator((PropertyValidator<Person, string>)null));
		}

		[Fact]
		public void Should_throw_if_overriding_validator_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator((IValidator<string>)null));
		}

		[Fact]
		public void Should_throw_if_overriding_validator_provider_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator((Func<Person, IValidator<string>>) null));
		}

		[Fact]
		public void Should_throw_if_message_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).WithMessage((string)null));
		}

		[Fact]
		public void Should_throw_if_property_name_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).WithName((string)null));
		}

		[Fact]
		public void Should_throw_when_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).When((Func<Person, bool>)null));
		}

		[Fact]
		public void Should_throw_when_context_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).When((Func<Person, ValidationContext<Person>, bool>)null));
		}

		[Fact]
		public void Should_throw_when_async_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).WhenAsync((Func<Person, CancellationToken, Task<bool>>) null));
		}

		[Fact]
		public void Should_throw_when_inverse_context_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).Unless((Func<Person, ValidationContext<Person>, bool>)null));
		}

		[Fact]
		public void Should_throw_when_inverse_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).Unless((Func<Person, bool>)null));
		}

		[Fact]
		public void Should_throw_when_async_inverse_predicate_is_null() {
			Assert.Throws<ArgumentNullException>(() => builder.SetValidator(new TestPropertyValidator<Person, string>()).UnlessAsync((Func<Person, CancellationToken, Task<bool>>) null));
		}

		[Fact]
		public void PropertyDescription_should_return_property_name_split() {
			var builder = _validator.RuleFor(x => x.DateOfBirth);
			IValidationRule<Person, DateTime> rule = null;
			builder.Configure(r => rule = r);
			rule.GetDisplayName(null).ShouldEqual("Date Of Birth");
		}

		[Fact]
		public void PropertyDescription_should_return_custom_property_name() {
			var builder = _validator.RuleFor(x => x.DateOfBirth);
			IValidationRule<Person, DateTime> rule = null;
			builder.Configure(r => rule = r);
			builder.NotEqual(default(DateTime)).WithName("Foo");
			rule.GetDisplayName(null).ShouldEqual("Foo");
		}

		[Fact]
		public void Nullable_object_with_condition_should_not_throw() {
			_validator.RuleFor(x => x.NullableInt.Value)
				.GreaterThanOrEqualTo(3).When(x => x.NullableInt != null);
			_validator.Validate(new ValidationContext<Person>(new Person(), new PropertyChain(), new DefaultValidatorSelector()));
		}

		[Fact]
		public async Task Nullable_object_with_async_condition_should_not_throw() {
			_validator.RuleFor(x => x.NullableInt.Value)
				.GreaterThanOrEqualTo(3)
				.WhenAsync((x,c) => Task.FromResult(x.NullableInt != null));

			await _validator.ValidateAsync(new ValidationContext<Person>(new Person(), new PropertyChain(), new DefaultValidatorSelector()));
		}

		[Fact]
		public void Rule_for_a_non_memberexpression_should_not_generate_property_name() {
			var builder = _validator.RuleFor(x => x.CalculateSalary());
			IValidationRule<Person, int> rule = null;
			builder.Configure(r => rule = r);
			rule.GetDisplayName(null).ShouldBeNull();
			rule.PropertyName.ShouldBeNull();
		}

		[Fact]
		public void Property_should_return_property_being_validated() {
			var property = typeof(Person).GetProperty("Surname");
			_rule.Member.ShouldEqual(property);
		}

		[Fact]
		public void Property_should_return_null_when_it_is_not_a_property_being_validated() {
			builder = _validator.RuleFor(x => "Foo");
			IValidationRule<Person, string> rule = null;
			builder.Configure(r => rule = r);
			rule.Member.ShouldBeNull();
		}

		[Fact]
		public void Result_should_use_custom_property_name_when_no_property_name_can_be_determined() {
			_validator.RuleFor(x => x.CalculateSalary())
				.GreaterThan(100).WithName("Foo");
			var context = new ValidationContext<Person>(new Person(), new PropertyChain(), new DefaultValidatorSelector());
			var result = _validator.Validate(context);
			result.Errors.Single().PropertyName.ShouldEqual("Foo");
		}

		[Fact]
		public void Conditional_child_validator_should_register_with_validator_type_not_property() {
			var builder = _validator.RuleFor(x => x.Address);
			builder.SetValidator((Person person) => new NoopAddressValidator());
			IValidationRule<Person, Address> rule = null;
			builder.Configure(r => rule = r);

			rule.Components
				.Select(x => x.Validator)
				.OfType<IChildValidatorAdaptor>().Single().ValidatorType.ShouldEqual(typeof(NoopAddressValidator));
		}

		class NoopAddressValidator : AbstractValidator<Address> {
		}

		class TestPropertyValidator<T,TProperty> : PropertyValidator<T,TProperty> {
			public override string Name => "TestPropertyValidator";

			public override bool IsValid(ValidationContext<T> context, TProperty value) {
				return true;
			}

			protected override string GetDefaultMessageTemplate(string errorCode) {
				return Localized(errorCode, "NotNullValidator");
			}
		}
	}
}
