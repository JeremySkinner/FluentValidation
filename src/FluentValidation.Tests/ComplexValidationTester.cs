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
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Results;
	using Xunit;

	public class ComplexValidationTester {
		PersonValidator validator;
		Person person;

		public ComplexValidationTester() {
			validator = new PersonValidator();
			person = new Person {
				Address = new Address {
					Country = new Country()
				},
				Orders = new List<Order> {
					new Order() {Amount = 5},
					new Order() {ProductName = "Foo"}
				}
			};
		}

		[Fact]
		public void Validates_complex_property() {
			var results = validator.Validate(person);

			results.Errors.Count.ShouldEqual(3);
			results.Errors[0].PropertyName.ShouldEqual("Forename");
			results.Errors[1].PropertyName.ShouldEqual("Address.Postcode");
			results.Errors[2].PropertyName.ShouldEqual("Address.Country.Name");
		}

		[Fact]
		public void Should_override_propertyName() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Address).SetValidator(new AddressValidator())
					.OverridePropertyName("Address2")
			};

			var results = validator.Validate(person);
			results.Errors[0].PropertyName.ShouldEqual("Address2.Postcode");
		}


		[Fact]
		public void Complex_validator_should_not_be_invoked_on_null_property() {
			var results = validator.Validate(new Person());
			results.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void Should_allow_normal_rules_and_complex_property_on_same_property() {
			validator.RuleFor(x => x.Address.Line1).NotNull();
			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(4);
		}

		[Fact]
		public void Explicitly_included_properties_should_be_propagated_to_nested_validators() {
			var results = validator.Validate(person, v => v.IncludeProperties(x => x.Address));
			results.Errors.Count.ShouldEqual(2);
			results.Errors.First().PropertyName.ShouldEqual("Address.Postcode");
			results.Errors.Last().PropertyName.ShouldEqual("Address.Country.Name");
		}

		[Fact]
		public void Explicitly_included_properties_should_be_propagated_to_nested_validators_using_strings() {
			var results = validator.Validate(person, v => v.IncludeProperties("Address"));
			results.Errors.Count.ShouldEqual(2);
			results.Errors.First().PropertyName.ShouldEqual("Address.Postcode");
			results.Errors.Last().PropertyName.ShouldEqual("Address.Country.Name");
		}


		[Fact]
		public void Complex_property_should_be_excluded() {
			var results = validator.Validate(person, v => v.IncludeProperties(x => x.Surname));
			results.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Condition_should_work_with_complex_property() {
			var validator = new TestValidator() {
				v => v.RuleFor(x => x.Address).SetValidator(new AddressValidator()).When(x => x.Address.Line1 == "foo")
			};

			var result = validator.Validate(person);
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Condition_should_work_with_complex_property_when_invoked_async() {
			var validator = new TestValidator() {
				v => v.RuleFor(x => x.Address).SetValidator(new AddressValidator()).When(x => x.Address.Line1 == "foo")
			};

			var result = await validator.ValidateAsync(person);
			result.IsValid.ShouldBeTrue();
		}


		[Fact]
		public async Task Async_condition_should_work_with_complex_property() {
			var validator = new TestValidator() {
				v => v.RuleFor(x => x.Address).SetValidator(new AddressValidator()).WhenAsync(async (x, c) => x.Address.Line1 == "foo")
			};

			var result = await validator.ValidateAsync(person);
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Async_condition_throws_when_validator_invoked_synchronously() {
			var validator = new TestValidator() {
				v => v.RuleFor(x => x.Address).SetValidator(new AddressValidator()).WhenAsync(async (x, c) => x.Address.Line1 == "foo")
			};

			Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() => validator.Validate(person));
		}

		[Fact]
		public void Can_validate_using_validator_for_base_type() {
			var addressValidator = new InlineValidator<IAddress>() {
				v => v.RuleFor(x => x.Line1).NotNull()
			};

			var validator = new TestValidator {
				v => v.RuleFor(x => x.Address).SetValidator(addressValidator)
			};

			var result = validator.Validate(new Person {Address = new Address()});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Can_directly_validate_multiple_fields_of_same_type() {
			var sut = new TestObjectValidator();
			var testObject = new TestObject {
				Foo2 = new TestDetailObject() {Surname = "Bar"}
			};

			//Should not throw
			sut.Validate(testObject);
		}

		[Fact]
		public void Validates_child_validator_synchronously() {
			var validator = new TracksAsyncCallValidator<Person>();
			var addressValidator = new TracksAsyncCallValidator<Address>();
			addressValidator.RuleFor(x => x.Line1).NotNull();
			validator.RuleFor(x => x.Address).SetValidator(addressValidator);

			validator.Validate(new Person() {Address = new Address()});
			addressValidator.WasCalledAsync.ShouldEqual(false);
		}

		[Fact]
		public void Validates_child_validator_asynchronously() {
			var validator = new TracksAsyncCallValidator<Person>();
			var addressValidator = new TracksAsyncCallValidator<Address>();
			addressValidator.RuleFor(x => x.Line1).NotNull();
			validator.RuleFor(x => x.Address).SetValidator(addressValidator);

			validator.ValidateAsync(new Person() {Address = new Address()}).GetAwaiter().GetResult();
			addressValidator.WasCalledAsync.ShouldEqual(true);
		}

		[Fact]
		public void Multiple_rules_in_chain_with_childvalidator_shouldnt_reuse_accessor() {
			var validator = new InlineValidator<Person>();
			var addrValidator = new InlineValidator<Address>();
			addrValidator.RuleFor(x => x.Line1).NotNull();

			validator.RuleFor(x => x.Address).SetValidator(addrValidator)
				.Must(a => a != null);

			var result = validator.Validate(new Person() {Address = new Address()});
			result.Errors.Count.ShouldEqual(1);
		}


		[Fact]
		public async Task Multiple_rules_in_chain_with_childvalidator_shouldnt_reuse_accessor_async() {
			var validator = new InlineValidator<Person>();
			var addrValidator = new InlineValidator<Address>();
			addrValidator.RuleFor(x => x.Line1).MustAsync((l, t) => Task.FromResult(l != null));

			validator.RuleFor(x => x.Address).SetValidator(addrValidator)
				.MustAsync((a, t) => Task.FromResult(a != null));

			var result = await validator.ValidateAsync(new Person() {Address = new Address()});
			result.Errors.Count.ShouldEqual(1);
		}

		public class TestObject {
			public TestDetailObject Foo1 { get; set; }
			public TestDetailObject Foo2 { get; set; }
		}

		public class TestDetailObject {
			public string Surname { get; set; }
		}

		public class TestObjectValidator : AbstractValidator<TestObject> {
			public TestObjectValidator() {
				RuleFor(x => x.Foo1.Surname).NotEmpty().When(x => x.Foo1 != null);
				RuleFor(x => x.Foo2.Surname).NotEmpty();
			}
		}

		//[Fact]
		//public void Should_not_infinite_loop() {
		//	var val = new InfiniteLoopValidator();
		//	var target = new InfiniteLoop();
		//	target.Property = new InfiniteLoop2 {Property = target};
		//	val.Validate(target);
		//}

		private static string PointlessMethod() {
			return null;
		}

		public class PersonValidator : InlineValidator<Person> {
			public PersonValidator() {
				RuleFor(x => x.Forename).NotNull();
				RuleFor(x => x.Address).SetValidator(new AddressValidator());
			}
		}

		public class AddressValidator : AbstractValidator<Address> {
			public AddressValidator() {
				RuleFor(x => x.Postcode).NotNull();
				RuleFor(x => x.Country).SetValidator(new CountryValidator());
			}
		}

		public class CountryValidator : AbstractValidator<Country> {
			public CountryValidator() {
				RuleFor(x => x.Name).NotNull();
			}
		}

		public class PointlessStringValidator : AbstractValidator<string> {
		}

		public class InfiniteLoop {
			public InfiniteLoop2 Property { get; set; }
		}

		public class InfiniteLoop2 {
			public InfiniteLoop Property { get; set; }
		}

		public class InfiniteLoopValidator : AbstractValidator<InfiniteLoop> {
			public InfiniteLoopValidator() {
				RuleFor(x => x.Property).SetValidator(new InfiniteLoop2Validator());
			}
		}

		public class InfiniteLoop2Validator : AbstractValidator<InfiniteLoop2> {
			public InfiniteLoop2Validator() {
				RuleFor(x => x.Property).SetValidator(new InfiniteLoopValidator());
			}
		}

		public class TracksAsyncCallValidator<T> : InlineValidator<T> {
			public bool? WasCalledAsync;

			public override ValidationResult Validate(ValidationContext<T> context) {
				WasCalledAsync = false;
				return base.Validate(context);
			}

			public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = new CancellationToken()) {
				WasCalledAsync = true;
				return base.ValidateAsync(context, cancellation);
			}
		}
	}
}
