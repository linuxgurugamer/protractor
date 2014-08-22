all: build install

build:
	xbuild protractor.csproj
clean:
	-rm -rf obj
	-rm -rf bin

package: install
	(cd Output; zip -r ProtractorContinued.zip Protractor)

install:
	cp bin/Debug/protractor.dll ./Output/Protractor/Plugins/
	cp ../Protractor.version ./Output/Protractor/
	cp LICENSE ./Output/Protractor/
	cp RPMDefs.cfg ./Output/Protractor/
	#cp gpl.txt ./Output/Protractor/
	#cp mit.txt ./Output/Protractor/
