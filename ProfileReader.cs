using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Mono.Profiler.Aot {
	//
	// Read the contents of a .aotprofile created by the AOT profiler
	// See mono/profiler/aot.h for a description of the file format
	//
	public class ProfileReader : ProfileBase {

		byte [] data;
		int pos;

		int ReadByte ()
		{
			int res = data [pos];
			pos++;
			return res;
		}

		unsafe int GetInt32 (byte [] intData, int index)
		{
			if (intData == null)
				throw new ArgumentNullException (nameof (intData));

			if (intData.Length - index < 4)
				throw new ArgumentException (nameof (index));

			if (index < 0)
				throw new ArgumentException (nameof (index));

			int ret;
			byte* b = (byte*)&ret;

			for (int i = 0; i < 4; i++)
				b [i] = intData [index + i];

			return ret;
		}

		int ReadInt ()
		{
			int res = GetInt32 (data, pos);
			pos += 4;
			return res;
		}

		string ReadString ()
		{
			int len = ReadInt ();
			var res = new String (Encoding.UTF8.GetChars (data, pos, len));
			pos += len;
			return res;
		}

		public ProfileData Read (Stream s)
		{
			byte [] buf = new byte [16];
			int len = s.Read (buf, 0, MAGIC.Length);
			if (len != MAGIC.Length)
				throw new IOException ("Input file is too small.");
			var magic = new String (Encoding.UTF8.GetChars (buf, 0, MAGIC.Length));
			if (magic != MAGIC)
				throw new IOException ("Input file is not a AOT profiler output file.");

			// Profile files are not expected to be large, so reading them is ok
			len = (int)s.Length - MAGIC.Length;
			data = new byte [len];
			pos = 0;
			int count = s.Read (data, 0, len);
			if (count != len)
				throw new IOException ("Can't read profile file.");

			int version = ReadInt ();
			int expected_version = (MAJOR_VERSION << 16) | MINOR_VERSION;
			if (version != expected_version)
				throw new IOException (String.Format ("Expected file version 0x{0:x}, got 0x{1:x}.", expected_version, version));

			ProfileData res = new ProfileData ();

			Dictionary<int, ProfileRecord> records = new Dictionary<int, ProfileRecord> ();

			while (true) {
				RecordType rtype = (RecordType)data [pos];
				pos++;
				if (rtype == RecordType.NONE)
					break;
				int id = ReadInt ();
				switch (rtype) {
				case RecordType.IMAGE: {
						string name = ReadString ();
						string mvid = ReadString ();
						var module = new ModuleRecord (id, name, mvid);
						records [id] = module;
						res.Modules.Add (module);
						break;
					}
				case RecordType.GINST: {
						int argc = ReadInt ();

						TypeRecord [] types = new TypeRecord [argc];
						for (int i = 0; i < argc; ++i) {
							int type_id = ReadInt ();
							types [i] = (TypeRecord)records [type_id];
						}
						var ginst = new GenericInstRecord (id, types);
						records [id] = ginst;
						break;
					}
				case RecordType.TYPE: {
						MonoTypeEnum ttype = (MonoTypeEnum)ReadByte ();

						switch (ttype) {
						case MonoTypeEnum.MONO_TYPE_CLASS: {
								int image_id = ReadInt ();
								int ginst_id = ReadInt ();
								string name = ReadString ();

								GenericInstRecord inst = null;
								if (ginst_id != -1)
									inst = (GenericInstRecord)records [ginst_id];

								var module = (ModuleRecord)records [image_id];
								var type = new TypeRecord (id, module, name, inst);
								res.Types.Add (type);
								records [id] = type;
								break;
							}
						default:
							throw new NotImplementedException ();
						}
						break;
					}
				case RecordType.METHOD: {
						int class_id = ReadInt ();
						int ginst_id = ReadInt ();
						int param_count = ReadInt ();
						string name = ReadString ();
						string sig = ReadString ();

						var type = (TypeRecord)records [class_id];
						GenericInstRecord ginst = ginst_id != -1 ? (GenericInstRecord)records [ginst_id] : null;
						var method = new MethodRecord (id, type, ginst, name, sig, param_count);
						res.Methods.Add (method);
						records [id] = method;
						break;
					}
				default:
					throw new NotImplementedException (rtype.ToString ());
				}
			}

			data = null;

			return res;
		}
	}
}
