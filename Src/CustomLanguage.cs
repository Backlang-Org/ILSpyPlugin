using System.ComponentModel.Composition;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy;
using System.Collections.Generic;
using ICSharpCode.Decompiler.Disassembler;
using System.Linq;

namespace Backlang.Ilspy
{
    [Export(typeof(Language))]
    public class CustomLanguage : Language
    {
        private readonly Dictionary<string, string> primitiveTypeTable = new Dictionary<string, string>()
        {
            {"Boolean", "bool"},
            {"Object", "obj"},

            {"String", "string"},
            {"Char",   "char"},

            {"Byte",   "i8"},
            {"Int16", "i16"},
            {"Int32", "i32"},
            {"Int64", "i64"},

            {"Half", "f16"},
            {"Float", "f32"},
            {"Double", "f64"},
        };

        public override string Name
        {
            get
            {
                return "Backlang";
            }
        }

        public override string FileExtension => ".back";

        public override string ProjectFileExtension => ".backproj";

        public override void DecompileType(ITypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            var smart = output as ISmartTextOutput;

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                WriteKeyword(smart, "module");
                smart.WriteLine(type.Namespace + ";");
            }
            smart.WriteLine();

            if (type.Name != "FreeFunctions")
            {
                WriteAccessibility(type.Accessibility, smart);

                if (type.IsSealed)
                {
                    WriteKeyword(smart, "sealed");
                }
                if (type.IsStatic)
                {
                    WriteKeyword(smart, "static");
                }
                if (type.IsAbstract)
                {
                    WriteKeyword(smart, "abstract");
                }

                if (type.Kind == TypeKind.Struct)
                {
                    WriteKeyword(smart, "struct");
                }
                else if (type.Kind == TypeKind.Interface)
                {
                    WriteKeyword(smart, "interface");
                }
                else if (type.Kind == TypeKind.Enum)
                {
                    WriteKeyword(smart, "enum");
                }
                else
                {
                    WriteKeyword(smart, "class");
                }

                WriteType(smart, type, true);

                var baseTypes = type.GetAllBaseTypes().Where(_ => _.FullName != "System.Object"
                                && _ != type && _.FullName != "System.Enum"
                                && _.FullName != "System.ValueType").ToArray();

                if (baseTypes.Any())
                {
                    smart.Write(" : ");
                    for (int i = 0; i < baseTypes.Length; i++)
                    {
                        var bt = baseTypes[i];

                        WriteType(smart, bt);

                        if (i < baseTypes.Length - 1)
                        {
                            smart.Write(", ");
                        }
                    }
                }

                smart.MarkFoldStart();
                smart.WriteLine(" {");

                smart.WriteLine();

                smart.Indent();
            }

            foreach (var field in type.Fields)
            {
                DecompileField(field, output, options);
            }

            smart.WriteLine();



            if (type.Name != "FreeFunctions")
            {
                smart.Unindent();

                smart.MarkFoldEnd();
                smart.WriteLine("}");

                smart.WriteLine();

                smart.MarkFoldStart();
                WriteKeyword(smart, "implement");
                WriteType(smart, type);
                smart.WriteLine(" {");

                smart.Indent();
            }

            foreach (var method in type.Methods)
            {
                DecompileMethod(method, smart, options);
            }

            if (type.Name != "FreeFunctions")
            {
                smart.Unindent();

                smart.MarkFoldEnd();
                smart.WriteLine("}");
            }
        }

        public override string PropertyToString(IProperty property, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            return property.FullName;
        }

        private void WriteKeyword(ISmartTextOutput smart, string keyword)
        {
            smart.BeginSpan(Colors.KeywordColor);
            smart.Write(keyword + " ");
            smart.EndSpan();
        }

        public override void DecompileField(IField field, ITextOutput output, DecompilationOptions options)
        {
            var smart = output as ISmartTextOutput;

            WriteAccessibility(field.Accessibility, smart);

            WriteKeyword(smart, "let");

            if (!field.IsReadOnly)
            {
                WriteKeyword(smart, "mut");
            }

            smart.WriteReference(field, field.Name, true);
            smart.Write(": ");
            WriteType(smart, field.Type);

            var value = field.GetConstantValue();

            if (value != null)
            {
                smart.Write(" = ");

                WriteValue(smart, value);
            }

            smart.WriteLine(";");
        }

        private void WriteValue(ISmartTextOutput smart, object value)
        {
            if (value is string s)
            {
                smart.BeginSpan(Colors.StringColor);
                smart.Write('"' + s.Replace("\"", "\\\"") + '"');
                smart.EndSpan();
            }
            else if (value is char c)
            {
                smart.BeginSpan(Colors.StringColor);
                smart.Write("'" + c + "'");
                smart.EndSpan();
            }
            else if (value is bool b)
            {
                WriteKeyword(smart, b.ToString().ToLower());
            }
            else
            {
                smart.Write(value.ToString());
            }
        }

        public override void DecompileMethod(IMethod method, ITextOutput output, DecompilationOptions options)
        {
            var smart = output as ISmartTextOutput;

            WriteAccessibility(method.Accessibility, smart);

            if (method.IsAbstract)
            {
                WriteKeyword(smart, "abstract");
            }
            if (method.IsStatic)
            {
                WriteKeyword(smart, "static");
            }

            if (method.IsOverride)
            {
                WriteKeyword(smart, "override");
            }
            if (method.IsOperator)
            {
                WriteKeyword(smart, "operator");
            }

            if (method.IsConstructor)
            {
                smart.BeginSpan(Colors.KeywordColor);
                smart.WriteReference(method, "constructor", true);
                smart.EndSpan();
            }
            else
            {
                WriteKeyword(smart, "func");
                smart.WriteReference(method, method.Name, true);
            }

            smart.Write("(");

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                IParameter p = method.Parameters[i];

                smart.Write(p.Name);
                smart.Write(": ");
                WriteType(smart, p.Type);

                if (i != method.Parameters.Count - 1)
                {
                    smart.Write(", ");
                }
            }

            smart.MarkFoldStart();
            smart.Write(")");

            if (method.ReturnType.FullName != "System.Void")
            {
                smart.Write(" -> ");
                WriteType(smart, method.ReturnType);
            }

            var module = method.ParentModule.PEFile;

            if (method.HasBody)
            {
                smart.WriteLine(" {");
                var methodDef = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
                var methodBody = module.Reader.GetMethodBody(methodDef.RelativeVirtualAddress);

                smart.Indent();

                var blob = methodBody.GetILReader();
                while (blob.RemainingBytes > 0)
                {
                    var code = blob.DecodeOpCode();
                    switch (code)
                    {
                        default: break;
                    }
                }

                smart.Unindent();

                smart.MarkFoldEnd();
                smart.WriteLine("}");
                smart.WriteLine();
            }
            else
            {
                smart.Write(";");
            }

            smart.WriteLine();
        }

        private void WriteAccessibility(Accessibility access, ISmartTextOutput smart)
        {
            if (access == Accessibility.Public)
            {
                WriteKeyword(smart, "public");
            }
            else if (access == Accessibility.Private)
            {
                WriteKeyword(smart, "private");
            }
            else if (access == Accessibility.Internal)
            {
                WriteKeyword(smart, "internal");
            }
            else if (access == Accessibility.Protected)
            {
                WriteKeyword(smart, "protected");
            }
        }

        private void WriteType(ISmartTextOutput smart, IType p, bool isDefinition = false)
        {
            smart.BeginSpan(Colors.TypeColor);
            string typename = p.Name;

            if (primitiveTypeTable.ContainsKey(typename))
            {
                typename = primitiveTypeTable[typename];
            }

            smart.WriteReference(p, typename, isDefinition);
            smart.EndSpan();
        }

        public override string TypeToString(IType type, bool includeNamespace)
        {
            return type.FullName;
        }

        public override string MethodToString(IMethod method, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            return method.FullName;
        }

        public override string FieldToString(IField field, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            return field.FullName;
        }

        public override string GetTooltip(IEntity entity)
        {
            return entity.FullName;
        }
    }
}