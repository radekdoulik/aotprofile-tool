using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Mono.Profiler.Aot {
	public abstract class ProfileBase {

		protected enum RecordType {
			NONE = 0,
			IMAGE = 1,
			TYPE = 2,
			GINST = 3,
			METHOD = 4
		}

		protected enum MonoTypeEnum {
			MONO_TYPE_CLASS = 0x12,
		}

		protected const string MAGIC = "AOTPROFILE";
		protected const int MAJOR_VERSION = 1;
		protected const int MINOR_VERSION = 0;
	}
}
