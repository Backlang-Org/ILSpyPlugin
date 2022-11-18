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

namespace Backlang.Ilspy
{
    [Export(typeof(Language))]
    public class CustomLanguage : Language
    {

        HighlightingColor gray = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.DarkGray) };
        HighlightingColor blue = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };

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
            if (type.Namespace != null)
            {
                WriteKeyword(smart, "module");
                smart.Write(type.Namespace + ";");
            }

            smart.WriteLine();

            smart.WriteLine();

            WriteKeyword(smart, "class");
            smart.WriteReference(type, type.Name, true);
            smart.Write(" ");

            smart.MarkFoldStart();
            smart.WriteLine("{");

            smart.WriteLine();

            smart.Indent();
            foreach (var method in type.Methods)
            {
                DecompileMethod(method, smart, options);
            }
            smart.Unindent();

            smart.WriteLine("}");
            smart.MarkFoldEnd();
        }

        private void WriteKeyword(ISmartTextOutput smart, string keyword)
        {
            smart.BeginSpan(blue);
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
                smart.BeginSpan(blue);
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
                smart.WriteReference(p.Type, p.Type.Name);

                if (i != method.Parameters.Count - 1)
                {
                    smart.Write(", ");
                }
            }

            smart.WriteLine(") {");
            smart.WriteLine("}");

            if (methodDef.HasBody())
            {
                var methodBody = module.Reader.GetMethodBody(methodDef.RelativeVirtualAddress);

                
            }

            smart.WriteLine();
        }
    }
}