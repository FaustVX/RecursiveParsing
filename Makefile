# https://makefiletutorial.com/

bin/main: obj/main.s
	cc -o $@ $^

obj/main.s: obj/main.ssa
	qbe -o $@ $^

obj/main.ssa: input.txt $(shell find . -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs") *.*proj
	~/dotnet/dotnet run -- $<

$(shell find ./out/ -name "*g.cs" | tail -n 1): myLang.ebnf lib/ebnf/bin/Debug/net11.0/EBNFParser.dll
	~/dotnet/dotnet build RecursiveParsing.csproj -t:ParseEBNF

lib/ebnf/bin/Debug/net11.0/EBNFParser.dll: ./lib/ebnf/*.*proj $(shell find ./lib/ebnf/ -not \( -path "./lib/*" -o -path "./obj/*" \) -name "*.cs")
	~/dotnet/dotnet build $<

clean:
	rm -rf obj/main.* bin/main out/; ~/dotnet/dotnet clean

run: bin/main
	./$^
