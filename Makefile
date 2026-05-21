# https://makefiletutorial.com/

bin/input: obj/input.s
	cc -o $@ $^

obj/input.s: obj/input.ssa
	qbe -o $@ $^

obj/input.ssa: bin/Debug/net11.0/RecursiveParsing.dll input.txt Visitors/runtime.ssa
	~/dotnet/dotnet $^

bin/Debug/net11.0/RecursiveParsing.dll: *.*proj out/Ext.g.cs $(shell find . -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs")
	~/dotnet/dotnet build $<

out/Ext.g.cs: myLang.ebnf lib/ebnf/bin/Debug/net11.0/EBNFParser.dll
	~/dotnet/dotnet build RecursiveParsing.csproj -t:ParseEBNF

lib/ebnf/bin/Debug/net11.0/EBNFParser.dll: ./lib/ebnf/*.*proj $(shell find ./lib/ebnf -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs")
	~/dotnet/dotnet build $<

clean:
	rm -rf obj/input.* bin/input out/; ~/dotnet/dotnet clean

run: bin/input
	./$^
