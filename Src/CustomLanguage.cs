using System.ComponentModel.Composition;
using System.Reflection.Metadata;
using System.Windows.Controls;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Solution;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using System.Collections.Generic;

namespace Backlang.Ilspy
{
    [Export(typeof(Language))]
    public class CustomLanguage : Language
    {

        HighlightingColor typeColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.DarkGray) };
        HighlightingColor keywordColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };

        private Dictionary<string, string> primitiveTypeTable = new Dictionary<string, string>()
        {
            {"String", "string"},
            {"Int32", "i32"}
        };

        public override string Name
        {
            get
            {
                return "Backlang";
            }
        }

        public override string FileExtension
        {
            get
            {
                // used in 'Save As' dialog
                return ".back";
            }
        }

        public override void DecompileType(ITypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            var smart = (ISmartTextOutput)output;
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                WriteKeyword(smart, "module");
                smart.Write(type.Namespace + ";");
            }

            smart.WriteLine();

            smart.WriteLine();

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

        private void WriteKeyword(ISmartTextOutput smart, string keyword)
        {
            smart.BeginSpan(keywordColor);
            smart.Write(keyword + " ");
            smart.EndSpan();
        }


        // There are several methods available to override; in this sample, we deal with methods only
        public override void DecompileMethod(IMethod method, ITextOutput output, DecompilationOptions options)
        {
            var smart = (ISmartTextOutput)output;

            var module = ((MetadataModule)method.ParentModule).PEFile;
            var methodDef = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);

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
                smart.WriteLine("{");
                var methodBody = module.Reader.GetMethodBody(methodDef.RelativeVirtualAddress);

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
    }
}