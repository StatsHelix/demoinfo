using System;
using google.protobuf;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;

namespace Brotogen
{
	public class CSharpGenerator
	{
		#region helper stuff
		CodeAttributeDeclaration IsObsolete = new CodeAttributeDeclaration(s<ObsoleteAttribute>());
		CodeAttributeDeclaration IsSerializable = new CodeAttributeDeclaration(s<SerializableAttribute>());
		#endregion

		public bool SharpifyNames { get; set; }
		public bool GenerateProperties { get; set; }
		public bool GenerateStructs { get; set; }

		public CSharpGenerator (FileDescriptorSet files)
		{
			GenerateProperties = false;
			SharpifyNames = true;
			GenerateStructs = true;

			var unit = new CodeCompileUnit();
			foreach (var file in files.file) {
				var codeDom = GenerateFile (file);

				unit.Namespaces.Add (codeDom);
			}

			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.BracingStyle = "java";

			StringWriter sourceWriter = new StringWriter ();
			provider.GenerateCodeFromCompileUnit(unit, sourceWriter, options);

			var res = sourceWriter.ToString ();
		}

		public CodeNamespace GenerateFile(FileDescriptorProto file)
		{
			//Set the namespace to the package - C# namespaces
			//aren't like java namespaces, so we rather want
			//google.protobuf instead of com.google.protobuf
			//that is why we ignore the java-stuff, even if it's 
			//there
			CodeNamespace result = new CodeNamespace (file.package);

			result.Imports.Add (new CodeNamespaceImport("System"));
			result.Imports.Add (new CodeNamespaceImport("DemoInfo"));

			foreach (var en in file.enum_type) {
				result.Types.Add (CreateEnum (en));
			}

			foreach (var message in file.message_type) {
				result.Types.Add (CreateClass (message));
			}


			return result;
		}

		public CodeTypeDeclaration CreateClass(DescriptorProto target)
		{
			CodeTypeDeclaration resultingClass = new CodeTypeDeclaration (target.name);
			resultingClass.IsClass = !(resultingClass.IsStruct = GenerateStructs);

			//Everything can be serialized
			resultingClass.CustomAttributes.Add (IsSerializable);

			if(target.options != null && target.options.deprecatedSpecified && target.options.deprecated)
				resultingClass.CustomAttributes.Add (IsObsolete);

			foreach (var en in target.enum_type) {
				resultingClass.Members.Add (CreateEnum (en));
			}

			foreach (var subMessage in target.nested_type) {
				resultingClass.Members.Add (CreateClass (subMessage));
			}

			//The field declarations
			foreach (var field in target.field) {
				field.name = SharpifyName (field.name);

				var resultingProperty = new CodeMemberProperty ();

				resultingProperty.Type = GetFieldType(field);

				resultingProperty.Name = field.name;

				resultingProperty.HasGet = true;
				resultingProperty.GetStatements.Add (new CodeMethodReturnStatement (new CodeVariableReferenceExpression (fieldName(field.name))));
				resultingProperty.Attributes = MemberAttributes.Public;


				if (target.options != null && target.options.deprecatedSpecified && target.options.deprecated) {
					resultingProperty.CustomAttributes.Add (IsObsolete);
				}

				var resultingField = new CodeMemberField ();
				resultingField.Type = resultingProperty.Type;
				resultingField.Name = fieldName(field.name);
				resultingField.Attributes = MemberAttributes.Private;


				if (GenerateProperties) {
					resultingClass.Members.Add (resultingField);
					resultingClass.Members.Add (resultingProperty);
				} else {
					resultingField.Attributes = resultingProperty.Attributes;
					resultingClass.Members.Add (resultingField);
				}
			}

			return resultingClass;
		}

		private void GenerateParseMethod(DescriptorProto target)
		{
			var method = new CodeMemberMethod ();
			method.Name = "Parse";
			method.Parameters.Add (new CodeParameterDeclarationExpression (new CodeTypeReference (s<DemoInfo.IBitStream>()), "bitstream"));


			int maxFieldNumber = target.field.Max (a => a.number);
			for (int i = 0; i < maxFieldNumber; i++) {
			}
		}

		private CodeTypeDeclaration CreateEnum(EnumDescriptorProto target)
		{
			CodeTypeDeclaration resultingEnum = new CodeTypeDeclaration (target.name);
			resultingEnum.IsEnum = true;

			RemovePrefixes(target);

			if (target.options != null && target.options.deprecatedSpecified && target.options.deprecated) {
				resultingEnum.CustomAttributes.Add (IsObsolete);
			}

			foreach (var rawEnum in target.value) {
				var member = new CodeMemberField ();

				if (rawEnum.options != null && rawEnum.options.deprecatedSpecified && rawEnum.options.deprecated)
					member.CustomAttributes.Add (IsObsolete);

				member.Name = rawEnum.name;
				member.InitExpression = new CodePrimitiveExpression (rawEnum.number);

				resultingEnum.Members.Add (member);
			}

			return resultingEnum;
		}

		void RemovePrefixes (EnumDescriptorProto target)
		{
			if (SharpifyNames) {
				if (target.value.Count != 0) {
					//Remove the prefixes - the rest is done later
					int currentPrefix = target.value.Min (a => a.name.Length) - 1;
					string prefix = target.value [0].name.Substring (0, currentPrefix);
					foreach (var option in target.value) {
						while (currentPrefix > 0 && prefix != option.name.Substring (0, currentPrefix)) {
							currentPrefix--;
							prefix = prefix.Substring (0, currentPrefix);
						}
					}
					foreach (var option in target.value) {
						option.name = SharpifyName (option.name.Substring (currentPrefix));
					}
				}
			}
		}

		static CodeTypeReference GetFieldType (FieldDescriptorProto field)
		{
			CodeTypeReference result = null;

			if (field.type_nameSpecified) {
				if (field.type_name.StartsWith (".")) {
					result = new CodeTypeReference (field.type_name.Substring (1));
				}
				else {
					result = new CodeTypeReference (field.type_name);
				}
			}
			else {
				switch (field.type) {
				case FieldDescriptorProto.Type.TYPE_BOOL:
					result = new CodeTypeReference (s<bool> ());
					break;
				case FieldDescriptorProto.Type.TYPE_BYTES:
					result = new CodeTypeReference (s<byte[]> ());
					break;
				case FieldDescriptorProto.Type.TYPE_DOUBLE:
					result = new CodeTypeReference (s<double> ());
					break;
				case FieldDescriptorProto.Type.TYPE_ENUM:
					//is never called anyways, becasue this has a name specified
					throw new Exception ("What kind of enum? Could not encode field, aborting.");
					break;
				case FieldDescriptorProto.Type.TYPE_FIXED32:
					//C# has no real fixed types afaik. 
					result = new CodeTypeReference (s<double> ());
					break;
				case FieldDescriptorProto.Type.TYPE_FIXED64:
					result = new CodeTypeReference (s<double> ());
					break;
				case FieldDescriptorProto.Type.TYPE_FLOAT:
					result = new CodeTypeReference (s<float> ());
					break;
				case FieldDescriptorProto.Type.TYPE_GROUP:
					//wat
					throw new Exception ("I don't find specs about a group - mind helping me?");
				case FieldDescriptorProto.Type.TYPE_INT32:
					result = new CodeTypeReference (s<int> ());
					break;
				case FieldDescriptorProto.Type.TYPE_INT64:
					result = new CodeTypeReference (s<long> ());
					break;
				case FieldDescriptorProto.Type.TYPE_SFIXED32:
					result = new CodeTypeReference (s<double> ());
					break;
				case FieldDescriptorProto.Type.TYPE_SFIXED64:
					result = new CodeTypeReference (s<double> ());
					break;
				case FieldDescriptorProto.Type.TYPE_SINT32:
					result = new CodeTypeReference (s<int> ());
					break;
				case FieldDescriptorProto.Type.TYPE_SINT64:
					result = new CodeTypeReference (s<int> ());
					break;
				case FieldDescriptorProto.Type.TYPE_STRING:
					result = new CodeTypeReference (s<string> ());
					break;
				case FieldDescriptorProto.Type.TYPE_UINT32:
					result = new CodeTypeReference (s<uint> ());
					break;
				case FieldDescriptorProto.Type.TYPE_UINT64:
					result = new CodeTypeReference (s<ulong> ());
					break;
				}
			}

			return result;
		}

		string SharpifyName (string name)
		{
			if (SharpifyNames) {
				var parts = name.Split ('_');
				name = "";
				for (int i = 0; i < parts.Length; i++) {
					string part = parts [i];
					if (part.Length != 0) {
						name += char.ToUpper (part [0]) + part.Substring (1).ToLower ();
					}
				}
			}

			return name;
		}

		private static string s<T>()
		{
			return typeof(T).Name;
		}


		private static string d(Delegate d, string instanceName)
		{
			return instanceName + "." + d;
		}

		private string fieldName(string fieldName)
		{
			if (GenerateProperties) {
				return "_" + fieldName;
			}

			return fieldName;
		}
	}
}

