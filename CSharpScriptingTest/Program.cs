////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CSharpScriptingTest
// Copyright (c) Kouji Matsui, All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Using NuGet Microsoft.CodeAnalysis.Scripting.CSharp-1.1.0-rc1
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;

namespace CSharpScriptingTest
{
	class Program
	{
		sealed class SuppressionResult
		{
			public Diagnostic Diag;
			public SuppressionInfo Info;
		}

		sealed class ScriptResult
		{
			// for debug: I have no idea how to extract symbol informations, try anything extraction...
			public Script<object> Script;
			public Compilation Compilation;
			public SyntaxTree[] SyntaxTree;
			public SemanticModel[] Semantic;
			public SuppressionResult[] Diag;
		}


		/// <summary>
		/// Do script splitted iteration with more 1-chars, and create ScriptResults.
		/// </summary>
		/// <param name="script">Script string</param>
		/// <returns>ScriptResult iterator</returns>
		static IEnumerable<ScriptResult> IterateScripts(string script)
		{
			for (var index = 1; index < script.Length; index++)
			{
				var script1 = CSharpScript.Create(
					script.Substring(0, index),	// repeat more 1-chars...
					ScriptOptions.Default
					.WithImports("System"));

				var compilation = script1.GetCompilation();

				// Strategy: Fixed to array, because debugging improvement, no side effect means.
				yield return new ScriptResult
				{
					Script = script1,
					Compilation = compilation,
					SyntaxTree = compilation.SyntaxTrees.
						ToArray(),
					Semantic = compilation.SyntaxTrees.
						Select(tree => compilation.GetSemanticModel(tree)).
						ToArray(),
					Diag = compilation.GetDiagnostics().
						Select(d => new SuppressionResult { Diag = d, Info = d.GetSuppressionInfo(compilation) }).
						ToArray(),
				};
			}
		}

		static async Task MainAsync()
		{
			// Extract script typed informations...
			var results = IterateScripts(
				"var data = 123; Console.WriteLine(\"Hello C# scripting with data: {0}\", data);").
				//                  |        |     |
				//                  ^--- suggest[0] = Type:System.Console
				//                           |     |
				//                           ^--- suggest[0] = Method:System.Console.WriteLine  OR
				//                           ^--- suggest[1] = Method:System.Console.Write
				//                                 |
				//                                 ^--- suggest[0] = Method:System.Console.WriteLine(string)  OR
				//                                 ^--- suggest[1] = Method:System.Console.WriteLine(string, string) OR ... overloads
				ToArray();

			// for debug: I have no idea how to extract symbol informations, try anything extraction...
			var result = results[28];
			var diag = result.Diag[0];
			var location = diag.Diag.Location;
			var descriptor = diag.Diag.Descriptor;
			var semantic = result.Semantic[0];
			var syntaxtree = semantic.SyntaxTree;

			/////////////////////////////////////////////////////////////////

			// Q: I want Intelli-sense like dynamic completion procedure.

			//   Where suggest (types/methods/props/uncomplited symbols/etc...) informations?
			//   How to extract informations?
			//   Extracted information is "ISymbol" ?

			/////////////////////////////////////////////////////////////////
		}

		static void Main(string[] args)
		{
			MainAsync().Wait();
		}
	}
}
