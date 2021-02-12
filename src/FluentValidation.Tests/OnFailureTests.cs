namespace FluentValidation.Tests {
	using System.Threading.Tasks;
	using System.Diagnostics;
	using Xunit;
	using static ComplexValidationTester;
	using TestHelper;

	public class OnFailureTests {
		private TestValidator _validator;

		public OnFailureTests() {
			_validator = new TestValidator();
		}

		[Fact]
		public void OnFailure_called_for_each_failed_rule() {
			_validator.CascadeMode = CascadeMode.Continue;

			int invoked = 0;
			_validator.RuleFor(person => person.Surname).NotNull().NotEmpty().OnFailure(person => {
				invoked += 1;
			});

			_validator.RuleFor(person => person.Surname).NotEmpty().OnFailure((person, ctx, value) => {
				Debug.WriteLine(ctx.PropertyName);
				invoked += 1;
			});

			_validator.RuleFor(person => person.Forename).NotEqual("John").OnFailure((person, ctx, value) => {
				invoked += 1;
			});

			_validator.RuleFor(person => person.Age).GreaterThanOrEqualTo(18).OnFailure((person, ctx, value) => {
				invoked += 1;
			});

			_validator.Validate(new Person { Forename = "John", Age = 17 });

			invoked.ShouldEqual(4);
		}

		[Fact]
		public async Task OnFailure_called_for_each_failed_rule_asyncAsync() {
			_validator.CascadeMode = CascadeMode.Continue;

			int invoked = 0;
			_validator.RuleFor(person => person.Surname).NotNull().NotEmpty().OnFailure(person => {
				invoked += 1;
			});

			_validator.RuleFor(person => person.Surname).NotEmpty().OnFailure((person, ctx, value) => {
				Debug.WriteLine(ctx.PropertyName);
				invoked += 1;
			});

			_validator.RuleFor(person => person.Forename).NotEqual("John").OnFailure((person, ctx, value) => {
				invoked += 1;
			});

			_validator.RuleFor(person => person.Age).GreaterThanOrEqualTo(18).OnFailure((person, ctx, value) => {
				invoked += 1;
			});

			await _validator.ValidateAsync(new Person { Forename = "John", Age = 17 });

			invoked.ShouldEqual(4);
		}

		[Fact]
		public void Should_be_able_to_access_error_message_in_OnFailure() {
			_validator.CascadeMode = CascadeMode.Continue;

			string errorMessage = string.Empty;
			_validator.RuleFor(person => person.Surname).NotNull().WithMessage("You must specify Surname!").OnFailure((person,ctx, value, message) => {
				errorMessage = message;
			});


			_validator.Validate(new Person());

			errorMessage.ShouldEqual("You must specify Surname!");
		}

		[Fact]
		public void ShouldHaveChildValidator_works_with_Include() {
			_validator.Include(new InlineValidator<Person>() {
				v => v.RuleFor(x => x.Forename).NotNull(),
			});

			_validator.ShouldHaveChildValidator(x => x, typeof(InlineValidator<Person>));
		}

		[Fact]
		public void WhenWithOnFailure_should_invoke_condition_on_inner_validator() {
			bool shouldNotBeTrue = false;
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.NotEqual("foo")
				.When(x => x.Id > 0)
				.OnFailure(x => shouldNotBeTrue = true);

			var result = validator.Validate(new Person {Id = 0, Surname = "foo"});
			result.Errors.Count.ShouldEqual(0);
			shouldNotBeTrue.ShouldBeFalse();
		}

		[Fact]
		public async Task WhenAsyncWithOnFailure_should_invoke_condition_on_inner_validator() {
			bool shouldNotBeTrue = false;
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.NotEqual("foo")
				.WhenAsync((x, token) => Task.FromResult(x.Id > 0))
				.OnFailure(x => shouldNotBeTrue = true);

			var result = await validator.ValidateAsync(new Person {Id = 0, Surname = "foo"});
			result.Errors.Count.ShouldEqual(0);
			shouldNotBeTrue.ShouldBeFalse();
		}

		[Fact]
		public void WhenAsyncWithOnFailure_should_invoke_condition_on_inner_validator_invoked_synchronously() {
			bool shouldNotBeTrue = false;
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.NotEqual("foo")
				.WhenAsync((x, token) => Task.FromResult(x.Id > 0))
				.OnFailure(x => shouldNotBeTrue = true);

			var result = validator.Validate(new Person {Id = 0, Surname = "foo"});
			result.Errors.Count.ShouldEqual(0);
			shouldNotBeTrue.ShouldBeFalse();
		}

		[Fact]
		public async Task WhenWithOnFailure_should_invoke_condition_on_async_inner_validator() {
			bool shouldNotBeTrue = false;
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.MustAsync((x, ctx) => Task.FromResult(false))
				.When(x => x.Id > 0)
				.OnFailure(x => shouldNotBeTrue = true);

			var result = await validator.ValidateAsync(new Person {Id = 0, Surname = "foo"});
			result.Errors.Count.ShouldEqual(0);
			shouldNotBeTrue.ShouldBeFalse();
		}

		[Fact]
		public async Task WhenAsyncWithOnFailure_should_invoke_condition_on_async_inner_validator() {
			bool shouldNotBeTrue = false;
			var validator = new TestValidator();
			validator.RuleFor(x => x.Surname)
				.MustAsync((x, ctx) => Task.FromResult(false))
				.WhenAsync((x, ctx) => Task.FromResult(x.Id > 0))
				.OnFailure(x => shouldNotBeTrue = true);

			var result = await validator.ValidateAsync(new Person {Id = 0, Surname = "foo"});
			result.Errors.Count.ShouldEqual(0);
			shouldNotBeTrue.ShouldBeFalse();
		}

	}

	public class AddressValidatorWithOnFailure : AbstractValidator<Address> {
		public AddressValidatorWithOnFailure() {
			RuleFor(x => x.Postcode).NotNull().OnFailure((address, ctx, value) => {
				Debug.WriteLine(address.Line1);
			});
			RuleFor(x => x.Country).SetValidator(new CountryValidator());
		}
	}
}
