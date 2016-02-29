﻿namespace MonoDevelopTests

open System
open Microsoft.FSharp.Compiler.SourceCodeServices
open NUnit.Framework
open MonoDevelop.FSharp
open MonoDevelop.Ide.Editor
open Mono.TextEditor.Highlighting
open FsUnit

[<TestFixture>]
type SemanticHighlighting() =
    let defaultStyles = SyntaxModeService.DefaultColorStyle
    let getStyle (content : string) =
        let fixedc = content.Replace("§", "")
        let doc = TestHelpers.createDoc fixedc "defined"
        let style = SyntaxModeService.GetColorStyle "Gruvbox"
        let segments =
            doc.Editor.GetLines()
            |> Seq.map (fun line -> let tokensSymbolsColours = SyntaxMode.tryGetTokensSymbolsAndColours doc
                                    let segments =
                                        SyntaxMode.getColouredSegment
                                            tokensSymbolsColours
                                            line.LineNumber
                                            line.Offset
                                            (doc.Editor.GetLineText line)
                                            style
                                    segments)
                                    
        let sortedUniqueSegments =
            segments
            |> Seq.concat
            |> Seq.distinct
            |> Seq.sortBy (fun s -> s.Offset)      
        
        for seg in sortedUniqueSegments do
            printf """Seg: %s S:%i E:%i L:%i - "%s" %s""" seg.ColorStyleKey seg.Offset seg.EndOffset seg.Length (doc.Editor.GetTextBetween(seg.Offset, seg.EndOffset)) Environment.NewLine

        let offset = content.IndexOf("§")
        let endOffset = content.LastIndexOf("§") - 1
        let segment = sortedUniqueSegments |> Seq.tryFind (fun s -> s.Offset = offset && s.EndOffset = endOffset)
        match segment with
        | Some(s) -> s.ColorStyleKey
        | _ -> "segment not found"

    [<Test>]
    member x.If_is_preprocessor() =
        let content =
            """§#if§ undefined
            let add = (+)
            #endif
            """
        let output = getStyle content
        output |> should equal "Preprocessor"

    [<Test>]
    member x.Test_is_plain_text() =
        let content =
            """#if §undefined§
            let add = (+)
            #endif
            """
        getStyle content |> should equal "Plain Text"

    [<Test>]
    member x.Ifdeffed_code_is_excluded() =
        let content =
            """#if undefined
            §let§ add = (+)
            #endif
            """
        getStyle content |> should equal "Excluded Code"

    [<Test>]
    member x.Endif_is_preprocessor() =
        let content =
            """#if undefined
            let add = (+)
            §#endif§
            """
        getStyle content |> should equal "Preprocessor"

    [<Test>]
    member x.Let_is_keyword() =
        let content =
            """#if defined
            §let§ add = (+)
            #endif
            """
        getStyle content |> should equal "Keyword(Type)"

    [<Test>]
    member x.Module_is_highlighted() =
        let content = """
                    module MyModule =
                        let someFunc() = ()

                    module Consumer =
                        §MyModule§.someFunc()
                    """
        let output = getStyle content
        output |> should equal "User Types"

    [<Test>]
    member x.Type_is_highlighted() =
        let content = """
                    open System

                    module MyModule =
                        let guid = §Guid§.NewGuid()
                    """
        let output = getStyle content
        output |> should equal "User Types(Value types)"

    [<Test>]
    member x.Add_is_plain_text() =
        let content = "let §add§ = (+)"
        getStyle content |> should equal "User Method Declaration"

    [<TestCase("let add = (§+§)", "Punctuation")>]
    [<TestCase("let §add§ = (+)", "User Method Declaration")>]
    [<TestCase("let add = §(§+)", "Punctuation(Brackets)")>]
    [<TestCase("let §simpleBinding§ = 1", "User Field Declaration")>]
    [<TestCase("let simpleBinding = §1§", "Number")>]
    [<TestCase("§type§ x() = ()", "Keyword(Iteration)")>]
    member x.Semantic_highlighting(source, expectedStyle) =
        getStyle source |> should equal expectedStyle
        
    [<Test>]    
    member x.Overlapping_custom_operators_are_highlighted() =
        let content = """
module Test =
    let ( §>>=§ ) a b = a + b"""
        let output = getStyle content
        output |> should equal defaultStyles.PunctuationForBrackets.Name
        
    [<Test>]    
    member x.Generics_are_highlighted() =
        let content = """
type Class<§'a§>() = class end
    let _ = new Class<_>()"""
        let output = getStyle content
        output |> should equal defaultStyles.UserTypesTypeParameters.Name
     
    [<Test>]    
    member x.Type_constraints_are_highlighted() =
        let content = """type Constrained<'a when §'a§ :> IDisposable> = class end"""
        let output = getStyle content
        output |> should equal defaultStyles.UserTypesTypeParameters.Name

    [<Test>]    
    member x.Static_inlined_type_constraints_are_highlighted() =
        let content = """let inline test (x: §^a§) (y: ^b) = x + y"""
        let output = getStyle content
        output |> should equal defaultStyles.UserTypesTypeParameters.Name