# https://makefiletutorial.com/

bin/input: obj/input.s
	cc -o $@ $^

obj/input.s: obj/input.ssa
	qbe -o $@ $^

obj/input.ssa: input.txt $(shell find . -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs") *.*proj
	~/dotnet/dotnet run -- $<

$(shell find ./out/ -name "*g.cs" | tail -n 1): myLang.ebnf lib/ebnf/bin/Debug/net11.0/EBNFParser.dll
	~/dotnet/dotnet build RecursiveParsing.csproj -t:ParseEBNF

lib/ebnf/bin/Debug/net11.0/EBNFParser.dll: ./lib/ebnf/*.*proj $(shell find ./lib/ebnf/ -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs")
	~/dotnet/dotnet build $<

clean:
	rm -rf obj/input.* bin/input out/; ~/dotnet/dotnet clean

run: bin/input
	./$^
