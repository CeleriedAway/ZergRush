using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


public class TreePruner : CSharpSyntaxRewriter
{
    public List<string> InterfacePruneException = new List<string>
    {
        "INetEventListener",
        "IDeliveryEventListener",
        "INtpEventListener",
        "INatPunchListener",
        "ICell",
        "IHasSkeletonDataAsset",
        "IReferencableFromDataRoot"
    };
    
    ThrowStatementSyntax BuildException()
    {
        var exceptionExpr = SF.ObjectCreationExpression(SF.ParseName("System.NotImplementedException"))//
            .WithArgumentList(SF.ArgumentList())
            .WithNewKeyword(SF.Token(SyntaxKind.NewKeyword));
        var throwExc = SF.ThrowExpression(exceptionExpr);
        var throwExcStat = SF.ThrowStatement(exceptionExpr).NormalizeWhitespace().WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed);

        return throwExcStat;
    }

    // public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    // {
    //     if (node.Usings.Any(us => us.Name.ToString() == "System")) return base.VisitCompilationUnit(node);
    //     ;
    //     node = node.AddUsings(SF.UsingDirective(SF.ParseName("System")).NormalizeWhitespace());
    //     return base.VisitCompilationUnit(node);
    // }

    /*
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var n = node.WithMembers(new SyntaxList<MemberDeclarationSyntax>(node.Members.Where(m =>
            !(m is MethodDeclarationSyntax method && method.ExplicitInterfaceSpecifier != null))));
        return base.VisitClassDeclaration(n);
    }
    */

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        if (InterfacePruneException.Contains(node.Identifier.Text))
            return base.VisitInterfaceDeclaration(node);
        return base.VisitInterfaceDeclaration(node.WithMembers(new SyntaxList<MemberDeclarationSyntax>()));
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.ExpressionBody != null)
        {
            node = node.RemoveNode(node.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!
                .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)
                    .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia)
                    .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));
            node = node.WithAccessorList(
                SF.AccessorList(
                    SF.List(new List<AccessorDeclarationSyntax>()
                        {
                            SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithBody(SF.Block(BuildException())).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed)
                        }
                    )
                )
            );
            return base.VisitPropertyDeclaration(node.WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
        }

        if (node.AccessorList == null) return base.VisitPropertyDeclaration(node);
        if (node.AccessorList.Accessors.Count == 0) return base.VisitPropertyDeclaration(node);
        var i = node.AccessorList.Accessors.Count;
        while (i > 0)
        {
            i--;
            var oldAcc = node.AccessorList!.Accessors[i];
            AccessorDeclarationSyntax newAcc;
            if (oldAcc.ExpressionBody != null)
            {
                newAcc = oldAcc.RemoveNode(oldAcc.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!
                    .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)
                        .WithLeadingTrivia(oldAcc.SemicolonToken.LeadingTrivia)
                        .WithTrailingTrivia(oldAcc.SemicolonToken.TrailingTrivia));

                newAcc = newAcc.WithBody(SF.Block(BuildException()).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
                node = node.ReplaceNode(oldAcc, newAcc);
                continue;
            }

            if (oldAcc.Body == null) continue;

            newAcc = oldAcc.ReplaceNode(oldAcc.Body, SF.Block(BuildException()).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
            node = node.ReplaceNode(oldAcc, newAcc);
        }
        return base.VisitPropertyDeclaration(node.WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var throwExcStat = BuildException();

        if (node.Body != null)
        {
            var newBody = SF.Block(throwExcStat);
            return base.VisitMethodDeclaration(node.ReplaceNode(node.Body, newBody).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
        }

        if (node.ExpressionBody == null) return base.VisitMethodDeclaration(node);
        node = node.RemoveNode(node.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!
            .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)
                .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia)
                .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));

        return base.VisitMethodDeclaration(node.WithBody(SF.Block(throwExcStat)).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var throwExcStat = BuildException();

        if (node.Body != null)
            return base.VisitConstructorDeclaration(node.ReplaceNode(node.Body, SF.Block(throwExcStat)));
        if (node.ExpressionBody == null) return base.VisitConstructorDeclaration(node);
        node = node.RemoveNode(node.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!
            .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)
                .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia)
                .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));
        return base.VisitConstructorDeclaration(node.WithBody(SF.Block(throwExcStat)).WithTrailingTrivia(SF.ElasticCarriageReturnLineFeed));
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
        var filename = Path.Combine(projectPath, projectFile);
        if (File.Exists(filename) == false)
        {
            Console.Error.WriteLine($"can't find project {filename}");
            goto ret;
        }
        xDoc.Load(filename); // Loading standart assembly

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
                        if (projectFile.Contains("ZergRush.Core") == false && attr.Value.EndsWith(".gen.cs")) continue;
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

        ret:
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