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
		void generateTestsFrom(Assembly asm) {
#if true
			new tests.Tester().runTests();
#else
			new TestGenerator().generateTestsFor(asm);
#endif
		}
	}
	class TestGenerator {
		const BindingFlags bfProps = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty;
		const BindingFlags bfCreate = BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance;
#if OTHER_VERSION
		const string CLASS_NAME_REGEX = "^Mid[0-9]+$";
#else
		const string CLASS_NAME_REGEX = "^MID_[0-9]+$";
#endif
		readonly object[] nullArgs = new object[0];
		readonly CodeNamespace _ns;
		readonly CodeCompileUnit _ccu;
		readonly CodeTypeDeclaration _ctd;
		readonly CodeMemberMethod _mcommon;
		const string RUN_METHOD_NAME = "runTests";
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
			CodeMemberMethod amethod;
			string aname;
			CodeDomProvider cdp = CodeDomProvider.CreateProvider("c#");
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
					generateTestsForType(obj, cdp);
				}
			}
			showCompileUnit(_ccu, cdp);
		}
		int byTypeName(Type typea, Type typeb) {
			return typea.Name.CompareTo(typeb.Name);
		}
		void generateTestsForType(object midInstance, CodeDomProvider cdp) {
			generateTestsForType1(midInstance, cdp);
		}
		void generateTestsForType1(object midInstance, CodeDomProvider cdp) {
			CodeTypeDeclaration midClassToTest;
			CodeMemberMethod testRunnerMethod;
			Type midType = midInstance.GetType();
			_ns.Types.Add(midClassToTest = new CodeTypeDeclaration("Test" + midType.Name));
			midClassToTest.Comments.AddRange(new CodeCommentStatement[] {
				new CodeCommentStatement("<summary>test</summary>"),
				new CodeCommentStatement("<remarks><para>Type="+midType.FullName+"</para></remarks>"),
});
			midClassToTest.Members.Add(testRunnerMethod = new CodeMemberMethod());
			testRunnerMethod.Name = "runTests";
			testRunnerMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			_mcommon.Statements.Add(
				new CodeExpressionStatement(
					new CodeMethodInvokeExpression(
						new CodeObjectCreateExpression(new CodeTypeReference(midClassToTest.Name)),
						testRunnerMethod.Name)));
			setupTestMethod(midInstance, cdp, midClassToTest, testRunnerMethod);
		}
		void setupTestMethod(object midInstance, CodeDomProvider cdp, CodeTypeDeclaration ctd, CodeMemberMethod mrunmTests, int revision = -1) {
			CodeMemberMethod msetProps;
			CodeArgumentReferenceExpression ar;
			Type midType = midInstance.GetType();
			ctd.Members.Add(msetProps = new CodeMemberMethod());
			msetProps.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			msetProps.Name = "setAllProperties";
			ar = new CodeArgumentReferenceExpression("mid");
			msetProps.Parameters.Add(
				new CodeParameterDeclarationExpression(
					new CodeTypeReference(midType), ar.ParameterName));
			setPropertiesFor(msetProps, ar, midType, revision);
			generateRevisionTests(midInstance, cdp, ctd, mrunmTests, msetProps);
		}
		CodeTypeReference createType(Type midType) {
			CodeTypeReference ret = new CodeTypeReference(midType);
			Trace.WriteLine("here");
			if (midType.IsGenericType)
				Trace.WriteLine("here-2");
			return ret;
		}
		void generateRevisionTests(object midInstance, CodeDomProvider cdp, CodeTypeDeclaration ctd, CodeMemberMethod mrunmTests, CodeMemberMethod msetProps) {
			int revision;
			Type midType = midInstance.GetType();
			var avar = midType.InvokeMember("HeaderData", bfProps, null, midInstance, nullArgs);
			revision = (int) avar.GetType().InvokeMember("Revision", bfProps, null, avar, nullArgs);
			if (revision > 1)
				for (int arev = 1; arev < revision; arev++)
					generateRevisionTestFor(ctd, mrunmTests, midType, arev, msetProps.Name, cdp, revision > 1);
			generateRevisionTestFor(ctd, mrunmTests, midType, revision, msetProps.Name, cdp, revision > 1);
		}
		void setPropertiesFor(CodeMemberMethod m, CodeArgumentReferenceExpression ar, Type atype, int revision) {
			foreach (PropertyInfo pi in atype.GetProperties(bfProps | BindingFlags.DeclaredOnly)) {
				if (pi.SetMethod != null && pi.SetMethod.IsPrivate) {
					m.Statements.AddRange(new CodeStatement[] {
						new CodeSnippetStatement("/*"),
						new CodeAssignStatement(
							new CodeFieldReferenceExpression(ar, pi.Name),
							createParameter(pi,revision,m)
							//makePropertyValue(pi.PropertyType, m)
							
							),
						new CodeSnippetStatement("*/")
					});
				} else {
					m.Statements.Add(
						new CodeAssignStatement(
							new CodeFieldReferenceExpression(ar, pi.Name),
							createParameter(pi, revision, m)
							));
				}
			}
		}


		[Obsolete("replace with createParameter", true)]
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
					return new CodePropertyReferenceExpression(
						new CodeTypeReferenceExpression(typeof(System.DateTime)),
						"MinValue");
				default:
					if (ptype.IsEnum) {
						return new CodeFieldReferenceExpression(
							new CodeTypeReferenceExpression(ptype),
							Enum.GetNames(ptype)[0]);
					} else {
						Trace.WriteLine("ack: what's '" + ptype.FullName + "'?");
						m.Comments.Add(new CodeCommentStatement("unhandled: " + typename));
						return new
							CodeObjectCreateExpression(
							ptype);
					}
			}
		}
		static void showCompileUnit(CodeCompileUnit ccu, CodeDomProvider provider) {
			StringBuilder sb;

			using (TextWriter tw = new StringWriter(sb = new StringBuilder())) {
				provider.GenerateCodeFromCompileUnit(ccu, tw, options);
			}
			string currentPath = Directory.GetCurrentDirectory(), newFilePath, filename;
			newFilePath = Path.Combine(currentPath, "..\\..", "Source\\Tests");
			if (!Directory.Exists(newFilePath))
				Directory.CreateDirectory(newFilePath);
			File.WriteAllText(filename = Path.GetFullPath(Path.Combine(newFilePath, "Tester." + provider.FileExtension)), sb.ToString());
		}
		void generateRevisionTestFor(CodeTypeDeclaration ctd, CodeMemberMethod mm, Type atype, int revision, string setPropMethodName, CodeDomProvider cdp, bool v) {
			CodeMemberMethod m;
			CodeVariableReferenceExpression vr = new CodeVariableReferenceExpression("mid");
			CodeVariableReferenceExpression vrPkg = new CodeVariableReferenceExpression("package");
			ctd.Members.Add(m = new CodeMemberMethod());
			m.Name = "testRevision" + revision;
			m.Attributes = default(MemberAttributes);
			mm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(null, m.Name)));
			if (v)
				m.Statements.Add(new CodeCommentStatement("HAVE multiple revisions"));
			addVarDefs(atype, m, vr, vrPkg);
			generateTestMethod(mm, atype, revision, setPropMethodName, cdp, m, vr, vrPkg);
		}

		void showMethod(CodeMemberMethod m, CodeDomProvider cdp, string msg = null) {
			StringBuilder sb;

			using (StringWriter sw = new StringWriter(sb = new StringBuilder())) {
				cdp.GenerateCodeFromMember(m, sw, options);
			};
			Trace.WriteLine((string.IsNullOrEmpty(msg) ? string.Empty : (msg + " : ")) + sb.ToString());
		}

		void generateTestMethod(CodeMemberMethod mm, Type atype, int revision, string setPropMethodName, CodeDomProvider cdp, CodeMemberMethod m, CodeVariableReferenceExpression vr, CodeVariableReferenceExpression vrPkg) {

			CodeStatementCollection cscResult;

#if true
			cscResult = generateCTORS(atype, vr, vrPkg, setPropMethodName, revision, m, cdp);
#else
			cscResult = generateBestCTOR(mm, atype, revision, cdp, m, vr, csc);
#endif
			m.Statements.AddRange(cscResult);
		}

		static CodeGeneratorOptions _opts;
		static CodeGeneratorOptions options {
			get {
				if (_opts == null)
					_opts = new CodeGeneratorOptions {
						BlankLinesBetweenMembers = false,
						BracingStyle = "BLOCK",
						ElseOnClosing = true,
						IndentString = new string(' ', 4),
						VerbatimOrder = false
					};
				return _opts;
			}
		}

		void showStatements(CodeDomProvider cdp, CodeStatementCollection csc) {
			StringBuilder sb;

			if (cdp == null)
				return;
			if (csc == null)
				return;

			using (StringWriter sw = new StringWriter(sb = new StringBuilder())) {
				foreach (CodeStatement cs in csc)
					cdp.GenerateCodeFromStatement(cs, sw, options);
			}
			Trace.WriteLine(sb.ToString());
		}

		CodeStatementCollection generateCTORS(Type midType, CodeVariableReferenceExpression vr, CodeVariableReferenceExpression vrPkg, string setPropMethodName, int revision, CodeTypeMember ctm, CodeDomProvider cdp) {
			CodeStatementCollection ret = new CodeStatementCollection();
			CodeStatementCollection testStatements = new CodeStatementCollection();
			//code
			CodeVariableReferenceExpression vrMB, vrEx;
			//CodeExpression ce;
#if OTHER_VERSION
			const string EXTRACT_METHOD_NAME="Pack";
#else
			const string EXTRACT_METHOD_NAME="buildPackage";
#endif


			vrMB = new CodeVariableReferenceExpression("mb");
			vrEx = new CodeVariableReferenceExpression("ex");

			testStatements = new CodeStatementCollection(
				new CodeStatement[] {
					new CodeTryCatchFinallyStatement(
						new CodeStatement[] {
							new CodeExpressionStatement(
								new CodeMethodInvokeExpression(new CodeThisReferenceExpression(),setPropMethodName, vr)),
							new CodeAssignStatement(vrPkg,new CodeMethodInvokeExpression(vr,EXTRACT_METHOD_NAME)),
							new CodeExpressionStatement(
								new CodeMethodInvokeExpression(
									new CodeTypeReferenceExpression ("Trace"),
									"WriteLine",
									new CodeBinaryOperatorExpression(
										new CodePrimitiveExpression(vrPkg.VariableName+" = "),
										 CodeBinaryOperatorType.Add ,
										 new CodeBinaryOperatorExpression(
										 vrPkg,
										  CodeBinaryOperatorType.Add,
										  new CodePrimitiveExpression("."))))),
						},
						new CodeCatchClause[] {
							new CodeCatchClause(
								vrEx.VariableName,
								new CodeTypeReference(typeof(Exception)),
								new CodeStatement[] {
									createVarDef(vrMB,typeof(MethodBase),
										invokeMethod(typeof(MethodBase),"GetCurrentMethod")),
									new CodeCommentStatement("catch-1"),
									new CodeExpressionStatement(
										new CodeMethodInvokeExpression(
											new CodeTypeReferenceExpression(typeof(Trace)),
											"WriteLine",
											makeExpressionVector(
											new CodeExpression[] {
												new CodePropertyReferenceExpression(
													new CodePropertyReferenceExpression(
														vrMB,
														"ReflectedType"),
													"Name"),
												new CodePrimitiveExpression("."),
												new CodePropertyReferenceExpression(
													new CodePropertyReferenceExpression(
														vrMB,
														"ReflectedType"),
													"Name"),
												new CodePrimitiveExpression(" : "),
												new CodePropertyReferenceExpression(vrEx,"Message"),
												new CodePropertyReferenceExpression(
													new CodeTypeReferenceExpression(typeof(Environment)),"NewLine"),
												new CodePropertyReferenceExpression(vrEx,"StackTrace")
											},cdp)))
								}) },
						new CodeStatement[] {
								new CodeCommentStatement("finally")
						}),
					new CodeSnippetStatement()
				});

			foreach (ConstructorInfo cc in midType.GetConstructors())
				addCTOR(cc, ret, vr, vrPkg, midType, testStatements, revision, ctm);
			return ret;
		}

		static CodeVariableDeclarationStatement createVarDef(CodeVariableReferenceExpression vr, Type t, CodeExpression ceInit) {
			return new CodeVariableDeclarationStatement(t, vr.VariableName, ceInit);
		}
		static CodeExpression invokeMethod(Type t, string mname) {
			return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(t), mname);
		}

		CodeExpression makeExpressionVector(CodeExpression[] exprs, CodeDomProvider cdp) {
			CodeExpression ret = null,tmp=null;
			int n;

			if (exprs != null && (n = exprs.Length) > 0) {
				if (n == 1)
					ret = exprs[0];
				else {
					// create a recursive CodeBinaryOperatorExpression
					ret = exprs[0];
					for (int i = n - 2; i > 0; --i) {
						if (tmp == null)
							tmp = new CodeBinaryOperatorExpression(exprs[i], CodeBinaryOperatorType.Add, exprs[i + 1]);
						else {
							tmp = new CodeBinaryOperatorExpression(exprs[0], CodeBinaryOperatorType.Add, tmp);
						}
					}
					if (tmp != null)
						ret = tmp;
				}
			}

			if (ret == null)
				return new CodeSnippetExpression("#warning " +
					MethodBase.GetCurrentMethod().ReflectedType.Name + "." +
					MethodBase.GetCurrentMethod().Name + ": ACK!");
			else {
				showExpressions(cdp, exprs);
				showExpression(cdp, ret);
			}
			return ret;
		}

		void showExpression(CodeDomProvider cdp, CodeExpression ret) {
			if (cdp == null)
				return;
			showExpressions(cdp, new CodeExpression[] { ret });
		}

		void showExpressions(CodeDomProvider cdp, CodeExpression[] exprs) {
			StringBuilder sb;

			if (exprs == null)
				return;
			using (StringWriter sw = new StringWriter(sb = new StringBuilder())) {
				foreach (CodeExpression ce in exprs)
					cdp.GenerateCodeFromExpression(ce, sw, options);
			}
			Trace.WriteLine(sb.ToString());
		}

		void addCTOR(ConstructorInfo cc, CodeStatementCollection ret, CodeVariableReferenceExpression vr, CodeVariableReferenceExpression vrPkg, Type midType, CodeStatementCollection testStatements, int revision, CodeTypeMember ctm) {
			ParameterInfo[] parms;
			CodeExpressionCollection ceCtor = new CodeExpressionCollection();

			parms = cc.GetParameters();
			ret.Add(new CodeCommentStatement("Revision " + revision + "."));
			ret.Add(new CodeAssignStatement(vr, createObj(new CodeTypeReference(midType), parms, revision, ctm)));
			ret.AddRange(testStatements);
			//}
		}

		CodeExpression createObj(CodeTypeReference ctr, ParameterInfo[] parms, int revision, CodeTypeMember ctm) {
			CodeObjectCreateExpression ret = new CodeObjectCreateExpression(ctr);

			foreach (ParameterInfo aparm in parms)
				ret.Parameters.Add(createParameter(aparm, revision, ctm));
			return ret;
		}


		static readonly CodeExpression nullParm = new CodePrimitiveExpression();
		static readonly IDictionary<Type, CodeExpression> parmMap = new Dictionary<Type, CodeExpression>(           );

		CodeExpression createParameter(ParameterInfo aparm, int revision, CodeTypeMember ctm) {
			return createParameter(aparm.Name, aparm.ParameterType, revision, ctm);
		}

		CodeExpression createParameter(PropertyInfo pi, int revision, CodeTypeMember ctm) {
			return createParameter(pi.Name, pi.PropertyType, revision, ctm);
		}

		CodeExpression createParameter(string parmName, Type ptype, int revision, CodeTypeMember ctm) {
			string errMsg;

			if (string.Compare(parmName, "revision", true) == 0)
				return new CodePrimitiveExpression(revision);
			if (!parmMap.ContainsKey(ptype)) {
				if (ptype.Equals(typeof(string)))
					parmMap.Add(ptype, new CodePrimitiveExpression("STRING"));
				else if (ptype.Equals(typeof(int)))
					parmMap.Add(ptype, new CodePrimitiveExpression(0));
				else if (ptype.Equals(typeof(double)))
					parmMap.Add(ptype, new CodePrimitiveExpression(0));
				else if (ptype.Equals(typeof(long)))
					parmMap.Add(ptype, new CodePrimitiveExpression((long) 0));
				else if (ptype.Equals(typeof(bool)))
					parmMap.Add(ptype, new CodePrimitiveExpression(false));
				else if (ptype.Equals(typeof(decimal)))
					parmMap.Add(ptype, new CodePrimitiveExpression((decimal) 0));
				else if (ptype.Equals(typeof(DateTime)))
					parmMap.Add(ptype,
						new CodeFieldReferenceExpression(
							new CodeTypeReferenceExpression(ptype),
							"Now"));
				else if (ptype.IsEnum)
					parmMap.Add(ptype,
						new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(ptype), Enum.GetNames(ptype)[0]));
				else {
					if (ptype.IsInterface) {
						if (ptype.Name.StartsWith("Nullable")) {
							Trace.WriteLine("ack");
						} else if (ptype.Name.StartsWith("IEnumerable")) {
							Trace.WriteLine("ack");
							parmMap.Add(ptype,
								new CodeArrayCreateExpression(
									new CodeTypeReference(
										ptype.GetGenericArguments()[0]), 1));

						} else {
							Trace.WriteLine("ack");

						}
					}
				}
				if (!parmMap.ContainsKey(ptype)) {
					bool addAsNull=true;

					if (ptype.IsClass) {
						if (ptype.FullName.Contains("+")) {
							errMsg = "adding unknown type: (inner class)" + ptype.FullName + "!";
							if (ctm != null)
								ctm.Comments.Add(new CodeCommentStatement(errMsg));
							Trace.WriteLine(errMsg);
							parmMap.Add(ptype, new CodeObjectCreateExpression(ptype));
							addAsNull = false;
						}
					}
					if (addAsNull) {
						errMsg = "adding NULL for unknown type: " + ptype.FullName + "!";
						if (ctm != null)
							ctm.Comments.Add(new CodeCommentStatement(errMsg));
						Trace.WriteLine(errMsg);
						parmMap.Add(ptype, nullParm);
					}
				}

			}
			if (parmMap.ContainsKey(ptype))
				return parmMap[ptype];
			return nullParm;
		}

		CodeStatementCollection generateBestCTOR(CodeMemberMethod mm, Type atype, int revision, CodeDomProvider cdp, CodeMemberMethod m, CodeVariableReferenceExpression vr, CodeStatementCollection cscIn) {
			ConstructorInfo[] ctors = atype.GetConstructors();
			ParameterInfo[] parms;
			CodeStatementCollection cscRet = new CodeStatementCollection();
			CodeExpression ceCTOR = null;
			Type ptype, nullableType;
			bool ctorFound = false;
			int nparms, nctors = 0;
			foreach (ConstructorInfo ci in ctors = atype.GetConstructors()) {
				if ((nparms = (parms = ci.GetParameters()).Length) > 0) {
					nctors++;
					if (nparms == 1) {
						ptype = parms[0].ParameterType;
						if (string.Compare(parms[0].Name, "revision", true) == 0) {
							ceCTOR = new CodeObjectCreateExpression(
								atype,
								new CodePrimitiveExpression(revision));
							ctorFound = true;
						} else {
							if (ptype.IsConstructedGenericType) {
								if (ptype.IsInterface) {
									Trace.WriteLine("INTERFACE!");
									ceCTOR = new CodePrimitiveExpression();
									ctorFound = true;
								} else {
									Trace.WriteLine("NOT interface");
									if (ptype.Name.StartsWith("Nullable")) {
										if ((nullableType = ptype.GetGenericArguments()[0]).Equals(typeof(int))) {
											ceCTOR = new CodeObjectCreateExpression(
												atype,
												new CodePrimitiveExpression(revision));
											ctorFound = true;
											mm.Statements.Add(new CodeCommentStatement("nullable int?"));
										} else {
											mm.Statements.Add(new CodeCommentStatement("nullable " + cdp.GetTypeOutput(new CodeTypeReference(nullableType))));
										}
									}
								}
							} else if (ptype.Equals(typeof(int))) {
								ceCTOR = new CodeObjectCreateExpression(
									atype,
									new CodePrimitiveExpression(-1));
								ctorFound = true;
							} else {
								if (ptype.Equals(typeof(string))) {
									ceCTOR = new CodeObjectCreateExpression(
										atype,
										new CodePrimitiveExpression("TEST"));
									ctorFound = true;
								} else if (ptype.Equals(typeof(bool))) {
									ceCTOR = new CodeObjectCreateExpression(
										atype,
										new CodePrimitiveExpression(false));
									ctorFound = true;
								} else if (ptype.Equals(typeof(DateTime))) {
									ceCTOR = new CodeObjectCreateExpression(
										atype,
										new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(DateTime)), "Now"));
									ctorFound = true;
								} else {
									Trace.WriteLine("not INT / revision!" + Environment.NewLine + "have " + cdp.GetTypeOutput(new CodeTypeReference(ptype)) + ".");
									addPossibleCTORComment(cscRet, ci, mm, cdp, vr, revision);
								}
							}
						}
					} else {
						Trace.WriteLine("have " + nparms + " parameters.");
						addPossibleCTORComment(cscRet, ci, m, cdp, vr, revision);
					}
				}
				if (ctorFound)
					break;
			}
			bool ctorAdded = true;
			if (ceCTOR == null) {
				if (nctors <= 1) {// no ctors with args
					cscRet.Add(new CodeAssignStatement(vr, new CodeObjectCreateExpression(atype)));
					ctorAdded = true;
				} else {
					if (!ctorFound)
						cscRet.Add(new CodeSnippetStatement("#warning CTOR not found!"));
				}
			} else {
				cscRet.Add(new CodeAssignStatement(vr, ceCTOR));
				ctorAdded = true;
			}
			if (ctorAdded)
				cscRet.AddRange(cscIn);
			return cscRet;
		}
		static void addVarDefs(Type atype, CodeMemberMethod m, CodeVariableReferenceExpression vr, CodeVariableReferenceExpression vrPkg) {
			m.Statements.AddRange(new CodeStatement[] {
				new CodeVariableDeclarationStatement(atype,vr.VariableName),
				new CodeVariableDeclarationStatement(typeof(string),vrPkg.VariableName),
				new CodeSnippetStatement()
			});
		}
		void addRegionTo(CodeStatement cs, string v) {
			if (cs != null) {
				cs.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, v));
				cs.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, v));
			}
		}
		void addPossibleCTORComment(CodeStatementCollection csc, ConstructorInfo ci, CodeMemberMethod m, CodeDomProvider cdp, CodeVariableReferenceExpression vr, int revision) {
			StringBuilder sb = new StringBuilder();
			ParameterInfo[] parms;
			CodeExpressionCollection cecParms = new CodeExpressionCollection();
			CodeObjectCreateExpression coce;
			CodeExpression ceTmp;

			parms = ci.GetParameters();
			sb.Append("possible ctor: ");
			sb.Append(ci.DeclaringType.FullName);
			sb.Append("(");
			for (int i = 0; i < parms.Length; i++) {
				if (i > 0) {
					sb.Append(", ");
				}
				sb.Append(cdp.GetTypeOutput(new CodeTypeReference(parms[i].ParameterType)) + " " + parms[i].Name);
				ceTmp = createParameter(parms[i], revision, m);
				Trace.WriteLine("fix this");
			}
			sb.Append(")");
			csc.Add(new CodeCommentStatement(sb.ToString()));
#if true
			csc.Add(
				new CodeAssignStatement(vr,
					coce = new CodeObjectCreateExpression(ci.DeclaringType)));
#else
			csc.Add(
				new CodeVariableDeclarationStatement(
			ci.DeclaringType, vr.VariableName,
				coce=new CodeObjectCreateExpression(ci.DeclaringType)));
#endif
			coce.Parameters.AddRange(cecParms);
		}
	}
}