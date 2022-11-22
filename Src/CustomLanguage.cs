using System.ComponentModel.Composition;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using System.Collections.Generic;
using ICSharpCode.Decompiler.Disassembler;

namespace Backlang.Ilspy
{
    [Export(typeof(Language))]
    public class CustomLanguage : Language
    {

        readonly HighlightingColor typeColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.LightBlue) };
        readonly HighlightingColor keywordColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };

        private readonly Dictionary<string, string> primitiveTypeTable = new Dictionary<string, string>()
        {
            {"String", "string"},
            {"Int32", "i32"},
            {"Int64", "i64"},
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
                smart.Write(type.Namespace + ";");
            }

            smart.WriteLine();
            smart.WriteLine();

            if (type.IsAbstract)
            {
                WriteKeyword(smart, "abstract");
            }
            if (type.IsStatic)
            {
                WriteKeyword(smart, "static");
            }

            if (type.Name != "FreeFunctions")
            {
                if (type.Kind == TypeKind.Struct)
                {
                    WriteKeyword(smart, "struct");
                }
                else if (type.Kind == TypeKind.Interface)
                {
                    WriteKeyword(smart, "interface");
                }
                else
                {
                    WriteKeyword(smart, "class");
                }

                smart.WriteReference(type, type.Name, true);
                smart.Write(" ");

                smart.MarkFoldStart();
                smart.WriteLine("{");

                smart.WriteLine();

                smart.Indent();
            }

            foreach(var field in type.Fields) {
                DecompileField(field, output, options);
            }

            smart.WriteLine();

            foreach (var method in type.Methods)
            {
                DecompileMethod(method, smart, options);
            }

            if (type.Name != "FreeFunctions")
            {
                smart.Unindent();

                smart.WriteLine("}");
                smart.MarkFoldEnd();
            }
        }

        public override RichText GetRichTextTooltip(IEntity entity)
        {
            return new RichText(entity.Name);
        }

        public override string PropertyToString(IProperty property, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            return property.FullName;
        }

        private void WriteKeyword(ISmartTextOutput smart, string keyword)
        {
            smart.BeginSpan(keywordColor);
            smart.Write(keyword + " ");
            smart.EndSpan();
        }

        public override void DecompileField(IField field, ITextOutput output, DecompilationOptions options)
        {
            var smart = output as ISmartTextOutput;

            WriteKeyword(smart, "let");
            smart.WriteReference(field, field.Name, true);
            smart.Write(": ");
            WriteType(smart, field.Type);

            var value = field.GetConstantValue();

            if(value != null) {
                smart.Write(" = ");
                smart.WriteLine(value.ToString());
            }

            smart.WriteLine(";");
        }

        public override void DecompileMethod(IMethod method, ITextOutput output, DecompilationOptions options)
        {
            var smart = (ISmartTextOutput)output;

            var module = ((MetadataModule)method.ParentModule).PEFile;
            var methodDef = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);

            if(method.IsAbstract){
                WriteKeyword(smart, "abstract");
            }
            if(method.IsStatic) {
                WriteKeyword(smart, "static");
            }

            if (method.IsConstructor)
            {
                smart.BeginSpan(keywordColor);
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

            if (methodDef.HasBody())
            {
                smart.WriteLine(" {");
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

                smart.WriteLine("}");
                smart.MarkFoldEnd();
                smart.WriteLine();
            }
            else
            {
                smart.Write(";");
            }

            smart.WriteLine();
        }

        private void WriteType(ISmartTextOutput smart, IType p)
        {
            smart.BeginSpan(typeColor);
            string typename = p.Name;

            if (primitiveTypeTable.ContainsKey(typename))
            {
                typename = primitiveTypeTable[typename];
            }

            smart.WriteReference(p, typename);
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