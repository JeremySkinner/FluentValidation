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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Xunit;
	using Results;


	public class SharedConditionTests {
		class SharedConditionValidator : AbstractValidator<Person> {
			public SharedConditionValidator() {
				// Start with a predicate to group rules together.
				//
				// The AbstractValidator appends this predicate
				// to each inner RuleFor so you only need write,
				// maintain, and think about it in one place.
				//
				// You can finish with an Unless clause that will
				// void the validation for the entire set when it's
				// predicate is true.
				//
				When(x => x.Id > 0, () => {
					RuleFor(x => x.Forename).NotEmpty();
					RuleFor(x => x.Surname).NotEmpty().Equal("Smith");
				});
			}
		}

		class SharedAsyncConditionValidator : AbstractValidator<Person> {
			public SharedAsyncConditionValidator() {
				// Start with a predicate to group rules together.
				//
				// The AbstractValidator appends this predicate
				// to each inner RuleFor so you only need write,
				// maintain, and think about it in one place.
				//
				// You can finish with an Unless clause that will
				// void the validation for the entire set when it's
				// predicate is true.
				//
				WhenAsync(async (x,c) => x.Id > 0,
					() => {
						RuleFor(x => x.Forename).NotEmpty();
						RuleFor(x => x.Surname).NotEmpty().Equal("Smith");
					}
				);
			}
		}

		class SharedConditionWithScopedUnlessValidator : AbstractValidator<Person> {
			public SharedConditionWithScopedUnlessValidator() {
				// inner RuleFor() calls can contain their own,
				// locally scoped When and Unless calls that
				// act only on that individual RuleFor() yet the
				// RuleFor() respects the grouped When() and
				// Unless() predicates.
				//
				When(x => x.Id > 0 && x.Age <= 65, () => { RuleFor(x => x.Orders.Count).Equal(0).Unless(x => String.IsNullOrWhiteSpace(x.CreditCard) == false); });
				//.Unless(x => x.Age > 65);
			}
		}

		class SharedAsyncConditionWithScopedUnlessValidator : AbstractValidator<Person> {
			public SharedAsyncConditionWithScopedUnlessValidator() {
				// inner RuleFor() calls can contain their own,
				// locally scoped When and Unless calls that
				// act only on that individual RuleFor() yet the
				// RuleFor() respects the grouped When() and
				// Unless() predicates.
				//
				WhenAsync(async (x,c) => x.Id > 0 && x.Age <= 65,
					() => {
						RuleFor(x => x.Orders.Count).Equal(0).UnlessAsync(async (x,c) => String.IsNullOrWhiteSpace(x.CreditCard) == false);
					}
				);
			}
		}

		class SharedConditionInverseValidator : AbstractValidator<Person> {
			public SharedConditionInverseValidator() {
				Unless(x => x.Id == 0, () => { RuleFor(x => x.Forename).NotNull(); });
			}
		}

		class SharedAsyncConditionInverseValidator : AbstractValidator<Person>
		{
			public SharedAsyncConditionInverseValidator()
			{
				UnlessAsync(async (x,c) => x.Id == 0, () => { RuleFor(x => x.Forename).NotNull(); });
			}
		}

		class BadValidatorDisablesNullCheck : AbstractValidator<string> {
			public BadValidatorDisablesNullCheck() {
				When(x => x != null, () => {
					RuleFor(x => x).Must(x => x != "foo");
				});
			}

			protected override void EnsureInstanceNotNull(object instanceToValidate) {
				//bad.
			}
		}

		class AsyncBadValidatorDisablesNullCheck : AbstractValidator<string> {
			public AsyncBadValidatorDisablesNullCheck() {
				When(x => x != null, () => {
					RuleFor(x => x).Must(x => x != "foo");
				});

				WhenAsync(async (x, ct) => x != null, () => {
					RuleFor(x => x).Must(x => x != "foo");
				});
			}

			protected override void EnsureInstanceNotNull(object instanceToValidate) {
				//bad.
			}
		}


		[Fact]
		public void Shared_When_is_not_applied_to_grouped_rules_when_initial_predicate_is_false() {
			var validator = new SharedConditionValidator();
			var person = new Person(); // fails the shared When predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public async Task Shared_async_When_is_not_applied_to_grouped_rules_when_initial_predicate_is_false() {
			var validator = new SharedAsyncConditionValidator();
			var person = new Person(); // fails the shared When predicate

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Shared_When_is_applied_to_grouped_rules_when_initial_predicate_is_true() {
			var validator = new SharedConditionValidator();
			var person = new Person() {
				Id = 4 // triggers the shared When predicate
			};

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(3);
		}

		[Fact]
		public async Task Shared_async_When_is_applied_to_grouped_rules_when_initial_predicate_is_true() {
			var validator = new SharedAsyncConditionValidator();
			var person = new Person() {
				Id = 4 // triggers the shared When predicate
			};

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(3);
		}

		[Fact]
		public void Shared_When_is_applied_to_groupd_rules_when_initial_predicate_is_true_and_all_individual_rules_are_satisfied() {
			var validator = new SharedConditionValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Forename = "Kevin", // satisfies RuleFor( x => x.Forename ).NotEmpty()
			                          	Surname = "Smith", // satisfies RuleFor( x => x.Surname ).NotEmpty().Equal( "Smith" )
			                          };

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public async Task Shared_async_When_is_applied_to_groupd_rules_when_initial_predicate_is_true_and_all_individual_rules_are_satisfied() {
			var validator = new SharedAsyncConditionValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Forename = "Kevin", // satisfies RuleFor( x => x.Forename ).NotEmpty()
			                          	Surname = "Smith", // satisfies RuleFor( x => x.Surname ).NotEmpty().Equal( "Smith" )
			                          };

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Shared_When_respects_the_smaller_scope_of_an_inner_Unless_when_the_inner_Unless_predicate_is_satisfied() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.CreditCard = "1234123412341234"; // satisfies the inner Unless predicate
			person.Orders.Add(new Order());

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public async Task Shared_async_When_respects_the_smaller_scope_of_an_inner_Unless_when_the_inner_Unless_predicate_is_satisfied() {
			var validator = new SharedAsyncConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.CreditCard = "1234123412341234"; // satisfies the inner Unless predicate
			person.Orders.Add(new Order());

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Shared_When_respects_the_smaller_scope_of_a_inner_Unless_when_the_inner_Unless_predicate_fails() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public async Task Shared_async_When_respects_the_smaller_scope_of_a_inner_Unless_when_the_inner_Unless_predicate_fails() {
			var validator = new SharedAsyncConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void Outer_Unless_clause_will_trump_an_inner_Unless_clause_when_inner_fails_but_the_outer_is_satisfied() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Age = 70 // satisfies the outer Unless predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public async Task Outer_async_Unless_clause_will_trump_an_inner_Unless_clause_when_inner_fails_but_the_outer_is_satisfied() {
			var validator = new SharedAsyncConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Age = 70 // satisfies the outer Unless predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = await validator.ValidateAsync(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Condition_can_be_used_inside_ruleset() {
			var validator = new TestValidator();
			validator.RuleSet("foo", () => { validator.When(x => x.Id > 0, () => { validator.RuleFor(x => x.Forename).NotNull(); }); });
			validator.RuleFor(x => x.Surname).NotNull();

#pragma warning disable 618
			var result = validator.Validate(new Person {Id = 5}, v => v.IncludeRuleSets("foo"));
#pragma warning restore 618
			result.Errors.Count.ShouldEqual(1);
			result.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public async Task Async_condition_can_be_used_inside_ruleset() {
			var validator = new TestValidator();
			validator.RuleSet("foo", () => {
				validator.WhenAsync(async (x,c) => (x.Id > 0), () => {
					validator.RuleFor(x => x.Forename).NotNull();
				});
			});
			validator.RuleFor(x => x.Surname).NotNull();

			var result = await validator.ValidateAsync(new Person {Id = 5}, v => v.IncludeRuleSets("foo"));
			result.Errors.Count.ShouldEqual(1);
			result.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public void RuleSet_can_be_used_inside_condition() {
			var validator = new TestValidator();

			validator.When(x => x.Id > 0, () => { validator.RuleSet("foo", () => { validator.RuleFor(x => x.Forename).NotNull(); }); });

			validator.RuleFor(x => x.Surname).NotNull();

			var result = validator.Validate(new Person {Id = 5}, v => v.IncludeRuleSets("foo"));
			result.Errors.Count.ShouldEqual(1);
			result.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public async Task RuleSet_can_be_used_inside_async_condition() {
			var validator = new TestValidator();

			validator.WhenAsync(async (x,c) => (x.Id > 0), () => { validator.RuleSet("foo", () => { validator.RuleFor(x => x.Forename).NotNull(); }); });

			validator.RuleFor(x => x.Surname).NotNull();

			var result = await validator.ValidateAsync(new Person {Id = 5}, v => v.IncludeRuleSets("foo"));
			result.Errors.Count.ShouldEqual(1);
			result.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public void Rules_invoke_when_inverse_shared_condition_matches() {
			var validator = new SharedConditionInverseValidator();
			var result = validator.Validate(new Person {Id = 1});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Rules_invoke_when_inverse_shared_async_condition_matches() {
			var validator = new SharedAsyncConditionInverseValidator();
			var result = await validator.ValidateAsync(new Person {Id = 1});
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Rules_not_invoked_when_inverse_shared_condition_does_not_match() {
			var validator = new SharedConditionInverseValidator();
			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Rules_not_invoked_when_inverse_shared_async_condition_does_not_match() {
			var validator = new SharedAsyncConditionInverseValidator();
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Does_not_execute_custom_Rule_when_condition_false() {
			var validator = new TestValidator();
			validator.When(x => false, () => {
				validator.RuleFor(x=>x).Custom((x,ctx)=> ctx.AddFailure(new ValidationFailure("foo", "bar")));
			});

			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Does_not_execute_custom_Rule_when_async_condition_false() {
			var validator = new TestValidator();
			validator.WhenAsync(async (x,c) =>(false), () => {
				validator.RuleFor(x=>x).Custom((x,ctx)=> ctx.AddFailure(new ValidationFailure("foo", "bar")));
			});

			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Does_not_execute_customasync_Rule_when_condition_false()
		{
			var validator = new TestValidator();
			validator.When(x => false, () => {

				validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("foo", "bar")));
			});

			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Does_not_execute_customasync_Rule_when_async_condition_false() {
			var validator = new TestValidator();
			validator.WhenAsync(async (x,c) =>(false), () => {

				validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("foo", "bar")));
			});

			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Executes_custom_rule_when_condition_true() {
			var validator = new TestValidator();
			validator.When(x => true, () => {
				validator.RuleFor(x=>x).Custom((x,ctx) => ctx.AddFailure(new ValidationFailure("foo", "bar")));

			});

			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Executes_custom_rule_when_async_condition_true() {
			var validator = new TestValidator();
			validator.WhenAsync(async (x,c) =>(true), () => {
				validator.RuleFor(x=>x).Custom((x,ctx) => ctx.AddFailure(new ValidationFailure("foo", "bar")));

			});

			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Executes_customasync_rule_when_condition_true() {
			var validator = new TestValidator();
			validator.When(x => true, () => validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("foo", "bar"))));

			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public async Task Executes_customasync_rule_when_async_condition_true() {
			var validator = new TestValidator();
			validator.WhenAsync(async (x,c) =>(true), () => validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("foo", "bar"))));

			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Nested_conditions_with_Custom_rule() {
			var validator = new TestValidator();
			validator.When(x => true, () => {
				validator.When(x => false, () => {
					validator.RuleFor(x=>x).Custom((x,ctx) => ctx.AddFailure(new ValidationFailure("Custom", "The validation failed")));

				});
			});
			var result = validator.Validate(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Nested_async_conditions_with_Custom_rule() {
			var validator = new TestValidator();
			validator.When(x => true, () => {
				validator.WhenAsync(async (x,c) =>(false), () => {
					validator.RuleFor(x=>x).Custom((x,ctx) => ctx.AddFailure(new ValidationFailure("Custom", "The validation failed")));
				});
			});
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Nested_conditions_with_CustomAsync_rule() {
			var validator = new TestValidator();
			validator.When(x => true, () => {
				validator.When(x => false, () => {
					validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("Custom", "The validation failed")));
				});
			});
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Nested_async_conditions_with_CustomAsync_rule() {
			var validator = new TestValidator();
			validator.When(x => true, () => {
				validator.WhenAsync(async (x,c) =>(false), () => {
					validator.RuleFor(x=>x).CustomAsync(async (x,ctx,c) => ctx.AddFailure(new ValidationFailure("Custom", "The validation failed")));
				});
			});
			var result = await validator.ValidateAsync(new Person());
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void When_condition_only_executed_once() {
			var validator = new TestValidator();
			int executions = 0;
			validator.When(x => {
				executions++;
				return x.Age > 10;
			}, () => {
				validator.RuleFor(x => x.Surname).NotNull();
				validator.RuleFor(x => x.Forename).NotNull();
			});

			validator.Validate(new Person {Age = 11});
			executions.ShouldEqual(1);
		}

		[Fact]
		public async Task WhenAsync_condition_only_executed_once() {
			var validator = new TestValidator();
			int executions = 0;
			validator.WhenAsync(async (x, ct) => {
				executions++;
				return x.Age > 10;
			}, () => {
				validator.RuleFor(x => x.Surname).NotNull();
				validator.RuleFor(x => x.Forename).NotNull();
			});

			await validator.ValidateAsync(new Person {Age = 11});
			executions.ShouldEqual(1);
		}

		[Fact]
		public void Runs_otherwise_conditions_for_When() {
			var validator = new TestValidator();
			validator.When(x => x.Age > 10, () => {
				validator.RuleFor(x => x.Forename).NotNull();
			}).Otherwise(() => {
				validator.RuleFor(x => x.Surname).NotNull();
			});

			var result1 = validator.Validate(new Person {Age = 11});
			result1.Errors.Single().PropertyName.ShouldEqual("Forename");
			var result2 = validator.Validate(new Person {Age = 9});
			result2.Errors.Single().PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public void Runs_otherwise_conditons_for_Unless() {
			var validator = new TestValidator();
			validator.Unless(x => x.Age > 10, () => {
				validator.RuleFor(x => x.Forename).NotNull();
			}).Otherwise(() => {
				validator.RuleFor(x => x.Surname).NotNull();
			});

			var result1 = validator.Validate(new Person {Age = 11});
			result1.Errors.Single().PropertyName.ShouldEqual("Surname");
			var result2 = validator.Validate(new Person {Age = 9});
			result2.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public async Task Runs_otherwise_conditions_for_WhenAsync() {
			var validator = new TestValidator();
			validator.WhenAsync(async (x, ct) => x.Age > 10, () => {
				validator.RuleFor(x => x.Forename).NotNull();
			}).Otherwise(() => {
				validator.RuleFor(x => x.Surname).NotNull();
			});

			var result1 = await validator.ValidateAsync(new Person {Age = 11});
			result1.Errors.Single().PropertyName.ShouldEqual("Forename");
			var result2 = await validator.ValidateAsync(new Person {Age = 9});
			result2.Errors.Single().PropertyName.ShouldEqual("Surname");
		}

		[Fact]
		public async Task Runs_otherwise_conditions_for_UnlessAsync() {
			var validator = new TestValidator();
			validator.UnlessAsync(async (x, ct) => x.Age > 10, () => {
				validator.RuleFor(x => x.Forename).NotNull();
			}).Otherwise(() => {
				validator.RuleFor(x => x.Surname).NotNull();
			});

			var result1 = await validator.ValidateAsync(new Person {Age = 11});
			result1.Errors.Single().PropertyName.ShouldEqual("Surname");
			var result2 = await validator.ValidateAsync(new Person {Age = 9});
			result2.Errors.Single().PropertyName.ShouldEqual("Forename");
		}

		[Fact]
		public void Nested_when_inside_otherwise() {
			var validator = new InlineValidator<Person>();
			validator.When(x => x.Id == 1, () => {
				validator.RuleFor(x => x.Forename).NotNull();
			}).Otherwise(() => {
				validator.When(x => x.Age > 18, () => {
					validator.RuleFor(x => x.Email).NotNull();
				});
			});

			var result = validator.Validate(new Person() {Id = 1});
			result.Errors.Count.ShouldEqual(1);
			result.Errors[0].PropertyName.ShouldEqual("Forename");

			result = validator.Validate(new Person() {Id = 2, Age = 20});
			result.Errors.Count.ShouldEqual(1);
			result.Errors[0].PropertyName.ShouldEqual("Email");
		}

		[Fact]
		public void When_condition_executed_for_each_instance_of_RuleForEach_condition_should_not_be_cached() {
			var person = new Person {
				Children = new List<Person> {
					new Person { Id = 1},
					new Person { Id = 0}
				}
			};

			var childValidator = new InlineValidator<Person>();
			int executions = 0;

			childValidator.When(a => {
				executions++;
				return a.Id != 0;
			}, () => {
				childValidator.RuleFor(a => a.Id).Equal(1);
			});
			var personValidator = new InlineValidator<Person>();
			personValidator.RuleForEach(p => p.Children).SetValidator(childValidator);

			var validationResult = personValidator.Validate(person);
			validationResult.IsValid.ShouldBeTrue();
			executions.ShouldEqual(2);
		}

		[Fact]
		public async Task When_async_condition_executed_for_each_instance_of_RuleForEach_condition_should_not_be_cached() {
			var person = new Person {
				Children = new List<Person> {
					new Person { Id = 1},
					new Person { Id = 0}
				}
			};

			var childValidator = new InlineValidator<Person>();
			int executions = 0;

			childValidator.WhenAsync(async (a, ct) => {
				executions++;
				return a.Id != 0;
			}, () => {
				childValidator.RuleFor(a => a.Id).Equal(1);
			});
			var personValidator = new InlineValidator<Person>();
			personValidator.RuleForEach(p => p.Children).SetValidator(childValidator);

			var validationResult = await personValidator.ValidateAsync(person);
			validationResult.IsValid.ShouldBeTrue();
			executions.ShouldEqual(2);
		}

		[Fact]
		public void Doesnt_throw_NullReferenceException_when_instance_not_null() {
			var v = new BadValidatorDisablesNullCheck();
			var result = v.Validate((string) null);
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async Task Doesnt_throw_NullReferenceException_when_instance_not_null_async() {
			var v = new AsyncBadValidatorDisablesNullCheck();
			var result = await v.ValidateAsync((string) null);
			result.IsValid.ShouldBeTrue();
		}

	}
}

