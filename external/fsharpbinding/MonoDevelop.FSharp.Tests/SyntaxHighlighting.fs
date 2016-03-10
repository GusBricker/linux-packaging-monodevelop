﻿namespace MonoDevelopTests

open System
open NUnit.Framework
open MonoDevelop.FSharp
open Mono.TextEditor
open Mono.TextEditor.Highlighting
open MonoDevelop.Ide.Editor
open FsUnit

[<TestFixture>]
type SyntaxHighlighting() =
    let assertStyle (input:string, expectedStyle:string) =
        let offset = input.IndexOf("§")
        let length = input.LastIndexOf("§") - offset - 1
        let input = input.Replace("§", "")
        let data = new TextEditorData (new TextDocument (input))
        let syntaxMode = SyntaxModeService.GetSyntaxMode (data.Document, "text/x-fsharp")
        let style = SyntaxModeService.GetColorStyle ("Gruvbox")
        let line = data.Lines |> Seq.head
        let chunks = syntaxMode.GetChunks(style, line, offset, line.Length)
        let chunk = chunks |> Seq.tryFind (fun c -> c.Offset = offset && c.Length = length)
        match chunk with
        | Some (c) -> c.Style |> should equal expectedStyle
        | _ -> printfn "Offset - %d, Length - %d" offset length
               printfn "%A" chunks
               Assert.Fail()

    [<TestCase("let simpleBinding = §1§", "Number")>]
    [<TestCase("§let§ simpleBinding = 1", "Keyword(Iteration)")>]
    [<TestCase("§let!§ simpleBinding = 1", "Keyword(Iteration)")>]
    [<TestCase("let §simpleBinding§ = 1", "User Field Declaration")>]
    [<TestCase("let §offset§ = 1", "User Field Declaration")>]
    [<TestCase("let §add§ x y = x + y", "User Method Declaration")>]
    [<TestCase("let simpleBinding§ = §1", "Plain Text")>]
    [<TestCase("§open§ MonoDevelop", "Keyword(Namespace)")>]
    [<TestCase("open§ MonoDevelop§", "Plain Text")>]
    [<TestCase("open§ Mono.Text§", "Plain Text")>]
    [<TestCase("Seq.§find§ (", "User Method Declaration")>]
    [<TestCase("SyntaxModeService.§GetColorStyle§ (\"Gruvbox\")", "User Method Declaration")>]
    [<TestCase("Seq.find (§fun§ c", "Keyword(Jump)")>]
    [<TestCase("§type§ SyntaxHighlighting() =", "Keyword(Namespace)")>]
    [<TestCase("type §SyntaxHighlighting§ () =", "User Types")>]
    [<TestCase("§module§ MyModule =", "Keyword(Namespace)")>]
    [<TestCase("module §MyModule§ =", "User Types")>]
    [<TestCase("[<§TestCase§(", "User Types")>]
    [<TestCase("[§<§TestCase(", "Punctuation(Brackets)")>]
    [<TestCase("inherits §SyntaxHighlighting§ () =", "User Types")>]
    [<TestCase("new §DefaultBraceMatcher§()", "User Types")>]
    [<TestCase("§match§ (startOffset, endOffset) with", "Keyword(Iteration)")>]
    [<TestCase("§else§", "Keyword(Iteration)")>]
    [<TestCase("let x (y: §string§", "User Types")>]
    [<TestCase("string.§Length§", "User Property Declaration")>]
    [<TestCase("§(§", "Punctuation(Brackets)")>]
    [<TestCase("§<§", "Punctuation(Brackets)")>]
    [<TestCase("§[§", "Punctuation(Brackets)")>]
    [<TestCase("§{§", "Punctuation(Brackets)")>]
    [<TestCase("do Something() |> §ignore§", "User Method Declaration")>]
    [<TestCase("let §mutable§ x = 1", "Keyword(Modifiers)")>]
    [<TestCase("let mutable §x§ = 1", "User Field Declaration")>]
    [<TestCase("c.Style §|> §should equal", "Plain Text")>]
    [<TestCase("c.Style |> §should§ equal", "User Method Declaration")>]
    [<TestCase("match §x§ with", "User Field Declaration")>]
    [<TestCase("Unchecked.defaultof<§_§>", "Plain Text")>]
    [<TestCase("Seq.§add§", "User Method Declaration")>]
    [<TestCase("let inline §add§ x y = x + y", "User Method Declaration")>]
    [<TestCase("§override§ x.Something()", "Keyword(Modifiers)")>]
    [<TestCase("member x.§``some identifier``§ = 1", "User Field Declaration")>]
    [<TestCase("member x.§``some identifier``§ () = 1", "User Method Declaration")>]
    [<TestCase("let mutable §vbox4§ : Gtk.VBox = null", "User Field Declaration")>]
    [<TestCase("§return§ x", "Keyword(Iteration)")>]
    [<TestCase("§return!§ x", "Keyword(Iteration)")>]
    [<TestCase("member val IndentOnTryWith = false with §get, set§", "Plain Text")>]
    [<TestCase("| Some §funion§ -> ", "User Field Declaration")>]
    [<TestCase("yield §sprintf§ \"%A\"", "User Method Declaration")>]
    [<TestCase("§doc§.Editor", "User Field Declaration")>]
    [<TestCase(":> §SomeType§", "User Types")>]
    [<TestCase("(§'c'§)", "String")>]
    [<TestCase("| Type of §string§", "User Types")>]
    [<TestCase("§DisplayFlags§ = DisplayFlags.DescriptionHasMarkup", "User Field Declaration")>]
    [<TestCase("let shouldEqual (x: §'a§) (y: 'a) =", "User Types")>]
    [<TestCase("| :? §string§", "User Types")>]
    [<TestCase("let inline §private§ is expr s =", "Keyword(Modifiers)")>]
    [<TestCase("let inline private §is§ expr s =", "User Method Declaration")>]
    [<TestCase("override x.§CanHandle§ editor", "User Method Declaration")>]
    [<TestCase("let addEdge ((n1, n2): 'n * §'n§)", "User Types")>]
    [<TestCase("Map<'n, Set<'n§>>§", "Punctuation(Brackets)")>]
    [<TestCase("let docs = §openDocuments§()", "User Method Declaration")>]
    [<TestCase("let x = §true§", "Keyword(Constants)")>]
    [<TestCase("let §``simple binding``§ = 1", "User Field Declaration")>]
    [<TestCase("let inline §``add number``§ x y = x + y", "User Method Declaration")>]
    [<TestCase("§|>§ Option.bind", "Punctuation(Brackets)")>]
    member x.``Syntax highlighting``(source, expectedStyle) =
        assertStyle (source, expectedStyle)