using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NSAtlasCopcoBreech {
	public partial class MainWindow {

		//static MainWindow() {

		//}

		void generateTestsFrom(Assembly asm) {
			new TestGenerator().generateTestsFor(asm);


		}

	}
	class TestGenerator {
		const BindingFlags bfProps = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
		const BindingFlags bfCreate = BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance;
		const string CLASS_NAME_REGEX = "^Mid[0-9]+$";
		readonly object[] nullArgs = new object[0];

		readonly CodeNamespace _ns;
		readonly CodeCompileUnit _ccu;
		readonly CodeTypeDeclaration _ctd;
		//readonly static CodeTypeDeclaration _ctdCommon;
		readonly CodeMemberMethod _mcommon;
		const string RUN_METHOD_NAME = "runTests";

		//static TestGenerator() {

		//}

		internal TestGenerator() {
			_ccu = new CodeCompileUnit();
			_ccu.Namespaces.Add(_ns = new CodeNamespace("tests"));
			_ns.Types.Add(_ctd = new CodeTypeDeclaration("Tester"));
			_ns.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
			_ctd.Members.Add(_mcommon = new CodeMemberMethod());
			_mcommon.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			_mcommon.Name = RUN_METHOD_NAME;
		}
		internal void generateTestsFor(Assembly asm) {
			object obj;
			//int midLen, midNo, revision;
			//CodeTypeDeclaration ctd = null;
			CodeMemberMethod m, amethod;
			string aname;

			//_mcommon.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			_mcommon.Statements.Clear();
			List<CodeTypeMember> mmethodsNames = new List<CodeTypeMember>();
			foreach (CodeTypeMember ctm in _ctd.Members) {
				if (ctm is CodeMemberMethod) {
					amethod = ctm as CodeMemberMethod;
					if (string.Compare(aname = amethod.Name, RUN_METHOD_NAME, true) != 0)
						mmethodsNames.Add(ctm);
				}
			}
			if (mmethodsNames.Count > 0)
				foreach (CodeTypeMember ctm in mmethodsNames)
					_ctd.Members.Remove(ctm);
			//}
			List<Type> desiredTypes = new List<Type>();
			foreach (Type atype in asm.GetTypes()) {
				if (atype.IsPublic && atype.IsClass &&
					Regex.IsMatch(atype.Name, CLASS_NAME_REGEX)) {
					desiredTypes.Add(atype);
				}
			}
			if (desiredTypes.Count > 0) {
				if (desiredTypes.Count > 1)
					desiredTypes.Sort(byTypeName);
				foreach (Type atype in desiredTypes) {
					obj = atype.InvokeMember(null, bfCreate, null, null, nullArgs);
					generateTestsForType(obj);
				}
			}
			showCompileUnit(_ccu);
		}

		int byTypeName(Type typea, Type typeb) {
			return typea.Name.CompareTo(typeb.Name);
			//return 0;
		}

		void generateTestsForType(object obj) {
			Type atype = obj.GetType();
			int midNo, revision;
			CodeTypeDeclaration ctd;
			CodeMemberMethod mrunmTests;

			_ns.Types.Add(ctd = new CodeTypeDeclaration("Test" + atype.Name));

			ctd.Comments.AddRange(new CodeCommentStatement[] {
				new CodeCommentStatement("<summary>test</summary>"),
				new CodeCommentStatement("<remarks><para>Type="+atype.FullName+"</para></remarks>"),

});
			ctd.Members.Add(mrunmTests = new CodeMemberMethod());
			mrunmTests.Name = "runTests";
			mrunmTests.Attributes = MemberAttributes.Public | MemberAttributes.Final;

			_mcommon.Statements.Add(
				new CodeExpressionStatement(
					new CodeMethodInvokeExpression(
						new CodeObjectCreateExpression(new CodeTypeReference(ctd.Name)),
						mrunmTests.Name)));

			CodeMemberMethod msetProps;
			CodeArgumentReferenceExpression ar;

			ctd.Members.Add(msetProps = new CodeMemberMethod());
			msetProps.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			msetProps.Name = "setAllProperties";

			ar = new CodeArgumentReferenceExpression("mid");
			msetProps.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(atype), ar.ParameterName));
			setPropertiesFor(msetProps, ar, atype);


			var avar = atype.InvokeMember("HeaderData", bfProps, null, obj, nullArgs);

			//midLen = (int)avar.GetType().InvokeMember("Length", bfProps, null, avar, nullArgs);
			midNo = (int)avar.GetType().InvokeMember("Mid", bfProps, null, avar, nullArgs);
			revision = (int)avar.GetType().InvokeMember("Revision", bfProps, null, avar, nullArgs);

			if (revision > 1)
				for (int arev = 1; arev < revision; arev++)
					generateRevisionTestFor(ctd, mrunmTests, atype, arev, midNo, msetProps.Name);

			generateRevisionTestFor(ctd, mrunmTests, atype, revision, midNo, msetProps.Name);
		}

		void setPropertiesFor(CodeMemberMethod m, CodeArgumentReferenceExpression ar, Type atype) {
			foreach (PropertyInfo pi in atype.GetProperties(bfProps | BindingFlags.DeclaredOnly)) {
				if (!pi.CanWrite)
					m.Statements.Add(new CodeCommentStatement("nope"));
				else
					m.Statements.Add(
						new CodeAssignStatement(
							new CodeFieldReferenceExpression(ar, pi.Name),
							makePropertyValue(pi.PropertyType, m)));
				//new CodePrimitiveExpression()));
			}
		}

		CodeExpression makePropertyValue(Type ptype, CodeMemberMethod m) {
			CodeExpression ret = new CodePrimitiveExpression();
			string typename;

			switch (typename = ptype.FullName.ToUpper()) {
				case "SYSTEM.STRING": return new CodePrimitiveExpression("TESTTESTTEST"); 
				case "SYSTEM.INT32": return new CodePrimitiveExpression(-1); 
				case "SYSTEM.INT64": return new CodePrimitiveExpression(-1); 
				case "SYSTEM.DECIMAL": return new CodePrimitiveExpression((decimal) -1); 
				case "SYSTEM.BOOLEAN": return new CodePrimitiveExpression(true); 
				case "SYSTEM.DATETIME":
					return
new CodePropertyReferenceExpression(
	new CodeTypeReferenceExpression(typeof(System.DateTime)),
	"MinValue");
					//new CodeTypeReference(typeof(System.DateTime)), "MinValue");
					//new CodeObjectCreateExpression(typeof(System.DateTime),
					//DateTime.MinValue); 
					
				default:
					if (ptype.IsEnum) {
						//Trace.WriteLine("here");
						//var v = Enum.GetValues(ptype);
						return new CodeFieldReferenceExpression(
							new CodeTypeReferenceExpression(ptype),
							Enum.GetNames(ptype)[0]);
						//return new CodePrimitiveExpression(Enum.Parse(ptype, Enum.GetNames(ptype)[0]));
						//return new CodePrimitiveExpression(null);
					} else
						m.Comments.Add(new CodeCommentStatement("unhandled: " + typename)); break;
			}
			//{ }
			return ret;
		}

		static void showCompileUnit(CodeCompileUnit ccu) {
			//StringBuilder sb = new StringBuilder();
			StringBuilder sb;
			CodeGeneratorOptions opts = new CodeGeneratorOptions {
				BlankLinesBetweenMembers = false,
				//opts.BlankLinesBetweenMembers=
				ElseOnClosing = false,
				VerbatimOrder = false
			};
			CodeDomProvider provider = CodeDomProvider.CreateProvider("c#");
			using (TextWriter tw = new StringWriter(sb = new StringBuilder())) {
				provider.GenerateCodeFromCompileUnit(ccu, tw, opts);
			}
			string currentPath = Directory.GetCurrentDirectory(), newFilePath, filename;
			newFilePath = Path.Combine(currentPath, "..\\..", "Source\\Tests");
			if (!Directory.Exists(newFilePath))
				Directory.CreateDirectory(newFilePath);
			File.WriteAllText(filename = Path.GetFullPath(Path.Combine(newFilePath, "Tester." + provider.FileExtension)), sb.ToString());
			//Utility.logger.log(ColtLogLevel.Debug, sb.ToString());
		}

		void generateRevisionTestFor(CodeTypeDeclaration ctd, CodeMemberMethod mm, Type atype, int revision, int midNo, string setPropMethodName) {
			CodeMemberMethod m;
			CodeVariableReferenceExpression vr = new CodeVariableReferenceExpression("mid");
			CodeVariableReferenceExpression vrPkg = new CodeVariableReferenceExpression("package");
			//int midNo = -1;

			ctd.Members.Add(m = new CodeMemberMethod());
			m.Name = "testRevision" + revision;
			//m.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			m.Attributes = default(MemberAttributes);
			mm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(null, m.Name)));
			m.Statements.AddRange(
				new CodeStatement[] {
					new CodeVariableDeclarationStatement(atype,vr.VariableName,
					new CodeObjectCreateExpression(atype,
						//new CodePrimitiveExpression(midNo),
						new CodePrimitiveExpression(revision))),
					new CodeVariableDeclarationStatement (typeof(string),vrPkg.VariableName),
					new CodeSnippetStatement(),
					new CodeExpressionStatement(
						new CodeMethodInvokeExpression(new CodeThisReferenceExpression(),setPropMethodName,vr)),
					new CodeAssignStatement(vrPkg,new CodeMethodInvokeExpression(vr,"Pack")),
					new CodeExpressionStatement(
						new CodeMethodInvokeExpression(
							new CodeTypeReferenceExpression (typeof(System.Diagnostics.Trace )),
							"WriteLine",
							new CodeBinaryOperatorExpression(
								new CodePrimitiveExpression("package = "),
								 CodeBinaryOperatorType.Add ,
								 new CodeBinaryOperatorExpression(
								 vrPkg,
								  CodeBinaryOperatorType.Add,
								  new CodePrimitiveExpression(".")))))
				});
		}
	}
}