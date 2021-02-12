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
	namespace Features {
		public interface ISupportsCustomMessage { }
		public interface ISupportsCustomErrorCode { }
		public interface ISupportsConditions { }
		public interface ISupportsAsyncConditions { }
		public interface ISupportsDisplayNameOverride { }
		public interface ISupportsPropertyNameOverride { }
		public interface ISupportsCustomState { }
		public interface ISupportsSeverity { }
		public interface ISupportsOnFailure { }

		public interface ISupportsOnAnyFailure { }

		public interface IStandardFeatures
			: ISupportsCustomMessage, ISupportsCustomErrorCode, ISupportsConditions,
				ISupportsDisplayNameOverride, ISupportsPropertyNameOverride,
				ISupportsCustomState, ISupportsSeverity, ISupportsOnFailure, ISupportsOnAnyFailure
		{ }

		public interface IStandardAsyncFeatures
			: ISupportsCustomMessage, ISupportsCustomErrorCode, ISupportsConditions,
				ISupportsAsyncConditions, ISupportsDisplayNameOverride, ISupportsPropertyNameOverride,
				ISupportsCustomState, ISupportsSeverity, ISupportsOnFailure, ISupportsOnAnyFailure
		{ }
	}
}
