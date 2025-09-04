all: publish-server

publish-server:
	dotnet publish src/Kureko \
		-r linux-x64 \
		--self-contained \
		-p PublishSingleFile=true \
		-p DebugSymbols=false \
		-o dist/server
	mv dist/server/Kureko dist/server/kureko
		
clean:
	rm -rf dist
