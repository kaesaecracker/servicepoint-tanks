using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EndiannessSourceGenerator;

internal class DebugMeException(string message) : Exception(message);

internal class InvalidUsageException(string message) : Exception(message);

[Generator]
public class StructEndiannessSourceGenerator : ISourceGenerator
{
    private const string Namespace = "EndiannessSourceGenerator";
    private const string AttributeName = "StructEndiannessAttribute";
    private const string IsLittleEndianProperty = "IsLittleEndian";

    private const string AttributeSourceCode =
        $$"""
          // <auto-generated/>
          namespace {{Namespace}}
          {
              [System.AttributeUsage(System.AttributeTargets.Struct)]
              public class {{AttributeName}}: System.Attribute
              {
                  public required bool {{IsLittleEndianProperty}} { get; init; }
              }
          }
          """;

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterForPostInitialization(i => i.AddSource($"{AttributeName}.g.cs", AttributeSourceCode));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var treesWithStructsWithAttributes = context.Compilation.SyntaxTrees
            .Where(st => st.GetRoot().DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .Any(p => p.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Any()))
            .ToList();

        foreach (var tree in treesWithStructsWithAttributes)
        {
            var semanticModel = context.Compilation.GetSemanticModel(tree);

            var structsWithAttributes = tree.GetRoot().DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .Where(cd => cd.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Any());

            foreach (var structDeclaration in structsWithAttributes)
            {
                var foundAttribute = GetEndiannessAttribute(structDeclaration, semanticModel);
                if (foundAttribute == null)
                    continue; // not my type

                var structIsLittleEndian = GetStructIsLittleEndian(foundAttribute);
                HandleStruct(context, structDeclaration, semanticModel, structIsLittleEndian);
            }
        }
    }

    private static void HandleStruct(GeneratorExecutionContext context, TypeDeclarationSyntax structDeclaration,
        SemanticModel semanticModel, bool structIsLittleEndian)
    {
        var isPartial = structDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
            throw new InvalidUsageException("struct is not marked partial");

        var accessibilityModifier = structDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))
            ? Token(SyntaxKind.InternalKeyword)
            : Token(SyntaxKind.PublicKeyword);

        var structType = semanticModel.GetDeclaredSymbol(structDeclaration);
        if (structType == null)
            throw new DebugMeException("struct type info is null");

        var structNamespace = structType.ContainingNamespace?.ToDisplayString();
        if (structNamespace == null)
            throw new InvalidUsageException("struct has to be contained in a namespace");

        if (structDeclaration.Members.Any(m => m.IsKind(SyntaxKind.PropertyDeclaration)))
            throw new InvalidUsageException("struct cannot have properties");

        var fieldDeclarations = structDeclaration.Members
            .Where(m => m.IsKind(SyntaxKind.FieldDeclaration)).OfType<FieldDeclarationSyntax>();

        var generatedCode = CompilationUnit()
            .WithUsings(List<UsingDirectiveSyntax>([
                UsingDirective(IdentifierName("System")),
                UsingDirective(IdentifierName("System.Buffers.Binary"))
            ]))
            .WithMembers(List<MemberDeclarationSyntax>([
                FileScopedNamespaceDeclaration(IdentifierName(structNamespace)),
                StructDeclaration(structType.Name)
                    .WithModifiers(TokenList([accessibilityModifier, Token(SyntaxKind.PartialKeyword)]))
                    .WithMembers(GenerateStructProperties(fieldDeclarations, semanticModel, structIsLittleEndian))
            ]))
            .NormalizeWhitespace()
            .ToFullString();

        context.AddSource(
            $"{structNamespace}.{structType.Name}.g.cs",
            SourceText.From(generatedCode, Encoding.UTF8)
        );
    }

    private static SyntaxList<MemberDeclarationSyntax> GenerateStructProperties(
        IEnumerable<FieldDeclarationSyntax> fieldDeclarations, SemanticModel semanticModel, bool structIsLittleEndian)
    {
        var result = new List<MemberDeclarationSyntax>();
        foreach (var field in fieldDeclarations)
        {
            if (!field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                throw new InvalidUsageException("fields have to be private");

            var variableDeclaration = field.DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();
            if (variableDeclaration == null)
                throw new DebugMeException("variable declaration of field declaration null");

            var variableTypeInfo = semanticModel.GetTypeInfo(variableDeclaration.Type).Type;
            if (variableTypeInfo == null)
                throw new DebugMeException("variable type info of field declaration null");

            var typeName = variableTypeInfo.ToDisplayString();
            var fieldName = variableDeclaration.Variables.First().Identifier.ToString();

            result.Add(GenerateProperty(typeName, structIsLittleEndian, fieldName));
        }

        return new SyntaxList<MemberDeclarationSyntax>(result);
    }

    private static PropertyDeclarationSyntax GenerateProperty(string typeName,
        bool structIsLittleEndian, string fieldName)
    {
        var propertyName = GeneratePropertyName(fieldName);
        var fieldIdentifier = IdentifierName(fieldName);

        ExpressionSyntax condition = MemberAccessExpression(
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: IdentifierName("BitConverter"),
            name: IdentifierName("IsLittleEndian")
        );

        if (!structIsLittleEndian)
            condition = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condition);

        var reverseEndiannessMethod = MemberAccessExpression(
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: IdentifierName("BinaryPrimitives"),
            name: IdentifierName("ReverseEndianness")
        );

        return PropertyDeclaration(ParseTypeName(typeName), propertyName)
            .WithModifiers(TokenList([Token(SyntaxKind.PublicKeyword)]))
            .WithAccessorList(AccessorList(List<AccessorDeclarationSyntax>([
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithExpressionBody(ArrowExpressionClause(ConditionalExpression(
                        condition: condition,
                        whenTrue: fieldIdentifier,
                        whenFalse: InvocationExpression(
                            expression: reverseEndiannessMethod,
                            argumentList: ArgumentList(SingletonSeparatedList(
                                Argument(fieldIdentifier)
                            ))
                        )
                    )))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(ArrowExpressionClause(AssignmentExpression(
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: fieldIdentifier,
                        right: ConditionalExpression(
                            condition: condition,
                            whenTrue: fieldIdentifier,
                            whenFalse: InvocationExpression(
                                expression: reverseEndiannessMethod,
                                argumentList: ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName("value"))
                                ))
                            )
                        )
                    )))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            ])));
    }

    private static SyntaxToken GeneratePropertyName(string fieldName)
    {
        var propertyName = fieldName;
        if (propertyName.StartsWith("_"))
            propertyName = propertyName.Substring(1);
        if (!char.IsLetter(propertyName, 0) || char.IsUpper(propertyName, 0))
            throw new InvalidUsageException("field names have to start with a lower case letter");
        propertyName = propertyName.Substring(0, 1).ToUpperInvariant()
                       + propertyName.Substring(1);
        return Identifier(propertyName);
    }

    private static AttributeSyntax? GetEndiannessAttribute(SyntaxNode structDeclaration, SemanticModel semanticModel)
    {
        AttributeSyntax? foundAttribute = null;
        foreach (var attributeSyntax in structDeclaration.DescendantNodes().OfType<AttributeSyntax>())
        {
            var attributeTypeInfo = semanticModel.GetTypeInfo(attributeSyntax).Type;
            if (attributeTypeInfo == null)
                throw new DebugMeException("attribute type info is null");

            if (attributeTypeInfo.ContainingNamespace?.Name != Namespace)
                continue;
            if (attributeTypeInfo.Name != AttributeName)
                continue;

            foundAttribute = attributeSyntax;
            break;
        }

        return foundAttribute;
    }

    private static bool GetStructIsLittleEndian(AttributeSyntax foundAttribute)
    {
        var endiannessArguments = foundAttribute.ArgumentList;
        if (endiannessArguments == null)
            throw new InvalidUsageException("endianness attribute has no arguments");

        var isLittleEndianArgumentSyntax = endiannessArguments.Arguments
            .FirstOrDefault(argumentSyntax =>
                argumentSyntax.NameEquals?.Name.Identifier.ToString() == IsLittleEndianProperty);
        if (isLittleEndianArgumentSyntax == null)
            throw new InvalidUsageException("endianness attribute argument not found");

        bool? structIsLittleEndian = isLittleEndianArgumentSyntax.Expression.Kind() switch
        {
            SyntaxKind.FalseLiteralExpression => false,
            SyntaxKind.TrueLiteralExpression => true,
            SyntaxKind.DefaultLiteralExpression => false,
            _ => throw new InvalidUsageException($"{IsLittleEndianProperty} has to be set with a literal")
        };
        return structIsLittleEndian.Value;
    }
}
