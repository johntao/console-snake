using Tomlyn;
using Tomlyn.Syntax;

static class TomlTableFormatHelper
{
    class TableVisitor : SyntaxVisitor
    {
        public override void Visit(TableSyntax table)
        {
            table.AddLeadingTriviaNewLine();
            base.Visit(table);
        }
    }
    internal static string? Do(string txt)
    {
        var doc = Toml.Parse(txt, options: TomlParserOptions.ParseOnly);
        doc.Accept(new TableVisitor());
        return doc.ToString();
    }
}
