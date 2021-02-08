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

namespace FluentValidation.Validators {
	using System;

	public class InclusiveBetweenValidator<T, TProperty> : RangeValidator<T, TProperty>, IInclusiveBetweenValidator where TProperty : IComparable, IComparable<TProperty> {

		public override string Name => "InclusiveBetweenValidator";

		public InclusiveBetweenValidator(TProperty from, TProperty to) : base(from, to) {
		}

		public override bool IsValid(ValidationContext<T> context, TProperty value) {
			// If the value is null then we abort and assume success.
			// This should not be a failure condition - only a NotNull/NotEmpty should cause a null to fail.
			if (value == null) return true;

			if (Compare(value, From) < 0 || Compare(value, To) > 0) {

				context.MessageFormatter
					.AppendArgument("From", From)
					.AppendArgument("To", To)
					.AppendArgument("Value", value);

				return false;
			}
			return true;
		}
	}

	public interface IBetweenValidator : IPropertyValidator {
		object From { get; }
		object To { get; }
	}

	public interface IInclusiveBetweenValidator : IBetweenValidator { }
}
