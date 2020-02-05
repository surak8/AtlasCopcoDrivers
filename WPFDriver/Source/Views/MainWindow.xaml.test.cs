using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NSAtlasCopcoBreech {
	public partial class MainWindow {
		const BindingFlags bfProps =BindingFlags.Public|BindingFlags.Instance|BindingFlags.GetProperty;
		const BindingFlags bfCreate =BindingFlags.Public|BindingFlags.CreateInstance|BindingFlags.Instance;
		string CLASS_NAME_REGEX = "^Mid[0-9]+$";
		readonly object[] nullArgs = new object[0];

		readonly static CodeNamespace _ns;
		readonly static CodeCompileUnit _ccu;
		readonly static CodeTypeDeclaration _ctd;

		static MainWindow() {
			_ccu=new CodeCompileUnit();
			_ccu.Namespaces.Add(_ns=new CodeNamespace("tests"));
			_ns.Types.Add(_ctd=new CodeTypeDeclaration("Tester"));
		}

		void generateTestsFrom(Assembly asm) {
			object obj;
			int midLen,midNo,revision;
			CodeTypeDeclaration ctd=null;
			CodeMemberMethod m;

			foreach (Type atype in asm.GetTypes()) {
				if (atype.IsPublic && atype.IsClass&&
					Regex.IsMatch(atype.Name, CLASS_NAME_REGEX)) {
					obj=atype.InvokeMember(null, bfCreate, null, null, nullArgs);
					generateTestsForType(obj);
				}
			}
		}

		void generateTestsForType(object obj) {
			Type atype=obj.GetType();
			int midLen,midNo,revision;
			CodeTypeDeclaration ctd;

			_ns.Types.Add(ctd=new CodeTypeDeclaration("Test"+atype.Name));

			var avar=atype.InvokeMember("HeaderData",bfProps,null,obj,nullArgs);

			midLen=(int) avar.GetType().InvokeMember("Length", bfProps, null, avar, nullArgs);
			midNo=(int) avar.GetType().InvokeMember("Mid", bfProps, null, avar, nullArgs);
			revision=(int) avar.GetType().InvokeMember("Revision", bfProps, null, avar, nullArgs);
			if (revision>1) 
				for (int arev = 1; arev<revision; arev++)
					generateRevisionTestFor(ctd, atype, arev);
			
			generateRevisionTestFor(ctd, atype, revision);
			showCompileUnit(_ccu);
		}

		static void showCompileUnit(CodeCompileUnit ccu) {
			StringBuilder sb=new StringBuilder();
			CodeGeneratorOptions opts=new CodeGeneratorOptions();
			CodeDomProvider provider=CodeDomProvider.CreateProvider("c#");
			using (TextWriter tw = new StringWriter(sb=new StringBuilder())) {
				provider.GenerateCodeFromCompileUnit(ccu, tw, opts);
			}
			Utility.logger.log(ColtLogLevel.Debug, sb.ToString());
		}

		void generateRevisionTestFor(CodeTypeDeclaration ctd, Type atype, int revision) {
			CodeMemberMethod m;

			ctd.Members.Add(m=new CodeMemberMethod());
			m.Name="testRevision"+revision;
		}
	}
}