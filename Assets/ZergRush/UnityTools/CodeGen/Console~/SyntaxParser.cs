using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class TreePruner : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.AccessorList == null) return node;
        var getSetList = new List<AccessorDeclarationSyntax>();
        if (node.AccessorList.Accessors.Any(a => a.Keyword.Kind() == SyntaxKind.GetKeyword))
            getSetList.Add(SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)));


        if (node.AccessorList.Accessors.Any(a => a.Keyword.Kind() == SyntaxKind.SetKeyword))
            getSetList.Add(SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)));
        var list = SF.List(getSetList);
        var al = SF.AccessorList(list);
        return base.VisitPropertyDeclaration(node.ReplaceNode(node.AccessorList, al));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var type = node.ReturnType;
        var returnExpression = SF.DefaultExpression(type);
        if (type is PredefinedTypeSyntax predefined)
        {
            if (predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                returnExpression = null;
        }


        var returnStatement = SF.ReturnStatement(default,
            SF.Token(SyntaxKind.ReturnKeyword).WithTrailingTrivia(SF.Whitespace(" ")), returnExpression,
            SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        if (node.Body != null)
        {
            var newBody = SF.Block(returnStatement);
            return base.VisitMethodDeclaration(node.ReplaceNode(node.Body, newBody));
        }

        if (node.ExpressionBody != null)
        {
            var newBody = SF.ArrowExpressionClause(returnExpression);
            return base.VisitMethodDeclaration(node.ReplaceNode(node.ExpressionBody, newBody));
        }

        return base.VisitMethodDeclaration(node);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (node.Parent is StructDeclarationSyntax)
            return base.VisitConstructorDeclaration(node);
        if (node.Body != null)
            return base.VisitConstructorDeclaration(node.ReplaceNode(node.Body, SF.Block()));
        if(node.ExpressionBody != null)
            throw new NotImplementedException("WHY IN THE NAME OF GOD WOULD YOU USE EXPRESSION CONSTRUCTOR !??");
        return base.VisitConstructorDeclaration(node);
    }
}


public static partial class TypeReader
{
    public static SyntaxTree PruneTree(SyntaxTree original)
    {
        var originalRoot = original.GetRoot();
        var tp = new TreePruner();
        var newRoot = tp.Visit(originalRoot);
        return newRoot.SyntaxTree.WithFilePath(original.FilePath);
    }


    public static (List<string>, List<string>, List<string>) FindAllFilesInProject(string projectPath,
        string projectFile)
    {
        var files = new List<string>();
        var references = new List<string>();
        var projects = new List<string>();
        var xDoc = new XmlDocument(); // Creating Document
        XmlNode? attr;
        xDoc.Load($@"{projectPath}\{projectFile}"); // Loading standart assembly

        var xRoot = xDoc.DocumentElement; // Extracting root element
        if (xRoot == null)
            throw new NullReferenceException();

        foreach (var node in xRoot.OfType<XmlElement>())
        {
            if (!(node is {Name: "ItemGroup"})) continue;
            foreach (var childNode in node.Cast<XmlNode?>().Where(childNode => childNode != null))
            {
                switch (childNode.Name)
                {
                    case "Compile":
                    {
                        attr = childNode.Attributes?.Item(0);
                        if (attr?.Value == null) continue;
                        files.Add($@"{projectPath}\{attr.Value}");
                        continue;
                    }
                    case "Reference":
                    {
                        references.AddRange(from XmlNode? cNode in childNode
                            where cNode != null
                            where cNode.Name.Equals("HintPath")
                            where cNode.InnerText != ""
                            select $@"{cNode.InnerText}");
                        continue;
                    }

                    case "ProjectReference":
                    {
                        attr = childNode.Attributes?.Item(0);
                        if (attr?.Value == null) continue;
                        projects.Add($@"{projectPath}\{attr.Value}");
                        continue;
                    }
                }
            }
        }

        return (files, references, projects);
    }

    public static string GetOutPath(string rPath)
    {
        var xDoc = new XmlDocument(); // Creating Document
        XmlNode? attr;
        xDoc.Load(rPath); // Loading standart assembly
        var path = "";
        var name = "";


        var xRoot = xDoc.DocumentElement; // Extracting root element
        if (xRoot == null)
            throw new NullReferenceException();
        foreach (var node in xRoot.OfType<XmlElement>())
        {
            if (!(node is {Name: "PropertyGroup"})) continue;
            foreach (var childNode in node.Cast<XmlNode?>().Where(childNode => childNode != null))
            {
                if ((childNode is {Name: "OutputPath"}))
                    path = childNode.InnerText;
                if ((childNode is {Name: "AssemblyName"}))
                    name = childNode.InnerText;
            }
        }

        return @$"{path}{name}.dll";
    }
}
